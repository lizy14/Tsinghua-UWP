﻿
using System;
using System.Threading.Tasks;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace TsinghuaUWP {
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page {

        public MainPage() {
            this.InitializeComponent();
        }

        protected override async void OnNavigatedTo(NavigationEventArgs e) {

            if (DataAccess.supposedToWorkAnonymously()) {
                btnLogin.Content = "登录";
                btnRefreshTimetable.IsEnabled = false;
                btnUpdate.IsEnabled = false;
                update_without_credential();
            } else if (!DataAccess.supposedToWorkAnonymously()
                 && DataAccess.credentialAbsent()) {
                update_without_credential();
                await changeAccountAsync();
            } else if (!DataAccess.credentialAbsent()) {
                update_with_credential();
            }
        }

        private async void update_with_credential() {

            updateDeadlinesAsyc();
            updateTimetableAsync();
            Appointment.updateCalendar();
            try {
                await Appointment.updateLectures();
            } catch { }
        }

        private async void update_without_credential() {
            try {
                await Notification.update(calendarOnly: true);
                await Appointment.updateCalendar();
            } catch { }
            try {
                await Appointment.updateLectures();
            } catch { }

        }

        private async Task changeAccountAsync() {
            btnLogin.Content = "登录";
            this.btnRefreshTimetable.IsEnabled = false;
            this.btnUpdate.IsEnabled = false;
            this.btnLogin.IsEnabled = false;
            if (await changeAccountHelper()) {
                this.btnLogin.Content = "注销登录";
                this.btnRefreshTimetable.IsEnabled = true;
                this.btnUpdate.IsEnabled = true;
                update_with_credential();
            } else {
                this.btnLogin.Content = "登录";
                update_without_credential();
            }
            this.progressLogin.IsActive = false;
            this.btnLogin.IsEnabled = true;
        }

        private async Task<bool> changeAccountHelper() //false for anonymous
        {
            var dialog = new PasswordDialog();
            Password password;
            this.progressLogin.IsActive = true;
            try {
                password = await dialog.getCredentialAsyc();
                this.progressLogin.IsActive = false;
            } catch (UserCancelException) {
                //user choose to stay anonymous
                DataAccess.setLocalSettings("username", "__anonymous");
                return false;
            }

            //save credential
            //TODO: wrap as a function and move into DataAccess
            DataAccess.setLocalSettings("toasted_assignments", "");
            DataAccess.setLocalSettings("username", password.username);

            var vault = new Windows.Security.Credentials.PasswordVault();
            vault.Add(new Windows.Security.Credentials.PasswordCredential(
                "Tsinghua_Learn_Website", password.username, password.password));

            return true;
        }

        private int updateNotificationsCounter = 0;
        private async Task updateDeadlinesAsyc() {
            updateNotificationsCounter++;

            this.progressUpdate.IsActive = true;
            this.btnUpdate.IsEnabled = false;
            this.errorUpdate.Visibility = Visibility.Collapsed;

            //TODO: simplify update logic of local being fall-back
            try {
                await Notification.update(true);
                await Appointment.updateDeadlines();
            } catch (Exception e) {
                this.errorUpdate.Visibility = Visibility.Visible;
                try {
                    await Notification.update();
                } catch (Exception) { }
            }

            if (--updateNotificationsCounter == 0) {
                this.progressUpdate.IsActive = false;
                this.btnUpdate.IsEnabled = !DataAccess.credentialAbsent();
            }
        }

        private int updateTimetableCounter = 0;
        private async Task updateTimetableAsync() {
            updateTimetableCounter++;

            this.progressRefreshTimetable.IsActive = true;
            this.btnRefreshTimetable.IsEnabled = false;
            this.errorRefreshTimetable.Visibility = Visibility.Collapsed;
            try {
                await Appointment.updateTimetable(true);
            } catch (Exception e) {
                this.errorRefreshTimetable.Visibility = Visibility.Visible;
                try {
                    await Appointment.updateTimetable(true);
                    this.errorRefreshTimetable.Visibility = Visibility.Collapsed;
                } catch (Exception) { }
            }

            if (--updateTimetableCounter == 0) {
                this.progressRefreshTimetable.IsActive = false;
                this.btnRefreshTimetable.IsEnabled = true;
            }
        }

        private void launchHelp() {
            Windows.System.Launcher.LaunchUriAsync(new Uri(Remote.helpUrl));
        }

        private void btnRefreshTimetable_Click(object sender, RoutedEventArgs e) {
            updateTimetableAsync();
        }

        private void btnUpdate_Click(object sender, RoutedEventArgs e) {
            updateDeadlinesAsyc();
        }

        private void btnHelp_Click(object sender, RoutedEventArgs e) {
            launchHelp();
        }

        private void button_Click(object sender, RoutedEventArgs e) {
            changeAccountAsync();
        }

    }
}
