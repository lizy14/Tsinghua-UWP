using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// “空白页”项模板在 http://go.microsoft.com/fwlink/?LinkId=234238 上有介绍

namespace TsinghuaComplex.Logins
{
    /// <summary>
    /// 可用于自身或导航至 Frame 内部的空白页。
    /// </summary>
    public sealed partial class Loginindex : Page
    {
        public Loginindex()
        {
            this.InitializeComponent();
            btnUpdate.IsEnabled = !DataAccess.credentialAbsent();
            btnRefreshTimetable.IsEnabled = !DataAccess.credentialAbsent();
            if(!DataAccess.credentialAbsent())
            {
                btnLogin.Content = "注销登录";
            }
        }

        protected override async void OnNavigatedTo(NavigationEventArgs e)
        { 
            //if (App.Islogin)
             //   btnLogin.Content = "注销登录";
             if (DataAccess.supposedToWorkAnonymously())
            {
                btnLogin.Content = "登录";
                btnRefreshTimetable.IsEnabled = false;
                btnUpdate.IsEnabled = false;
                update_without_credential();
            }
            else if (!DataAccess.supposedToWorkAnonymously()
               && DataAccess.credentialAbsent())
            {
                update_without_credential();
                await changeAccountAsync();
            }
            else if (!DataAccess.credentialAbsent())
            {
                update_with_credential();
            }
        }

        private async void update_with_credential()
        {

            updateDeadlinesAsyc();
            updateTimetableAsync();
            Appointment.updateCalendar();
        }

        private async void update_without_credential()
        {
            try
            {
                await Notification.update(calendarOnly: true);
                await Appointment.updateCalendar();
            }
            catch { }

        }

        private async Task changeAccountAsync()
        {
            btnLogin.Content = "登录";
            this.btnRefreshTimetable.IsEnabled = false;
            this.btnUpdate.IsEnabled = false;
            this.btnLogin.IsEnabled = false;
            // added code by zhang
           // App.Islogin = false;// 全局登陆模式
            if (await changeAccountHelper())
            {
                this.btnLogin.Content = "注销登录";
                this.btnRefreshTimetable.IsEnabled = true;
                this.btnUpdate.IsEnabled = true;
                update_with_credential();
                // added code by zhang
               // App.Islogin = true;// 全局登陆模式
                var dialog1 = new ContentDialog()
                {
                    Title = "登录提示",
                    Content = "Welcome!",
                    PrimaryButtonText = "确定",
                    FullSizeDesired = false,
                };
                dialog1.PrimaryButtonClick += (_s, _e) => { };
                await dialog1.ShowAsync();
                // add end 
            }
            else
            {
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
            try
            {
                password = await dialog.getCredentialAsyc();
                this.progressLogin.IsActive = false;
            }
            catch (UserCancelException)
            {
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
        private async Task updateDeadlinesAsyc()
        {
            updateNotificationsCounter++;

            this.progressUpdate.IsActive = true;
            this.btnUpdate.IsEnabled = false;
            this.errorUpdate.Visibility = Visibility.Collapsed;

            //TODO: simplify update logic of local being fall-back
            try
            {
                await Notification.update(true);
                await Appointment.updateDeadlines();
            }
            catch (Exception e)
            {
                this.errorUpdate.Visibility = Visibility.Visible;
                try
                {
                    await Notification.update();
                }
                catch (Exception) { }
            }

            if (--updateNotificationsCounter == 0)
            {
                this.progressUpdate.IsActive = false;
                this.btnUpdate.IsEnabled = !DataAccess.credentialAbsent();
            }
        }

        private int updateTimetableCounter = 0;

        private async Task updateTimetableAsync()
        {
            updateTimetableCounter++;

            this.progressRefreshTimetable.IsActive = true;
            this.btnRefreshTimetable.IsEnabled = false;
            this.errorRefreshTimetable.Visibility = Visibility.Collapsed;
            try
            {
                await Appointment.updateTimetable(true);
            }
            catch (Exception e)
            {
                this.errorRefreshTimetable.Visibility = Visibility.Visible;
                try
                {
                    await Appointment.updateTimetable(true);
                    this.errorRefreshTimetable.Visibility = Visibility.Collapsed;
                }
                catch (Exception) { }
            }

            if (--updateTimetableCounter == 0)
            {
                this.progressRefreshTimetable.IsActive = false;
                this.btnRefreshTimetable.IsEnabled = true;
            }
        }

        private async void btnRefreshTimetable_Click(object sender, RoutedEventArgs e)
        {
            await updateTimetableAsync();

        }

        private async void btnUpdate_Click(object sender, RoutedEventArgs e)
        {
            await updateDeadlinesAsyc();

        }

        private async void btnLogin_Click(object sender, RoutedEventArgs e)
        {
            await changeAccountAsync();
        }
    }
}

