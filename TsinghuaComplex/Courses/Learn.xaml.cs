using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using TsinghuaComplex.Model;
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

namespace TsinghuaComplex.Courses
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

        }

        private async void CourseGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            //List<Course> temp;
            int tp;
            String coid;
            tp = CourseGrid.SelectedIndex;

            try
            {
                coid = listcourses[tp].id;
              
                listddl = await CourseManager.getRemoteAncList(coid, ddl1);
               // tryhomework.Text = listddl[0].name;
                
            }
            catch
            {

            }
            ButtonHK.IsEnabled = true;
            ButtonAnc.IsEnabled = false;
        }

        private async void ButtonHK_Click(object sender, RoutedEventArgs e)
        {
            int tp;
            String coid;
            tp = CourseGrid.SelectedIndex;

            try
            {
                coid = listcourses[tp].id;

                listddl = await CourseManager.getRemoteHomeworkList(coid, ddl1);
                // tryhomework.Text = listddl[0].name;

            }
            catch
            {

            }
            ButtonHK.IsEnabled = false;
            ButtonAnc.IsEnabled = true;
        }

        private async void ButtonAnc_Click(object sender, RoutedEventArgs e)
        {
            int tp;
            String coid;
            tp = CourseGrid.SelectedIndex;

            try
            {
                coid = listcourses[tp].id;

                listddl = await CourseManager.getRemoteAncList(coid, ddl1);
                // tryhomework.Text = listddl[0].name;

            }
            catch
            {

            }
            ButtonHK.IsEnabled = true;
            ButtonAnc.IsEnabled = false;
        }
    }
}
