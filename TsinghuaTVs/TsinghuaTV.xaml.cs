using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
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

namespace TsinghuaUWP.TsinghuaTVs
{
    /// <summary>
    /// 可用于自身或导航至 Frame 内部的空白页。
    /// </summary>
     public sealed partial class TsinghuaTV : Page
    {
        private ObservableCollection<TV1> TV1;
        private List<TV1> TTT;
        public TsinghuaTV()
        {
            this.InitializeComponent();
            TV1 = new ObservableCollection<TV1>();
            TTT = new List<TV1>();

        }

        private void REFRESHKUAIDI_Click(object sender, RoutedEventArgs e)
        {
            TTT = TVManager.GETTV("hd", TV1);
        }

        private void TVGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)

        {
            string Url
        = "https://iptv.tsinghua.edu.cn/player.html?vid=cctv1hd";
            int tp = TVGrid.SelectedIndex;
            Url = TTT[tp].URL;
            // Webview.Navigate(new Uri(Url));
            MyMedias.Source = new Uri(Url);
        }
    }
}
