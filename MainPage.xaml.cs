
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace TsinghuaUWP
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {

        public MainPage()
        {
            this.InitializeComponent();
        }

        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            Notification.update(calendarOnly: true);
            if (DataAccess.supposedToWorkAnonymously() == false
                && DataAccess.credentialAbsent() == true) {
                await changeAccountAsync();
            }

            if(DataAccess.credentialAbsent() == false) {
                updateNotificationsAsyc();
            }
        }
        async Task changeAccountAsync()
        {
            btnLogin.Content = "登录";
            this.btnRefreshTimetable.IsEnabled = false;
            this.btnUpdate.IsEnabled = false;
            this.btnLogin.IsEnabled = false;
            if (await changeAccountWithoutModifyingUI())
            {
                this.btnLogin.Content = "注销登录";
                this.btnRefreshTimetable.IsEnabled = true;
                this.btnUpdate.IsEnabled = true;
            }
            else
            {
                this.btnLogin.Content = "登录";
            }
            this.progressLogin.IsActive = false;
            this.btnLogin.IsEnabled = true;
        }
        async Task<bool> changeAccountWithoutModifyingUI()
        {
            var dialog = new PasswordDialog();
            Password password;
            this.progressLogin.IsActive = true;
            try
            {
                password = await dialog.getCredentialAsyc();
                this.progressLogin.IsActive = false;
            }
            catch(UserCancelException){
                //user choose to stay anonymous
                DataAccess.setLocalSettings("username", "__anonymous");
                return false;
            }

            //save credential
            DataAccess.setLocalSettings("username", password.username);

            var vault = new Windows.Security.Credentials.PasswordVault();
            vault.Add(new Windows.Security.Credentials.PasswordCredential(
                "Tsinghua_Learn_Website", password.username, password.password));

            //fresh log-in, update everything
            updateNotificationsAsyc();
            updateTimetableAsync();

            return true;
        }

        private void button_Click(object sender, RoutedEventArgs e)
        {
            changeAccountAsync();
        }
        private async Task updateNotificationsAsyc()
        {
            this.progressUpdate.IsActive = true;
            this.btnUpdate.IsEnabled = false;
            try
            {
                await Notification.update(true);
                this.errorUpdate.Visibility = Visibility.Collapsed;
            }
            catch (Exception)
            {
                this.errorUpdate.Visibility = Visibility.Visible;
            }
            
            this.progressUpdate.IsActive = false;
            this.btnUpdate.IsEnabled = ! DataAccess.credentialAbsent();
        }
        
        private void btnRefreshTimetable_Click(object sender, RoutedEventArgs e)
        {
            updateTimetableAsync();
        }
        private async Task updateTimetableAsync()
        {
            this.progressRefreshTimetable.IsActive = true;
            this.btnRefreshTimetable.IsEnabled = false;
            this.errorRefreshTimetable.Visibility = Visibility.Collapsed;
            try {
                await Appointment.update(true);
                this.errorRefreshTimetable.Visibility = Visibility.Collapsed;
            } catch (Exception) {
                this.errorRefreshTimetable.Visibility = Visibility.Visible;
            }
            this.progressRefreshTimetable.IsActive = false;
            this.btnRefreshTimetable.IsEnabled = true;
        }

        private async void btnUpdate_Click(object sender, RoutedEventArgs e)
        {
            await updateNotificationsAsyc();
        }

    }
}
