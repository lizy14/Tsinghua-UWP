using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Popups;

namespace TsinghuaUWP
{
    public class courseuwp
    {
        public string id { get; set; }
        public string name { get; set; }
        public bool isNew { get; set; } //uses (`learn.cic` or `learn`?) .tsinghua.edu.cn
        public string semester { get; set; }
    }

    public class CourseManager
    {
        private static List<Course> dejson;
        private static List<Deadline> dejson1;
        public static void GetNews(ObservableCollection<Course> newsItems)
        {


            //var filteredNewsItems = allItems;
            newsItems.Clear();
            //filteredNewsItems.ForEach(p => newsItems.Add(p));

        }
        public static async Task<List<Course>> CourseGet(ObservableCollection<Course> newsItems)
        {
            try
            {
                await Remote.login();
                dejson = Remote.parseCourseList(await Remote.getCourseListPage());
            }
            catch
            {
                if (DataAccess.credentialAbsent())
                {
                    MessageDialog b = new MessageDialog("请先登录,点击左边最下面一个按键进入登录页面");
                    await b.ShowAsync();

                }
                else
                {
                    MessageDialog a = new MessageDialog("数据错误");
                    await a.ShowAsync();
                }

            }

            newsItems.Clear();
            dejson.ForEach(p => newsItems.Add(p));
            return dejson;
        }
        public static async Task<List<Deadline>> getRemoteHomeworkList(string courseId, ObservableCollection<Deadline> newsItems)
        {

            try
            {
                await Remote.login();
                dejson1 = Remote.parseHomeworkListPage(await Remote.getHomeworkListPage(courseId));
                //dejson1 =Remote.parseAncListPage(await Remote.getAncListPage(courseId));
            }
            catch
            {
                // MessageDialog a = new MessageDialog("暂时不支持新版网络学堂");
                // await a.ShowAsync();
                try
                {
                    dejson1 = await Remote.parseHomeworkListPageNew(await Remote.getHomeworkListPageNew(courseId));

                }
                catch
                {
                    MessageDialog a = new MessageDialog("数据错误");
                    await a.ShowAsync();
                }
            }
            newsItems.Clear();
            dejson1.ForEach(p => newsItems.Add(p));
            return dejson1;
        }


        public static async Task<List<Deadline>> getRemoteAncList(string courseId, ObservableCollection<Deadline> newsItems)
        {

            try
            {
                await Remote.login();
                // dejson1 =Remote.parseHomeworkListPage(await Remote.getHomeworkListPage(courseId));
                dejson1 = Remote.parseAncListPage(await Remote.getAncListPage(courseId));
            }
            catch
            {
               
                try
                {
                    var uri = await Remote.getNewAncPage(courseId);
                    dejson1 = await Remote.parseAncPageNew(uri);

                }
                catch
                {
                    MessageDialog a = new MessageDialog("数据错误");
                    await a.ShowAsync();
                }
            }
            newsItems.Clear();
            dejson1.ForEach(p => newsItems.Add(p));
            return dejson1;
        }
    }


}
