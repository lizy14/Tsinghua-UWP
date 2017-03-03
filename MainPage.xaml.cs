
using System;
using System.Threading.Tasks;
using TsinghuaUWP.Courses;
using TsinghuaUWP.Logins;
using TsinghuaUWP.TsinghuaTVs;
using Windows.System.Profile;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Documents;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace TsinghuaUWP {
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        bool tvflag;
        bool tvfllag;
        public MainPage()
        {
            this.InitializeComponent();
            var bb = AnalyticsInfo.VersionInfo.DeviceFamily;
            if (bb == "Windows.Desktop" || bb == "Windows.Tablet")
            {
                var titleBar = Windows.UI.ViewManagement.ApplicationView.GetForCurrentView().TitleBar;
                titleBar.BackgroundColor = Colors.Purple;
                titleBar.ButtonHoverBackgroundColor = Colors.Wheat;
                titleBar.ButtonBackgroundColor = Colors.Purple;
            }
            else
            {
                var appTitleBar = Windows.UI.ViewManagement.ApplicationView.GetForCurrentView().TitleBar;
                var coreTitleBar = Windows.ApplicationModel.Core.CoreApplication.GetCurrentView().TitleBar;
                appTitleBar.BackgroundColor = Colors.Black;
                // Highest.Background = "Black";

            }

            tvflag = false;
            
        }

        private void MyFrame_Navigated(object sender, NavigationEventArgs e)
        {

        }
        private void HamburgerButton_Click(object sender, RoutedEventArgs e)
        {
            MySplitView.IsPaneOpen = !MySplitView.IsPaneOpen;
        }
        private void ListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            Webview.Visibility = Visibility.Collapsed;
            if(!News.IsSelected)
            {
                BackButton.Visibility = Visibility.Collapsed;
            }
             if (tvflag) {
               
                MyFrame.Navigate(typeof(TsinghuaTV));
                tvflag = false;
               
              }
            
            if (Courses.IsSelected)
            {

                Refresh.Visibility = Visibility.Collapsed;

                MyFrame.Navigate(typeof(Learn));
                TitleTextBlock.Text = "MyCourses beta";
            }
            else if (Login.IsSelected)
            {
                Refresh.Visibility = Visibility.Collapsed;
                MyFrame.Navigate(typeof(Loginindex));
                TitleTextBlock.Text = "Login";
            }
            else if (Mails.IsSelected)
            {
                Refresh.Visibility = Visibility.Collapsed;
                Webview.Visibility = Visibility.Visible;
                Webview.Navigate(new Uri("http://mails.tsinghua.edu.cn/coremail/xphone/index.jsp"));
                //MyFrame.Navigate(typeof(WEBS));
                TitleTextBlock.Text = "Tsinghua Mails";
            }
            else if (TV.IsSelected)
            {
                Refresh.Visibility = Visibility.Collapsed;
                MyFrame.Navigate(typeof(TsinghuaTV));
                TitleTextBlock.Text = "Tsinghua TV";
                tvflag = true;
            }
            else if(News.IsSelected)
            {
                Refresh.Visibility = Visibility.Collapsed;
                Webview.Visibility = Visibility.Visible;
                Webview.Navigate(new Uri("http://news.tsinghua.edu.cn/publish/thunews/index.html"));
                BackButton.Visibility = Visibility.Visible;
                TitleTextBlock.Text = "Tsinghua News";
            }
        
        }

        private void SearchButton_Click(object sender, RoutedEventArgs e)
        {
            String Baidu = "https://www.bing.com/search?q=";
            String Content = SearchTextBox.Text;
            String helpurl = string.Join("", Baidu, Content);
            Windows.System.Launcher.LaunchUriAsync(new Uri(helpurl));
            // Webview.Visibility = Visibility.Visible;
            // Webview.Navigate(new Uri(helpurl));
        }
        private void TitleTextBlock_SelectionChanged(object sender, RoutedEventArgs e)
        {

        }

        private void Refresh_Click(object sender, RoutedEventArgs e)
        {
        }
        private void Initial1(object sender, RoutedEventArgs e)
        {
            News.IsSelected = true;
            
        }

        private void Webview_NewWindowRequested(WebView sender, WebViewNewWindowRequestedEventArgs e)
        {
            
            Webview.Navigate(e.Uri);
           
            e.Handled = true;

        }

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            if(News.IsSelected&&Webview.CanGoBack&&Webview.Source!= new Uri("http://news.tsinghua.edu.cn/publish/thunews/index.html"))
            {
                Webview.GoBack();

            }
            

        }
    }
}
