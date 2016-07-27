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
        





        // exposes access to remote objects

        public static async Task<Timetable> getRemoteTimetable()
        {

            await login();


            HttpStringContent stringContent = new HttpStringContent(
                $"appId = ALL_ZHJW", Windows.Storage.Streams.UnicodeEncoding.Utf8, "application/x-www-form-urlencoded");
            httpResponse = await m_httpClient.PostAsync(new Uri("http://learn.cic.tsinghua.edu.cn:80/gnt"), stringContent);
            httpResponse.EnsureSuccessStatusCode();
            var ticket = await httpResponse.Content.ReadAsStringAsync();


            Int32 timestamp = (Int32)(DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1))).TotalMilliseconds;
            var year_ago = DateTime.Now.AddYears(-1).ToString("yyyyMMdd");
            var year_later = DateTime.Now.AddYears(+1).ToString("yyyyMMdd");

            try
            {
                var zhjw = await getPageContent($"http://zhjw.cic.tsinghua.edu.cn/j_acegi_login.do?url=/&ticket={ticket}");
            }
            catch (System.Runtime.InteropServices.COMException e)
            {
                if (e.Message.IndexOf("403") == -1)
                    throw e;
                Debug.WriteLine("[getRemoteTimetable] outside campus network");

                //connect via sslvpn
                await loginSSLVPN();
                var ticketPage = await getPageContent(
                    $"https://sslvpn.tsinghua.edu.cn/,DanaInfo=zhjw.cic.tsinghua.edu.cn+j_acegi_login.do?url=/&ticket={ticket}");
                string pageSslvpn = await getPageContent(
                    $"https://sslvpn.tsinghua.edu.cn/,DanaInfo=zhjw.cic.tsinghua.edu.cn,CT=js+jxmh.do?m=bks_jxrl_all&p_start_date={year_ago}&p_end_date={year_later}&jsoncallback=_");
                logoutSSLVPN();
                return parseTimetablePage(pageSslvpn);
            }

            //connect directly
            string page = await getPageContent(
                $"http://zhjw.cic.tsinghua.edu.cn/jxmh.do?m=bks_jxrl_all&p_start_date={year_ago}&p_end_date={year_later}&jsoncallback=_&_={timestamp}");
            return parseTimetablePage(page);

        }
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
            var _remoteCalendar = parseSemestersPage(await getCalendarPage());
            return new Semesters
            {
                currentSemester = _remoteCalendar.currentSemester,
                nextSemester = _remoteCalendar.nextSemester
            };
        }
        
        public static async Task validateCredential(string username, string password) // throws LoginException if fail
        {
            await login(false, username, password);
        }







        // handle cookies with m_httpClient

        static async Task<int> login(bool useLocalSettings = true, string username = "", string password = "")
        {
            Debug.WriteLine("[login] begin");


            //retrieve username and password
            if (useLocalSettings)
            {
                if (DataAccess.credentialAbsent()) {
                    throw new LoginException("没有指定用户名和密码");
                }
                username = DataAccess.getLocalSettings()["username"].ToString();

                if (username == "__anonymous"){
                    throw new LoginException("没有指定用户名和密码");
                }

                var vault = new Windows.Security.Credentials.PasswordVault();
                password = vault.Retrieve("Tsinghua_Learn_Website", username).Password;
            }


            //check for last login
            if ((DateTime.Now - lastLogin).TotalMinutes < LOGIN_TIMEOUT_MINUTES
                && lastLoginUsername == username)
            {
                Debug.WriteLine("[login] reuses recent session");
                return 2;
            }


            //login to learn.tsinghua.edu.cn
            HttpStringContent stringContent = new HttpStringContent(
                $"leixin1=student&userid={username}&userpass={password}",
                Windows.Storage.Streams.UnicodeEncoding.Utf8,
                "application/x-www-form-urlencoded");

            httpResponse = await m_httpClient.PostAsync(new Uri(loginUri), stringContent);
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
            HtmlDocument htmlDoc = new HtmlDocument();
            htmlDoc.LoadHtml(await getPageContent(courseListUrl));

            string iframeSrc;
            try {
                iframeSrc = htmlDoc.DocumentNode.Descendants("iframe")/*MAGIC*/.First().Attributes["src"].Value;
            } catch (Exception) {
                throw new ParsePageException("find_cic_iframe");
            }


            //login to learn.cic.tsinghua.edu.cn
            await getPageContent(iframeSrc);


            Debug.WriteLine("[login] successful");
            lastLogin = DateTime.Now;


            return 0;
        }
        static async Task logoutSSLVPN()
        {
            await getPageContent("https://sslvpn.tsinghua.edu.cn/dana-na/auth/logout.cgi");
            Debug.WriteLine("[loginSSLVPN] finish");
        }
        static async Task<int> loginSSLVPN()
        {

            Debug.WriteLine("[loginSSLVPN] start");

            //retrieve username and password
            if (DataAccess.credentialAbsent()){
                throw new LoginException("没有指定用户名和密码");
            }
            var username = DataAccess.getLocalSettings()["username"].ToString();

            var vault = new Windows.Security.Credentials.PasswordVault();
            var password = vault.Retrieve("Tsinghua_Learn_Website", username).Password;


            //login to sslvpn.tsinghua.edu.cn
            HttpStringContent stringContent = new HttpStringContent(
                $"tz_offset=480&username={username/*should be numeral ID*/}&password={password}&realm=ldap&btnSubmit=登录",
                Windows.Storage.Streams.UnicodeEncoding.Utf8,
                "application/x-www-form-urlencoded");

            httpResponse = await m_httpClient.PostAsync(new Uri(loginSslvpnUri), stringContent);
            httpResponse.EnsureSuccessStatusCode();
            var loginResponse = await httpResponse.Content.ReadAsStringAsync();


            //get xsauth token
            var xsauthGroups = Regex.Match(loginResponse, @"name=""xsauth"" value=""([^""]+)""").Groups;
            if (xsauthGroups.Count < 2){
                //TODO: create error message on login failure
                throw new ParsePageException("find_xsauth_from_sslvpn");
            }
            var xsauth = xsauthGroups[1];

            //second step, invoking xsauth token
            var time = (Int32)(DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1))).TotalSeconds; 
            stringContent = new HttpStringContent(
                $"xsauth={xsauth}&tz_offset=480&clienttime={time}&url=&activex_enabled=0&java_enabled=0&power_user=0&grab=1&browserproxy=&browsertype=&browserproxysettings=&check=yes",
                Windows.Storage.Streams.UnicodeEncoding.Utf8,
                "application/x-www-form-urlencoded");

            httpResponse = await m_httpClient.PostAsync(new Uri(loginSslvpnCheckUri), stringContent);
            httpResponse.EnsureSuccessStatusCode();
            loginResponse = await httpResponse.Content.ReadAsStringAsync();

            Debug.WriteLine("[loginSSLVPN] finish");
            return 0;
        }

        static DateTime lastLogin = DateTime.MinValue;
        static string lastLoginUsername = "";
        static int LOGIN_TIMEOUT_MINUTES = 5;

        static string loginSslvpnUri = "https://sslvpn.tsinghua.edu.cn/dana-na/auth/url_default/login.cgi";
        static string loginSslvpnCheckUri = "https://sslvpn.tsinghua.edu.cn/dana/home/starter0.cgi";
        static string loginUri = "https://learn.tsinghua.edu.cn/MultiLanguage/lesson/teacher/loginteacher.jsp";






        // remote object URLs and wrappers

        static string courseListUrl = "http://learn.tsinghua.edu.cn/MultiLanguage/lesson/student/MyCourse.jsp?language=cn";
        static string hostedCalendarUrl = "http://lizy14.github.io/thuCalendar.json";
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
            return await getPageContent(courseListUrl);
        }
        static async Task<string> getCalendarPage()
        {
            return await getPageContent("http://learn.cic.tsinghua.edu.cn/b/myCourse/courseList/getCurrentTeachingWeek");
        }








        // parse HTML or JSON, and return corresponding Object

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
        static SemestersRootObject parseSemestersPage(string page)
        {
            return JSON.parse<SemestersRootObject>(page);
        }
        static Timetable parseTimetablePage(string page)
        {
            if (page.Length < "_([])".Length)
                throw new ParsePageException("timetable_javascript");
            string json = page.Substring(2, page.Length - 3);
            return JSON.parse<Timetable>(json);
        }







        // utilities

        static HttpClient m_httpClient = new HttpClient();
        static HttpResponseMessage httpResponse = new HttpResponseMessage();
        static async Task<string> getPageContent(string url)
        {
            //getPage
            httpResponse = await m_httpClient.GetAsync(new Uri(url));
            httpResponse.EnsureSuccessStatusCode();
            return await httpResponse.Content.ReadAsStringAsync();
        }

    }

    // wrapped JSON parser
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