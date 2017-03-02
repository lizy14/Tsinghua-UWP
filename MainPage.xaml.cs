
using System;
using System.Threading.Tasks;
using TsinghuaUWP.Courses;
using TsinghuaUWP.Logins;
using TsinghuaUWP.TsinghuaTVs;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
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
            var titleBar = Windows.UI.ViewManagement.ApplicationView.GetForCurrentView().TitleBar;
            titleBar.BackgroundColor = Colors.Purple;
            titleBar.ButtonHoverBackgroundColor = Colors.Wheat;
            titleBar.ButtonBackgroundColor = Colors.Purple;
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
             if (tvflag) {
                // MessageDialog a = new MessageDialog("请将正在播放的视频暂停");
                // a.ShowAsync();
                // TV.IsSelected = true;
                MyFrame.Navigate(typeof(TsinghuaTV));
                tvflag = false;
                 // goto stoptv;
              }
            
            if (Courses.IsSelected)
            {

                Refresh.Visibility = Visibility.Collapsed;

                MyFrame.Navigate(typeof(Learn));
                TitleTextBlock.Text = "Courses beta";
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
                TitleTextBlock.Text = "Mails";
            }
            else if (TV.IsSelected)
            {
                Refresh.Visibility = Visibility.Collapsed;
                MyFrame.Navigate(typeof(TsinghuaTV));
                TitleTextBlock.Text = "Tsinghua TV IPV6";
                tvflag = true;
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
            var Url = "http://www.tsinghua.edu.cn/publish/newthu/index.html";

            Webview.Navigate(new Uri(Url));
            //  Refresh.Visibility = Visibility.Collapsed;
            // MyFrame.Navigate(typeof(Loginindex));
            // TitleTextBlock.Text = "Login";
        }
    }
}
