﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Background;
using Windows.Storage;
using Windows.UI;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using KittensAndPets.BackgroundTasks;
using KittensAndPets.Models.Common;
using KittensAndPets.Models.Helpers;
using Template10.Mvvm;

namespace KittensAndPets.ViewModels
{
    public class OptionsViewModel : ViewModelBase
    {
        private bool isInitialized = false;
        private readonly ApplicationDataContainer localSettings;
        private bool isBackgroundTaskEnabled;
        private string lastTaskStatusMessage;
        private string currentStatus = "Background task not running";
        private SolidColorBrush currentStatusBrush = new SolidColorBrush(Colors.Red);
        private List<int> monitorTimeWindows;
        private int selectedMonitorTimeWindow = 30;
        private bool isBusy;
        private string isBusyMessage;

        public OptionsViewModel()
        {
            if (DesignMode.DesignModeEnabled)
                return;

            localSettings = ApplicationData.Current.LocalSettings;
        }

        #region Properties

        public bool IsBackgroundTaskEnabled
        {
            get { return isBackgroundTaskEnabled; }
            set
            {
                Set(ref isBackgroundTaskEnabled, value);

                // Prevents registering the task multiple times when the view model first loads
                if (!isInitialized)
                    return;

                // Enable or disable the task accordingly
                if (value)
                    EnableBackgroundTask();
                else
                    DisableBackgroundTask();
            }
        }

        public string CurrentStatus
        {
            get { return currentStatus; }
            set { Set(ref currentStatus, value); }
        }

        public SolidColorBrush CurrentStatusBrush
        {
            get { return currentStatusBrush; }
            set { Set(ref currentStatusBrush, value); }
        }

        public string LastTaskStatusMessage
        {
            get { return lastTaskStatusMessage; }
            set { Set(ref lastTaskStatusMessage, value); }
        }

        public List<int> MonitorTimeWindows => monitorTimeWindows ?? (monitorTimeWindows = new List<int> { 60, 120, 240, 680 });
        
        public int SelectedMonitorTimeWindow
        {
            get
            {
                if (DesignMode.DesignModeEnabled)
                    return selectedMonitorTimeWindow;

                object obj;
                if (localSettings != null && localSettings.Values.TryGetValue(Constants.SelectedMonitorTimeWindowSettingsKey, out obj))
                {
                    selectedMonitorTimeWindow = (int)obj;
                }

                return selectedMonitorTimeWindow;
            }
            set
            {
                Set(ref selectedMonitorTimeWindow, value);

                if (localSettings != null)
                    localSettings.Values[Constants.SelectedMonitorTimeWindowSettingsKey] = selectedMonitorTimeWindow;

                EnableBackgroundTask(selectedMonitorTimeWindow);
            }
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

        #region Methods

        public override async Task OnNavigatedToAsync(object parameter, NavigationMode mode, IDictionary<string, object> state)
        {
            try
            {
                // This is so that we don't re-register the task again (see the IsBackgroundTaskEnabled property setter)
                isInitialized = false;

                UpdateStatus("loading...");

                IsBackgroundTaskEnabled = await BackgroundTaskHelpers.CheckBackgroundTasksAsync(Constants.SearchTaskFriendlyNameSettingsKey);

                if (IsBackgroundTaskEnabled)
                {
                    ShowBackgroundTaskStatus("Background task running", true);
                }
                else
                {
                    ShowBackgroundTaskStatus("Background task not running", false);
                }

                isInitialized = true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"OnNavigatedToAsync Exception: {ex}");
            }
            finally
            {
                UpdateStatus("");
            }
        }

        private async void EnableBackgroundTask(int frequency = 30)
        {
            try
            {
                UpdateStatus("registering background task...");

                var accessStatus = await BackgroundExecutionManager.RequestAccessAsync();

                switch (accessStatus)
                {
                    // If we are allowed to register a background task
                    case BackgroundAccessStatus.AlwaysAllowed:
                    case BackgroundAccessStatus.AllowedSubjectToSystemPolicy:
                        BackgroundTaskHelpers.RegisterAsync(Constants.SearchTaskFriendlyNameSettingsKey, typeof(SearchAndTweetTask).FullName, (uint)frequency);
                        ShowBackgroundTaskStatus("Background task running", true);
                        break;

                    // If we were denied access, show the the reason to the user
                    case BackgroundAccessStatus.DeniedBySystemPolicy:
                        ShowBackgroundTaskStatus($"Background Task Disabled by System Policy", false);
                        await new MessageDialog("The app has denied from adding a background task due to System Policy. This is usually because there are too many background tasks already. " + "r\n\nGo to Phone Settings > Background Apps and free up a couple slots.").ShowAsync();
                        break;
                    case BackgroundAccessStatus.Unspecified:
                        ShowBackgroundTaskStatus($"Background Task Disabled", false);
                        await new MessageDialog("You did not make a choice. If you want to get reminders for upcoming lunches, please try again.").ShowAsync();
                        break;
                    case BackgroundAccessStatus.DeniedByUser:
                        ShowBackgroundTaskStatus($"Background Task Disabled", false);
                        await new MessageDialog("You've blocked background tasks for this app or you have too many background tasks already. " + "r\n\nGo to Phone Settings > Background Apps \r\n\nFind this app in the list and re-enable background tasks.").ShowAsync();
                        break;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"EnableBackgroundTask Exception: {ex}");
            }
            finally
            {
                UpdateStatus("");
            }
        }

        private async void DisableBackgroundTask()
        {
            try
            {
                UpdateStatus("unregistering background task...");

                if (await BackgroundTaskHelpers.UnregisterTaskAsync(Constants.SearchTaskFriendlyNameSettingsKey))
                    ShowBackgroundTaskStatus("Reminders Background Task Disabled", false);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"DisableBackgroundTask Exception: {ex}");
            }
            finally
            {
                UpdateStatus("");
            }
        }

        /// <summary>
        /// Shows busy indicator. Passing an empty string will hide the busy overlay
        /// </summary>
        /// <param name="message">busy message to show, leave empty to hide the overlay</param>
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

        private void ShowBackgroundTaskStatus(string status, bool isBackgroundTaskRunning)
        {
            CurrentStatus = status;
            CurrentStatusBrush = isBackgroundTaskRunning
                ? new SolidColorBrush(Colors.Green)
                : new SolidColorBrush(Colors.Red);
        }

        #endregion

        #region Event Handlers

        public void GetStatusButton_OnClick(object sender, RoutedEventArgs e)
        {
            object obj;
            if (localSettings != null && localSettings.Values.TryGetValue(Constants.BackgroundTaskStatusSettingsKey, out obj))
            {
                LastTaskStatusMessage = obj.ToString();
            }
        }

        #endregion
    }
}
