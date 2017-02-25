
using System;
using System.Threading.Tasks;
using Windows.UI;
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
            MyFrame.Navigate(typeof(Function1));
            var titleBar = Windows.UI.ViewManagement.ApplicationView.GetForCurrentView().TitleBar;
            titleBar.BackgroundColor = Colors.Purple;
            titleBar.ButtonHoverBackgroundColor = Colors.Wheat;
            titleBar.ButtonBackgroundColor = Colors.Purple;



        }

        private void HamburgerButton_Click(object sender, RoutedEventArgs e)
        {
            MySplitView.IsPaneOpen = !MySplitView.IsPaneOpen;
        }

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            if (MyFrame.CanGoBack)
            {
                MyFrame.GoBack();
                Function1.IsSelected = true;
            }
        }

        private void ListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (Function1.IsSelected)
            {
                BackButton.Visibility = Visibility.Visible;
                MyFrame.Navigate(typeof(Function1));
                TitleTextBlock.Text = "Tsinghua UWP";
                
            }
            else if (Food.IsSelected)
            {
                BackButton.Visibility = Visibility.Visible;
                MyFrame.Navigate(typeof(About));
                TitleTextBlock.Text = "About";
            }
        }

        private void SearchButton_Click(object sender, RoutedEventArgs e)
        { String Baidu = "https://www.baidu.com/s?wd=";
            String Content = SearchTextBox.Text;
            String helpurl= string.Join("",Baidu,Content);
            Windows.System.Launcher.LaunchUriAsync(new Uri(helpurl));
        }

        private void MyFrame_Navigated(object sender, NavigationEventArgs e)
        {

        }
    }
}
