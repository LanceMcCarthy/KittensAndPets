using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Threading.Tasks;
using Windows.ApplicationModel;
using Windows.Storage;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Navigation;
using KittensAndPets.Models;
using KittensAndPets.Models.Common;
using Microsoft.Toolkit.Uwp;
using Microsoft.Toolkit.Uwp.Services.Bing;
using Microsoft.Toolkit.Uwp.Services.Twitter;
using Template10.Mvvm;

namespace KittensAndPets.ViewModels
{
    public class MainViewModel : ViewModelBase
    {
        #region Fields

        private readonly StorageFolder localFolder;
        private readonly ApplicationDataContainer localSettings;

        private bool isBusy;
        private string isBusyMessage;
        private ObservableCollection<Tweet> tweets;
        private TwitterUser user;
        private ObservableCollection<BingResult> searchResults;
        private BingResult selectedSearchResult;
        private string searchTerm;
        private Tweet selectedTweet;
        private bool isSearchEnabled;
        private DelegateCommand searchCommand;
        private DelegateCommand loginCommand;
        private DelegateCommand logoutCommand;

        #endregion

        public MainViewModel()
        {
            if (DesignMode.DesignModeEnabled)
            {
                Tweets = SampleDataService.GenerateTweets();
                SearchResults = SampleDataService.GenerateBingResults();
                User = SampleDataService.GenerateUser();
                return;
            }

            localFolder = ApplicationData.Current.LocalFolder;
            localSettings = ApplicationData.Current.LocalSettings;
        }
        
        #region Properties

        public ObservableCollection<Tweet> Tweets
        {
            get { return tweets ?? (tweets = new ObservableCollection<Tweet>()); }
            set { Set(ref tweets, value); }
        }

        public Tweet SelectedTweet
        {
            get { return selectedTweet; }
            set { Set(ref selectedTweet, value); }
        }

        public ObservableCollection<BingResult> SearchResults
        {
            get { return searchResults ?? (searchResults = new ObservableCollection<BingResult>()); }
            set { Set(ref searchResults, value); }
        }

        public BingResult SelectedSearchResult
        {
            get { return selectedSearchResult; }
            set { Set(ref selectedSearchResult, value); }
        }

        public string SearchTerm
        {
            get { return searchTerm; }
            set
            {
                IsSearchEnabled = !string.IsNullOrEmpty(value);
                Set(ref searchTerm, value);
            }
        }

        public TwitterUser User
        {
            get { return user; }
            set
            {
                Set(ref user, value);

                // If the User object is not null, then the user is logged in
                if (localSettings != null)
                    localSettings.Values[Constants.IsUserLoggedInSettingsKey] = value != null;
            }
        }

        public bool IsSearchEnabled
        {
            get { return isSearchEnabled; }
            set { Set(ref isSearchEnabled, value); }
        }

        public bool IsBusy
        {
            get { return isBusy; }
            set { Set(ref isBusy, value); }
        }

        public string IsBusyMessage
        {
            get { return isBusyMessage; }
            set { Set(ref isBusyMessage, value); }
        }

        #endregion

        #region Commands

        public DelegateCommand SearchCommand => searchCommand ?? (searchCommand = new DelegateCommand(async () =>
        {
            await SearchAsync(SearchTerm);
        }));

        public DelegateCommand LoginCommand => loginCommand ?? (loginCommand = new DelegateCommand(async () =>
        {
            await LogIntoTwitterAsync();
        }));

        public DelegateCommand LogoutCommand => logoutCommand ?? (logoutCommand = new DelegateCommand(async () =>
        {
            

            bool shouldLogout = await DispatcherHelper.ExecuteOnUIThreadAsync<bool>(async () =>
            {
                var md = new MessageDialog("Are you sure you want to log out?", "Log Out");
                md.Commands.Add(new UICommand("log out"));
                md.Commands.Add(new UICommand("cancel"));
                md.CancelCommandIndex = 1;
                md.DefaultCommandIndex = 1;
                var result = await md.ShowAsync();

                return result.Label == "log out";
            });

            if (shouldLogout)
                LogOutOfTwitter();
        }));

        #endregion
        
        #region Methods

        private async Task SearchAsync(string query)
        {
            try
            {
                UpdateStatus($"searching {searchTerm}");

                // Update the search term so that the background task can stay in sync
                if (localSettings != null)
                    localSettings.Values[Constants.CurrentSearchTermSettingsKey] = query;

                SearchResults.Clear();

                var searchConfig = new BingSearchConfig
                {
                    Country = BingCountry.UnitedStates,
                    Language = BingLanguage.English,
                    Query = query,
                    QueryType = BingQueryType.Search
                };

                var result = await BingService.Instance.RequestAsync(searchConfig, 50);

                SearchResults.Clear();

                if (result == null)
                {
                    // We'll first check if the emptycollectionconverter works

                    //await new MessageDialog("There were no results from that search, try again.", "No Results").ShowAsync();

                    //SearchResults.Add(new BingResult() {Title = "No Results"});
                    return;
                }
                
                foreach (var bingResult in result)
                {
                    SearchResults.Add(bingResult);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"SearchAsync Exception: {ex}");
            }
            finally
            {
                UpdateStatus("");
            }
        }

        public async Task<bool> LogIntoTwitterAsync()
        {
            try
            {
                UpdateStatus("logging into Twitter...");

                TwitterService.Instance.Initialize(
                    ServiceKeys.TwitterConsumerKey,
                    ServiceKeys.TwitterConsumerSecret,
                    ServiceKeys.CallbackUri);

                if (!await TwitterService.Instance.LoginAsync())
                {
                    User = null;
                    return false;
                }
                
                User = await TwitterService.Instance.GetUserAsync();

                var recentTweets = await TwitterService.Instance.GetUserTimeLineAsync(User.ScreenName, 50);

                foreach (var tweet in recentTweets)
                {
                    if(!Tweets.Contains(tweet))
                        Tweets.Add(tweet);
                }

                return true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"LogIntoTwitterAsync Exception: {ex}");
                User = null;
                return false;
            }
            finally
            {
                UpdateStatus("");
            }
        }

        public void LogOutOfTwitter()
        {
            TwitterService.Instance.Logout();
            User = null;
            Tweets.Clear();
        }
        
        private void UpdateStatus(string message)
        {
            if (string.IsNullOrEmpty(message))
            {
                IsBusy = false;
                IsBusyMessage = message;
            }
            else
            {
                IsBusy = true;
                IsBusyMessage = message;
            }
        }

        public override async Task OnNavigatedToAsync(object parameter, NavigationMode mode, IDictionary<string, object> state)
        {
            try
            {
                UpdateStatus("loading...");

                if (User == null)
                {
                    await LogIntoTwitterAsync();
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"OnNavigatedToAsync Exception: {ex}");
            }
            finally
            {
                UpdateStatus("");
            }

            // Needed if there arenot awaitable methods
            //return base.OnNavigatedToAsync(parameter, mode, state);
        }

        #endregion
        
    }
}
