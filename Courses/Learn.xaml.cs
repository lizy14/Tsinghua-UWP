using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// “空白页”项模板在 http://go.microsoft.com/fwlink/?LinkId=234238 上有介绍

namespace TsinghuaUWP.Courses
{
    /// <summary>
    /// 可用于自身或导航至 Frame 内部的空白页。
    /// </summary>
    public sealed partial class Learn : Page
    {
        public List<Course> listcourses;
        private ObservableCollection<Deadline> ddl1;
        private ObservableCollection<Course> courses1;
        private List<Deadline> listddl;
        public Learn()
        {
            this.InitializeComponent();
            courses1 = new ObservableCollection<Course>();
            ddl1 = new ObservableCollection<Deadline>();
        }
        private async void Coursebuttons_Click(object sender, RoutedEventArgs e)
        {
            PR0.IsActive = true;
            try
            {
                //listcourses.Clear();
                listcourses = await CourseManager.CourseGet(courses1);//temp
                coursetry.Text = listcourses[0].id + "";
            }
            catch
            {
                MessageDialog a = new MessageDialog("Wrong data");
                await a.ShowAsync();
            }
            PR0.IsActive = false;
        }

        private async void CourseGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            //List<Course> temp;
            INFOTB.Visibility = Visibility.Collapsed;
            PR1.IsActive = true;

            int tp;
            String coid;
            tp = CourseGrid.SelectedIndex;

            try
            {
                coid = listcourses[tp].id;

                listddl = await CourseManager.getRemoteAncList(coid, ddl1);

               var text= listddl[0];
               
            }
            catch
            {
                INFOTB.Visibility = Visibility.Visible;
            }
            ButtonHK.IsEnabled = true;
            ButtonAnc.IsEnabled = false;
            PR1.IsActive = false;
        }

        private async void ButtonHK_Click(object sender, RoutedEventArgs e)
        {
            INFOTB.Visibility = Visibility.Collapsed;
            PR1.IsActive = true;
            int tp;
            String coid;
            tp = CourseGrid.SelectedIndex;

            try
            {
                coid = listcourses[tp].id;

                listddl = await CourseManager.getRemoteHomeworkList(coid, ddl1);

                var text = listddl[0];

            }
            catch
            {
                INFOTB.Visibility = Visibility.Visible;
            }
            ButtonHK.IsEnabled = false;
            ButtonAnc.IsEnabled = true;
            PR1.IsActive = false;
        }

        private async void ButtonAnc_Click(object sender, RoutedEventArgs e)
        {
            INFOTB.Visibility = Visibility.Collapsed;
            PR1.IsActive = true;
            int tp;
            String coid;
            tp = CourseGrid.SelectedIndex;

            try
            {
                coid = listcourses[tp].id;

                listddl = await CourseManager.getRemoteAncList(coid, ddl1);

                var text = listddl[0];

            }
            catch
            {
                INFOTB.Visibility = Visibility.Visible;
            }
            ButtonHK.IsEnabled = true;
            ButtonAnc.IsEnabled = false;
            PR1.IsActive = false;
        }

        private async void HWGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            int tp;
            tp = HWGrid.SelectedIndex;

            try
            {
                 var tp1 = CourseGrid.SelectedIndex;
                bool coid = listcourses[tp1].isNew;

                if(!coid)
                {
                    string uri = listddl[tp].detail;
                    var dialog = new ContentDialogAnc(uri);
                    
                    await dialog.ShowAsync();
                }
                else
                {
                    string uri = listddl[tp].detail;


                    var dialog1 = new ContentDialogAncnew(uri);

                    await dialog1.ShowAsync();

                }
               

                HWGrid.SelectedItem = null;

            }
            catch
            {
            }


        }

        private void GridView_Loading(FrameworkElement sender, object args)
        {
         
        }

        private void GridView_Loaded(object sender, RoutedEventArgs e)
        {
           
        }
    }
}
