using HtmlAgilityPack;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Windows.Web.Http;
using System.Runtime.Serialization.Json;
using System.IO;
using System.Diagnostics;
using Windows.Storage;

namespace TsinghuaUWP
{
    public static class Remote
    {

        //Remote Access

        public static async Task<List<Deadline>> getRemoteHomeworkList(string courseId)
        {
            await login();
            return parseHomeworkListPage(await getHomeworkListPage(courseId));
        }
        public static async Task<List<Deadline>> getRemoteHomeworkListNew(string courseId)
        {
            await login();
            return await parseHomeworkListPageNew(await getHomeworkListPageNew(courseId));
        }
        public static async Task<List<Course>> getRemoteCourseList()
        {
            await login();
            return parseCourseList(await getCourseListPage());
        }
        public static async Task<Semesters> getHostedSemesters()
        {
            return JSON.parse<Semesters>(await getPageContent(hostedCalendarUrl));
        }
        public static async Task<Semesters> getRemoteSemesters()
        {
            await login();
            var _remoteCalendar = parseCalendarPage(await getCalendarPage());
            return new Semesters
            {
                currentSemester = _remoteCalendar.currentSemester,
                nextSemester = _remoteCalendar.nextSemester
            };
        }

        static DateTime lastLogin = DateTime.MinValue;
        static int LOGIN_TIMEOUT_MINUTES = 5;
        public static async Task<int> login(
            bool useLocalSettings = true,
            string username = "",
            string password = "")
        {

            //check for last login
            if ((DateTime.Now - lastLogin).TotalMinutes < LOGIN_TIMEOUT_MINUTES)
            {
                Debug.WriteLine("[login] reuses recent session");
                return 2;
            }

            Debug.WriteLine("[login] begin");

            if (useLocalSettings)
            {
                if (DataAccess.credentialAbsent()) {
                    throw new LoginException("没有指定用户名和密码");
                }
                username = DataAccess.getLocalSettings()["username"].ToString();

                var vault = new Windows.Security.Credentials.PasswordVault();
                password = vault.Retrieve("Tsinghua_Learn_Website", username).Password;
            }

            //login to learn.tsinghua.edu.cn
            HttpStringContent stringContent = new HttpStringContent(
                $"leixin1=student&userid={username}&userpass={password}",
                Windows.Storage.Streams.UnicodeEncoding.Utf8,
                "application/x-www-form-urlencoded");

            httpResponse = await httpClient.PostAsync(new Uri(loginUri), stringContent);
            httpResponse.EnsureSuccessStatusCode();
            var loginResponse = await httpResponse.Content.ReadAsStringAsync();

            //check if successful
            var alertInfoGroup = Regex.Match(loginResponse, @"window.alert\(""(.+)""\);").Groups;
            if (alertInfoGroup.Count > 1)
            {
                throw new LoginException(alertInfoGroup[1].Value.Replace("\\r\\n", "\n"));
            }
            if(loginResponse.IndexOf(@"window.location = ""loginteacher_action.jsp"";") == -1)
            {
                throw new ParsePageException("login_redirect");
            }

            //get iframe src
            try
            {
                HtmlDocument htmlDoc = new HtmlDocument();
                htmlDoc.LoadHtml(await getPageContent(homeUri));
                var iframeSrc = htmlDoc.DocumentNode.Descendants("iframe")/*MAGIC*/.First().Attributes["src"].Value;

                //login to learn.cic.tsinghua.edu.cn
                await getPageContent(iframeSrc);

                Debug.WriteLine("[login] successful");
                lastLogin = DateTime.Now;
            }
            catch (Exception)
            {
                throw new ParsePageException("find_cic_iframe");
            }

            return 0;
        }

        static string loginUri = "https://learn.tsinghua.edu.cn/MultiLanguage/lesson/teacher/loginteacher.jsp";
        static string homeUri = "http://learn.tsinghua.edu.cn/MultiLanguage/lesson/student/MyCourse.jsp?language=cn";
        static string hostedCalendarUrl = "http://lizy14.github.io/thuCalendar.json";

        static HttpClient httpClient = new HttpClient();
        static HttpResponseMessage httpResponse = new HttpResponseMessage();

        static async Task<string> getPageContent(string url)
        {
            //getPage
            httpResponse = await httpClient.GetAsync(new Uri(url));
            httpResponse.EnsureSuccessStatusCode();
            return await httpResponse.Content.ReadAsStringAsync();
        }

        static async Task<string> getHomeworkListPage(string courseId)
        {
            return await getPageContent($"http://learn.tsinghua.edu.cn/MultiLanguage/lesson/student/hom_wk_brw.jsp?course_id={courseId}");
        }
        static async Task<string> getHomeworkListPageNew(string courseId)
        {
            Int32 timestamp = (Int32)(DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1))).TotalMilliseconds;
            string url = $"http://learn.cic.tsinghua.edu.cn/b/myCourse/homework/list4Student/{courseId}/0?_={timestamp}";
            return await getPageContent(url);
        }
        static async Task<string> getCourseListPage()
        {
            return await getPageContent(homeUri);
        }
        static async Task<string> getCalendarPage()
        {
            return await getPageContent("http://learn.cic.tsinghua.edu.cn/b/myCourse/courseList/getCurrentTeachingWeek");
        }
        static List<Deadline> parseHomeworkListPage(string page)
        {
            try
            {
                HtmlDocument htmlDoc = new HtmlDocument();
                htmlDoc.LoadHtml(page);

                string _name, _due, _course;

                _course = htmlDoc.DocumentNode.Descendants("td")/*MAGIC*/.First().InnerText;
                _course = _course.Trim();
                _course = _course.Substring(6/*MAGIC*/);

                HtmlNode[] nodes = htmlDoc.DocumentNode.Descendants("tr")/*MAGIC*/.ToArray();


                List<Deadline> deadlines = new List<Deadline>();
                for (int i = 4/*MAGIC*/; i < nodes.Length - 1/*MAGIC*/; i++)
                {
                    HtmlNode node = nodes[i];

                    var tds = node.Descendants("td");

                    var _isFinished = (tds.ElementAt(3/*MAGIC*/).InnerText.Trim() == "已经提交");

                    _due = tds.ElementAt(2/*MAGIC*/).InnerText;
                    _name = node.Descendants("a")/*MAGIC*/.First().InnerText;

                    deadlines.Add(new Deadline
                    {
                        name = _name,
                        ddl = _due,
                        course = _course,
                        hasBeenFinished = _isFinished
                    });
                }
                return deadlines;
            }
            catch (Exception)
            {
                throw new ParsePageException("AssignmentList");
            }
            
        }
        static async Task<List<Deadline>> parseHomeworkListPageNew(string page)
        {

            List<Deadline> deadlines = new List<Deadline>();

            string _course = "";
            var root = JSON.parse<CourseAssignmentsRootobject>(page);
            foreach (var item in root.resultList)
            {
                var _isFinished = (item.courseHomeworkRecord.status != "0" /*MAGIC*/);

                var _dueTimeStamp = item.courseHomeworkInfo.endDate;
                var _dueDate = (new DateTime(1970, 1, 1, 0, 0, 0, 0)).ToLocalTime().AddMilliseconds(_dueTimeStamp).Date;
                string _due = $"{_dueDate.Year}-{_dueDate.Month}-{_dueDate.Day}";

                string _name = item.courseHomeworkInfo.title;
                string _courseId = item.courseHomeworkInfo.courseId;

                if (_course == "")
                    _course = _courseId;
                if (_course == _courseId)
                    foreach (var course in await DataAccess.getCourses())
                        if (course.id == _courseId)
                            _course = course.name;

                deadlines.Add(new Deadline
                {
                    name = _name,
                    ddl = _due,
                    course = _course,
                    hasBeenFinished = _isFinished
                });
            }
            return deadlines;
        }
        static List<Course> parseCourseList(string page)
        {
            try
            {
                List<Course> courses = new List<Course>();

                HtmlDocument htmlDoc = new HtmlDocument();
                htmlDoc.LoadHtml(page);
                var links = htmlDoc.DocumentNode.Descendants("table")/*MAGIC*/.Last()/*MAGIC*/.Descendants("a")/*MAGIC*/.ToArray();

                foreach (var link in links)
                {
                    string _name = link.InnerText.Trim();
                    string _url = link.Attributes["href"].Value;
                    var match = Regex.Match(_name, "(.+?)\\((\\d+)\\)\\((.+?)\\)");
                    string _semester = match.Groups[3].Value;
                    _name = match.Groups[1].Value;
                    bool _isNew = false;
                    string _id = "";

                    if (_url.StartsWith("http://learn.cic.tsinghua.edu.cn/"))
                    {
                        _isNew = true;
                        _id = Regex.Match(_url, "/([-\\d]+)").Groups[1].Value;
                    }
                    else
                    {
                        _isNew = false;
                        _id = Regex.Match(_url, "course_id=(\\d+)").Groups[1].Value;
                    }
                    courses.Add(new Course
                    {
                        name = _name,
                        isNew = _isNew,
                        id = _id,
                        semester = _semester
                    });
                }
                return courses;
            }
            catch (Exception)
            {
                throw new ParsePageException("CourseList");
            }
        }
        static CalendarRootObject parseCalendarPage(string page)
        {
            return JSON.parse<CalendarRootObject>(page);
        }
    }

    //JSON parser wrapped
    public class JSON
    {
        public static T parse<T>(string jsonString)
        {
            try
            {
                using (var ms = new MemoryStream(Encoding.UTF8.GetBytes(jsonString)))
                {
                    return (T)new DataContractJsonSerializer(typeof(T)).ReadObject(ms);
                }
            }
            catch (Exception)
            {
                throw new ParsePageException("JSON "+typeof(T).ToString());
            }
        }
        public static string stringify(object jsonObject)
        {
            using (var ms = new MemoryStream())
            {
                new DataContractJsonSerializer(jsonObject.GetType()).WriteObject(ms, jsonObject);
                return Encoding.UTF8.GetString(ms.ToArray());
            }
        }
    }
}