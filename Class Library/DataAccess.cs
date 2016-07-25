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

    static public class DataAccess
    {
        static ApplicationDataContainer localSettings = ApplicationData.Current.LocalSettings;
        static List<Course> courses = null;
        static List<Deadline> deadlines = null;
        static Calendar calendar = null;

        static public async Task<int> update()
        {
            return 0;
        }
        public static async Task<List<Course>> getCourses(bool forceRemote = false)
        {
            if (!forceRemote)
            {
                //try memory
                if (courses != null)
                {
                    Debug.WriteLine("[getCourses] Returning memory");
                    return courses;
                }

                //try localSettings
                var localCourses = localSettings.Values["courses"];
                if (localCourses != null)
                {
                    Debug.WriteLine("[getCourses] Returning local settings");
                    courses = JSON.parse<List<Course>>((string)localCourses);
                    return courses;
                }
            }


            //fetch from remote
            await login();
            var _courses = parseCourseList(await getCourseListPage());
            courses = _courses;
            localSettings.Values["courses"] = JSON.stringify(_courses);
            Debug.WriteLine("[getCourses] Returning remote");
            return courses;
        }

        public static async Task<Calendar> getCalendar(bool forceRemote = false)
        {
            if(forceRemote == false)
            {
                Calendar __calendar = null;
                //try memory
                if (calendar != null)
                {
                    Debug.WriteLine("[getCalendar] memory");
                    __calendar = calendar;
                }

                //try localSettings
                var localJSON = localSettings.Values["calendar"];
                if (localJSON != null)
                {
                    Debug.WriteLine("[getCalendar] local settings");
                    __calendar = JSON.parse<Calendar>((string)localJSON);
                }

                if(__calendar != null)
                {
                    if(DateTime.Parse(__calendar.currentSemester.endDate + " 23:59") > DateTime.Now)
                    {
                        Debug.WriteLine("[getCalendar] cache dirty");
                        //force a remote update
                        try
                        {

                            return await getCalendar(true);
                        }
                        catch(Exception)
                        {
                            var fallback = __calendar;
                            fallback.currentSemester.semesterName  = __calendar.nextSemester.semesterName;
                            fallback.currentSemester.semesterEname = __calendar.nextSemester.semesterName;
                            fallback.currentSemester.id            = __calendar.nextSemester.id;
                            fallback.currentSemester.startDate     = __calendar.nextSemester.startDate;
                            fallback.currentSemester.semesterName  = __calendar.nextSemester.semesterName;
                            fallback.currentTeachingWeek.calculateWeekName(__calendar.nextSemester.startDate);
                            Debug.WriteLine("[getCalendar] fallen back, returning");
                            return fallback;
                        }
                    }
                    Debug.WriteLine("[getCalendar] cache good, returning");
                    return __calendar;
                }
            }
            

            //fetch from remote
            await login();
            var _calendar = parseCalendarPage(await getCalendarPage());
            calendar = _calendar;
            localSettings.Values["calendar"] = JSON.stringify(calendar);
            Debug.WriteLine("[getCalendar] remote returning");
            return calendar;
        }

        static public async Task<List<Deadline>> getDeadlines()
        {

            var assignments = await getAllDeadlines();
            var result = (from assignment in assignments
                          where assignment.hasBeenFinished == false
                          orderby ((DateTime.Parse(assignment.ddl) - DateTime.Now).TotalDays)
                          select assignment);
            return result.Take(5).ToList();
        }

        static public async Task<List<Deadline>> getAllDeadlines(bool forceRemote = false)
        {
            if (!forceRemote)
            {
                //try session memory
                if (deadlines != null)
                {
                    Debug.WriteLine("[getAllDeadlines] Returning memory");
                    return deadlines;
                }


                //try localSettings
                var local = localSettings.Values["deadlines"];
                if (local != null)
                {
                    Debug.WriteLine("[getAllDeadlines] Returning local settings");
                    return JSON.parse<List<Deadline>>((string)local);
                }
            }

            //fetch from remote


            await login();
            var courses = await getCourses();

            List<Deadline> _deadlines = new List<Deadline>();

            foreach (var course in courses)
            {
                Debug.WriteLine("[getAllDeadlines] Remote " + course.name);
                var id = course.id;
                List<Deadline> __deadlines;
                if (course.isNew)
                    __deadlines = parseHomeworkListPageNew(await getHomeworkListPageNew(id));
                else
                    __deadlines = parseHomeworkListPage(await getHomeworkListPage(id));
                _deadlines = _deadlines.Concat(__deadlines).ToList();
            }


            deadlines = _deadlines;
            localSettings.Values["deadlines"] = JSON.stringify(_deadlines);
            Debug.WriteLine("[getAllDeadlines] Returning remote");

            return _deadlines;
        }


        //Remote Access

        static string loginUri = "https://learn.tsinghua.edu.cn/MultiLanguage/lesson/teacher/loginteacher.jsp";
        static string homeUri = "http://learn.tsinghua.edu.cn/MultiLanguage/lesson/student/MyCourse.jsp?language=cn";

        static HttpClient httpClient;
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

        static DateTime lastLogin = DateTime.MinValue;
        static int LOGIN_TIMEOUT_MINUTES = 5;
        static async Task<int> login()
        {

            //check for last login
            if ((DateTime.Now - lastLogin).TotalMinutes < LOGIN_TIMEOUT_MINUTES)
            {
                Debug.WriteLine("[login] reusing recent session");
                return 2;
            }

            Debug.WriteLine("[login] begin");

            string username = localSettings.Values["username"].ToString();
            string password = localSettings.Values["password"].ToString();

            httpClient = new HttpClient();

            //login to learn.tsinghua.edu.cn
            HttpStringContent stringContent = new HttpStringContent(
                $"leixin1=student&userid={username}&userpass={password}",
                Windows.Storage.Streams.UnicodeEncoding.Utf8,
                "application/x-www-form-urlencoded");

            httpResponse = await httpClient.PostAsync(new Uri(loginUri), stringContent);
            httpResponse.EnsureSuccessStatusCode();
            await httpResponse.Content.ReadAsStringAsync();

            //get iframe src
            HtmlDocument htmlDoc = new HtmlDocument();
            htmlDoc.LoadHtml(await getPageContent(homeUri));
            var iframeSrc = htmlDoc.DocumentNode.Descendants("iframe")/*MAGIC*/.First().Attributes["src"].Value;

            //login to learn.cic.tsinghua.edu.cn
            await getPageContent(iframeSrc);

            Debug.WriteLine("[login] successful");
            lastLogin = DateTime.Now;

            return 0;
        }

        static List<Deadline> parseHomeworkListPage(string page)
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

        static List<Deadline> parseHomeworkListPageNew(string page)
        {

            List<Deadline> deadlines = new List<Deadline>();

            string _course = "";
            var root = JSON.parse<Rootobject>(page);
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
                    foreach (var course in courses)
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

        static Calendar parseCalendarPage(string page)
        {
            return JSON.parse<Calendar>(page);
        }


    }

    public class JSON
    {

        public static T parse<T>(string jsonString)
        {
            using (var ms = new MemoryStream(Encoding.UTF8.GetBytes(jsonString)))
            {
                return (T)new DataContractJsonSerializer(typeof(T)).ReadObject(ms);
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

    public class Rootobject
    {
        public string message { get; set; }
        public Resultlist[] resultList { get; set; }
    }
    public class Resultlist
    {
        public Coursehomeworkrecord courseHomeworkRecord { get; set; }
        public Coursehomeworkinfo courseHomeworkInfo { get; set; }
    }
    public class Coursehomeworkrecord
    {
        public int seqId { get; set; }
        public string studentId { get; set; }
        public string teacherId { get; set; }
        public string homewkId { get; set; }
        public long? regDate { get; set; }
        public string homewkDetail { get; set; }
        public Resourcesmappingbyhomewkaffix resourcesMappingByHomewkAffix { get; set; }
        public object replyDetail { get; set; }
        public object resourcesMappingByReplyAffix { get; set; }
        public int? mark { get; set; }
        public long? replyDate { get; set; }
        public object iffine { get; set; }
        public string status { get; set; }
        public string ifDelay { get; set; }
        public string gradeUser { get; set; }
        public int groupId { get; set; }
        public object groupName { get; set; }
    }
    public class Resourcesmappingbyhomewkaffix
    {
        public string fileId { get; set; }
        public string resourcesId { get; set; }
        public int diskId { get; set; }
        public long regDate { get; set; }
        public string fileName { get; set; }
        public int browseNum { get; set; }
        public int downloadNum { get; set; }
        public string extension { get; set; }
        public string fileSize { get; set; }
        public string courseId { get; set; }
        public object playUrl { get; set; }
        public object downloadUrl { get; set; }
        public int resourcesStatus { get; set; }
        public string userCode { get; set; }
    }
    public class Coursehomeworkinfo
    {
        public int homewkId { get; set; }
        public long? regDate { get; set; }
        public long beginDate { get; set; }
        public long endDate { get; set; }
        public int teachingWeekId { get; set; }
        public string title { get; set; }
        public string detail { get; set; }
        public object homewkAffix { get; set; }
        public object homewkAffixFilename { get; set; }
        public object homewkIndex { get; set; }
        public object answerDetail { get; set; }
        public object answerLink { get; set; }
        public object answerLinkFilename { get; set; }
        public object answerDate { get; set; }
        public string courseId { get; set; }
        public object homewkGroupNum { get; set; }
        public string courseSource { get; set; }
        public int noteId { get; set; }
        public object courseKnowledge { get; set; }
        public int weiJiao { get; set; }
        public int yiJiao { get; set; }
        public int yiYue { get; set; }
        public int yiPi { get; set; }
        public int jiaoed { get; set; }
    }


    public class Calendar
    {
        public Currentteachingweek currentTeachingWeek { get; set; }
        public Currentsemester currentSemester { get; set; }
        public string currentDate { get; set; }
        public Nextsemester nextSemester { get; set; }
    }

    public class Currentteachingweek
    {
        public int teachingWeekId { get; set; }
        public string weekName { get; set; }
        public string beginDate { get; set; }
        public string endDate { get; set; }
        public string semesterId { get; set; }
        public void calculateWeekName(string semesterStartDate)
        {
            var semesterStart = DateTime.Parse(semesterStartDate);
            var delta = DateTime.Now - semesterStart;
            var days = delta.TotalDays;

            /* 0 ~  6.9 -> 1
               7 ~ 13.9 -> 2 */
            weekName = (Math.Floor(days / 7) + 1).ToString();
        }
    }

    public class Currentsemester
    {
        public string id { get; set; }
        public string semesterName { get; set; }
        public string startDate { get; set; }
        public string endDate { get; set; }
        public string semesterEname { get; set; }
    }

    public class Nextsemester
    {
        public string id { get; set; }
        public string semesterName { get; set; }
        public string startDate { get; set; }
        public object endDate { get; set; }
        public string semesterEname { get; set; }
    }

}
