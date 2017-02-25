using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using TsinghuaComplex.Courses;
using TsinghuaComplex.Logins;
using TsinghuaComplex.WEBs;
using TsinghuaComplex.TsinghuaTVs;
using Windows.UI.Popups;



//“空白页”项模板在 http://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409 上有介绍

namespace TsinghuaComplex
{
    /// <summary>
    /// 可用于自身或导航至 Frame 内部的空白页。
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
            Webview.Visibility=Visibility.Collapsed;
          /*  if (tvflag) {
                MessageDialog a = new MessageDialog("请将正在播放的视频暂停");
                a.ShowAsync();
                TV.IsSelected = true;
                tvflag = false;
                goto stoptv;
            }*/
            

            if (Courses.IsSelected)
            {

                Refresh.Visibility = Visibility.Collapsed;

                MyFrame.Navigate(typeof(Learn));
                TitleTextBlock.Text = "Courses beta";
            }
            else if(Login.IsSelected)
            {
                Refresh.Visibility = Visibility.Collapsed;
                MyFrame.Navigate(typeof(Loginindex));
                TitleTextBlock.Text = "Login";
            }
            else if(Mails.IsSelected)
            {
                Refresh.Visibility = Visibility.Collapsed;
                Webview.Visibility = Visibility.Visible;
                Webview.Navigate(new Uri("http://mails.tsinghua.edu.cn/coremail/xphone/index.jsp"));
                //MyFrame.Navigate(typeof(WEBS));
                TitleTextBlock.Text = "Mails";
            }
            else if(TV.IsSelected)
            {
                Refresh.Visibility = Visibility.Collapsed;
                MyFrame.Navigate(typeof(TsinghuaTV));
                TitleTextBlock.Text = "Tsinghua TV IPV6";
                tvflag = true;
            }
        stoptv:;
        }

        private void SearchButton_Click(object sender, RoutedEventArgs e)
        {
            String Baidu = "https://www.baidu.com/s?wd=";
            String Content = SearchTextBox.Text;
            String helpurl = string.Join("", Baidu, Content);
            Windows.System.Launcher.LaunchUriAsync(new Uri(helpurl));
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
            // var httpRequestMessage = new Windows.Web.Http.HttpRequestMessage(Windows.Web.Http.HttpMethod.Get, new Uri(Url));
            //  var userAgent = "Mozilla/5.0 (Windows Phone 10.0; Android 4.2.1; WebView/3.0; Microsoft; Virtual) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/42.0.2311.135 Mobile Safari/537.36 Edge/12.10240 sample/1.0";
            //  httpRequestMessage.Headers.Add("User-Agent", userAgent);
            // Webview.NavigateWithHttpRequestMessage(httpRequestMessage);
            Webview.Navigate(new Uri(Url));
        }
    }
}
