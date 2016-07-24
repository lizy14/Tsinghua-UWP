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

namespace TsinghuaUWP
{

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

    static public class Remote
    {

        static List<Course> courses = null;

        static string loginUri = "https://learn.tsinghua.edu.cn/MultiLanguage/lesson/teacher/loginteacher.jsp";
        static string homeUri = "http://learn.tsinghua.edu.cn/MultiLanguage/lesson/student/MyCourse.jsp?language=cn";
        static HttpClient httpClient = new HttpClient();
        static HttpResponseMessage httpResponse = new HttpResponseMessage();
        static string httpResponseBody = "";


        
        static async Task<int> login()
        {
            string username = "lizy14";
            string password = "i won't commit my password";

            //login to learn.tsinghua.edu.cn
            HttpStringContent stringContent = new HttpStringContent(
                $"leixin1=student&userid={username}&userpass={password}",
                Windows.Storage.Streams.UnicodeEncoding.Utf8,
                "application/x-www-form-urlencoded");

            httpResponse = await httpClient.PostAsync(new Uri(loginUri), stringContent);
            httpResponse.EnsureSuccessStatusCode();
            httpResponseBody = await httpResponse.Content.ReadAsStringAsync();

            //get iframe src
            HtmlDocument htmlDoc = new HtmlDocument();
            htmlDoc.LoadHtml(await getPageContent(homeUri));
            var iframeSrc = htmlDoc.DocumentNode.Descendants("iframe")/*MAGIC*/.First().Attributes["src"].Value;

            //login to learn.cic.tsinghua.edu.cn
            await getPageContent(iframeSrc);

            return 0;
        }

        static List<Deadline> parseHomeworkList(string page)
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
                    due = _due,
                    course = _course,
                    hasBeenFinished = _isFinished
                });
            }
            return deadlines;
        }

        static List<Deadline> parseHomeworkListNew(string page)
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
                if(_course == _courseId)
                    foreach(var course in courses)
                        if (course.id == _courseId)
                            _course = course.name;

                deadlines.Add(new Deadline
                {
                    name = _name,
                    due = _due,
                    course = _course,
                    hasBeenFinished = _isFinished
                });
            }
            return deadlines;
        }

        static async Task<string> getPageContent(string url)
        {
            //getPage
            httpResponse = await httpClient.GetAsync(new Uri(url));
            httpResponse.EnsureSuccessStatusCode();
            httpResponseBody = await httpResponse.Content.ReadAsStringAsync();
            return httpResponseBody;
        }

        static async Task<string> getHomeworkList(string courseId)
        {
            return await getPageContent($"http://learn.tsinghua.edu.cn/MultiLanguage/lesson/student/hom_wk_brw.jsp?course_id={courseId}");
        }

        static async Task<string> getHomeworkListNew(string courseId)
        {
            Int32 timestamp = (Int32)(DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1))).TotalMilliseconds;
            string url = $"http://learn.cic.tsinghua.edu.cn/b/myCourse/homework/list4Student/{courseId}/0?_={timestamp}";
            return await getPageContent(url);
        }

        static async Task<string> getCourses()
        {
            return await getPageContent(homeUri);
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


        static public async Task<Deadline> getDeadline()
        {

            await login();

            if (courses == null)
                courses = parseCourseList(await getCourses());

            List<Deadline> deadlines = new List<Deadline>();

            foreach (var course in courses)
            {
                var id = course.id;
                List<Deadline> _deadlines;
                if (course.isNew)
                    _deadlines = parseHomeworkListNew(await getHomeworkListNew(id));
                else
                    _deadlines = parseHomeworkList(await getHomeworkList(id));
                deadlines = deadlines.Concat(_deadlines).ToList();
            }


            try { }
            catch (Exception ex)
            {
                httpResponseBody = "Error: " + ex.HResult.ToString("X") + " Message: " + ex.Message;
            }

            deadlines.Sort();

            return deadlines.Last();
        }
    }
}
