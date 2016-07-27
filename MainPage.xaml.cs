using System;
using System.Collections.Generic;
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
            if (DataAccess.supposedToWorkAnonymously() == true
                || DataAccess.credentialAbsent() == false) {
                this.InitializeComponent();
                button.Content = "注销并重新登录";
            }
                


        }

        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            Notification.update();

            if (DataAccess.supposedToWorkAnonymously() == false
                && DataAccess.credentialAbsent() == true) { 
                await changeAccount();
                this.InitializeComponent();
                button.Content = "登录";
            }
        }

        async Task changeAccount()
        {
            var dialog = new PasswordDialog();
            Password password;
            try{
                password = await dialog.getCredentialAsyc();
            }catch(UserCancelException){
                //user choose to stay anonymous
                DataAccess.setLocalSettings("username", "__anonymous");
                return;
            }

            //save credential
            DataAccess.setLocalSettings("username", password.username);

            var vault = new Windows.Security.Credentials.PasswordVault();
            vault.Add(new Windows.Security.Credentials.PasswordCredential(
                "Tsinghua_Learn_Website", password.username, password.password));

            //fresh log-in, update everything
            Notification.update(true);
            Appointment.update(true);
            try
            {
                button.Content = "注销并重新登录";
            }
            catch (Exception)
            {

            }
        }

        private void button_Click(object sender, RoutedEventArgs e)
        {
            changeAccount();
        }
    }
}
