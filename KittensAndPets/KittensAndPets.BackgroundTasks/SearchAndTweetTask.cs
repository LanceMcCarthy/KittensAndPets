using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.ApplicationModel.Background;
using Windows.Storage;
using Windows.UI.Xaml;
using KittensAndPets.Models.Common;
using Microsoft.Toolkit.Uwp.Services.Bing;
using Microsoft.Toolkit.Uwp.Services.Twitter;

namespace KittensAndPets.BackgroundTasks
{
    public sealed class SearchAndTweetTask : IBackgroundTask
    {
        private ApplicationDataContainer localSettings;

        public async void Run(IBackgroundTaskInstance taskInstance)
        {
            var deferral = taskInstance.GetDeferral();

            try
            {
                localSettings = ApplicationData.Current.LocalSettings;
                if(localSettings == null)
                    return;

                // -------- Check user is valid --------

                LogStatus("Starting...");

                object obj;
                if (localSettings.Values.TryGetValue(Constants.IsUserLoggedInSettingsKey, out obj))
                {
                    // if the user is not logged in, return
                    if (!(bool) obj)
                    {
                        LogStatus("User was not logged in");
                        return;
                    }
                }
                else
                {
                    // setting couldnt be retrieved
                    LogStatus("IsUserLoggedIn setting could not be retrieved");
                    return;
                }

                // -------- Check search term is valid --------

                string query = "";
                object searchObj;
                if (localSettings.Values.TryGetValue(Constants.CurrentSearchTermSettingsKey, out searchObj))
                {
                    query = searchObj.ToString();

                    // if the search term is empty, return
                    if (string.IsNullOrEmpty(query))
                    {
                        LogStatus("Search Term was empty, canceled tweets");
                        return;
                    }
                }
                else
                {
                    // setting couldnt be retrieved
                    return;
                }

                // -------- Perform search --------

                LogStatus($"Performing search for {query}");

                var searchConfig = new BingSearchConfig
                {
                    Country = BingCountry.UnitedStates,
                    Language = BingLanguage.English,
                    Query = query,
                    QueryType = BingQueryType.Search
                };

                var searchResult = await BingService.Instance.RequestAsync(searchConfig, 10);

                LogStatus($"Search for {query} returned {searchResult.Count} results");

                LogStatus("Logging into twitter");

                // -------- Login to twitter --------

                TwitterService.Instance.Initialize(
                    ServiceKeys.TwitterConsumerKey,
                    ServiceKeys.TwitterConsumerSecret,
                    ServiceKeys.CallbackUri);

                if (!await TwitterService.Instance.LoginAsync())
                {
                    // If the login failed, return
                    LogStatus("Twitter login failed");
                    return;
                }
                
                var user = await TwitterService.Instance.GetUserAsync();

                LogStatus($"Logged in as {user.ScreenName}");

                // get recent tweets so that we dont duplicate tweets
                var recentTweets = await TwitterService.Instance.GetUserTimeLineAsync(user.ScreenName, 10);

                int newItemsCount = 0;

                foreach (var bingResult in searchResult)
                {
                    foreach (var recentTweet in recentTweets)
                    {
                        // If we've already tweeted this, skip this loop
                        if(recentTweet.Text == bingResult.Title)
                            continue;

                        var message = "BGTEST" + bingResult.Title;

                        if (message.Length > 140)
                            message = message.Substring(0, 137) + "...";


                        await TwitterService.Instance.TweetStatusAsync(message);
                        newItemsCount++;

                        // TODO Determine if posting a tweet with a picture is viable
                        //await TwitterService.Instance.TweetStatusAsync(TweetText.Text, stream);
                    }
                }

                LogStatus($"Tweeted {newItemsCount} item(s)");
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
                LogStatus($"Error: {ex.Message}");
            }
            finally
            {
                deferral.Complete();
            }
        }

        private void LogStatus(string message)
        {
            if (localSettings != null)
                localSettings.Values[Constants.BackgroundTaskStatusSettingsKey] = message;

#if DEBUG
            Debug.WriteLine($"Background Task Log - {message}");
#endif
        }
    }
}
