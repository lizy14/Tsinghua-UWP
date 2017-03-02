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
using System.Net;

namespace TsinghuaUWP {
    public static class Remote
    {






        // exposes access to remote objects

        public static async Task<Timetable> getRemoteTimetable()
        {
            Debug.WriteLine("[getRemoteTimetable] start");
            await login();


            var ticket = await POST(
                "http://learn.cic.tsinghua.edu.cn:80/gnt",
                "appId=ALL_ZHJW");

            //await 十１ｓ(); //cross-domain tickets needs some time to take effect

            bool outside_campus_network = false;
            try
            {
                var zhjw = await GET(
                    $"http://zhjw.cic.tsinghua.edu.cn/j_acegi_login.do?url=/&ticket={ticket}");
            }
            catch (System.Runtime.InteropServices.COMException e)
            {
                if (e.Message.IndexOf("403") == -1)
                    throw e;
                Debug.WriteLine("[getRemoteTimetable] outside campus network");
                //throw new NeedCampusNetworkException();

                outside_campus_network = true;

            }



            if (outside_campus_network)
            {
                //connect via sslvpn
                await logoutSSLVPN();
                await loginSSLVPN();

                await login();

                ticket = await POST(
                    "http://learn.cic.tsinghua.edu.cn:80/gnt",
                    "appId=ALL_ZHJW");

                await 十１ｓ();

                var ticketPage = await GET(
                    $"https://sslvpn.tsinghua.edu.cn/,DanaInfo=zhjw.cic.tsinghua.edu.cn+j_acegi_login.do?url=/&ticket={ticket}");

                Timetable timetable = new Timetable();

                for (int i = -6; i <= 4; i += 2)
                {
                    string page;
                    try
                    {
                        page = await get_calendar_sslvpn_page(
                            DateTime.Now.AddMonths(i).AddDays(1).ToString("yyyyMMdd"),
                            DateTime.Now.AddMonths(i + 2).ToString("yyyyMMdd")
                            );
                    }
                    catch (Exception)
                    {
                        page = await get_calendar_sslvpn_page(
                            DateTime.Now.AddMonths(i).AddDays(1).ToString("yyyyMMdd"),
                            DateTime.Now.AddMonths(i + 2).ToString("yyyyMMdd")
                            );
                    }
                    var set_to_be_appended = parseTimetablePage(page);
                    foreach (var _____ in set_to_be_appended)
                    {
                        timetable.Add(_____);
                    }
                }

                logoutSSLVPN();

                Debug.WriteLine("[getRemoteTimetable] returning sslvpn");
                return timetable;
            }
            else
            { //TODO: duplicate code

                //connect directly

                Timetable timetable = new Timetable();
                for (int i = -6; i <= 4; i += 2)
                {
                    string page;
                    try
                    {
                        page = await get_calendar_page(
                            DateTime.Now.AddMonths(i).AddDays(1).ToString("yyyyMMdd"),
                            DateTime.Now.AddMonths(i + 2).ToString("yyyyMMdd")
                            );
                    }
                    catch (Exception)
                    {
                        page = await get_calendar_page(
                            DateTime.Now.AddMonths(i).AddDays(1).ToString("yyyyMMdd"),
                            DateTime.Now.AddMonths(i + 2).ToString("yyyyMMdd")
                            );
                    }
                    var set_to_be_appended = parseTimetablePage(page);
                    foreach (var _____ in set_to_be_appended)
                    {
                        timetable.Add(_____);
                    }
                }

                Debug.WriteLine("[getRemoteTimetable] returning direct");
                return timetable;
            }
        }



        static async Task<string> get_calendar_page(string starting_date, string ending_date)
        {
            Debug.WriteLine($"[get_calendar_page] {starting_date}-{ending_date}");
            var stamp = (long)UnixTime().TotalMilliseconds;
            return await GET(
                $"http://zhjw.cic.tsinghua.edu.cn/jxmh.do?m=bks_jxrl_all&p_start_date={starting_date}&p_end_date={ending_date}&jsoncallback=_&_={stamp}");
        }
        static async Task<string> get_calendar_sslvpn_page(string starting_date, string ending_date)
        {
            var stamp = (long)UnixTime().TotalMilliseconds;
            return await GET(
                    $"https://sslvpn.tsinghua.edu.cn/,DanaInfo=zhjw.cic.tsinghua.edu.cn,CT=js+jxmh.do?m=bks_jxrl_all&p_start_date={starting_date}&p_end_date={ending_date}&jsoncallback=_&_={stamp}");
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
            return JSON.parse<Semesters>(await GET(hostedCalendarUrl));
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
            if (username == "233333")
                return;
            await login(false, username, password);
        }







        // handle cookies with m_httpClient
        private static bool occupied = false;
        public static async Task<int> login(bool useLocalSettings = true, string username = "", string password = "")
        {
            Debug.WriteLine("[login] begin");

            //retrieve username and password
            if (useLocalSettings)
            {
                if (DataAccess.credentialAbsent())
                {
                    throw new LoginException("没有指定用户名和密码");
                }
                username = DataAccess.getLocalSettings()["username"].ToString();

                if (username == "__anonymous")
                {
                    throw new LoginException("没有指定用户名和密码");
                }

                var vault = new Windows.Security.Credentials.PasswordVault();
                password = vault.Retrieve("Tsinghua_Learn_Website", username).Password;
            }

            while (occupied)
            {
                await 十１ｓ(.1);
            }

            occupied = true;
            //check for last login
            if ((DateTime.Now - lastLogin).TotalMinutes < LOGIN_TIMEOUT_MINUTES
                && lastLoginUsername == username)
            {
                Debug.WriteLine("[login] reuses recent session");
                occupied = false;
                return 2;
            }


            try
            {
                string loginResponse;

                //login to learn.tsinghua.edu.cn

                loginResponse = await POST(
                    loginUri,
                    $"leixin1=student&userid={username}&userpass={password}");

                //check if successful
                var alertInfoGroup = Regex.Match(loginResponse, @"window.alert\(""(.+)""\);").Groups;
                if (alertInfoGroup.Count > 1)
                {
                    throw new LoginException(alertInfoGroup[1].Value.Replace("\\r\\n", "\n"));
                }
                if (loginResponse.IndexOf(@"window.location = ""loginteacher_action.jsp"";") == -1)
                {
                    throw new ParsePageException("login_redirect");
                }

                //get iframe src
                HtmlDocument htmlDoc = new HtmlDocument();
                htmlDoc.LoadHtml(await GET(courseListUrl));

                string iframeSrc;
                try
                {
                    iframeSrc = htmlDoc.DocumentNode.Descendants("iframe")/*MAGIC*/.First().Attributes["src"].Value;
                }
                catch (Exception)
                {
                    throw new ParsePageException("find_cic_iframe");
                }


                //login to learn.cic.tsinghua.edu.cn
                await 十１ｓ();
                await GET(iframeSrc);
            }
            catch (Exception e)
            {
                occupied = false;
                Debug.WriteLine("[login] unsuccessful");
                throw e;
            }

            Debug.WriteLine("[login] successful");

            lastLogin = DateTime.Now;
            lastLoginUsername = username;

            occupied = false;

            return 0;
        }

        private static async Task logoutSSLVPN()
        {
            await GET(logoutSslvpnUrl);
            Debug.WriteLine("[logoutSSLVPN] finish");
        }

        private static async Task<int> loginSSLVPN()
        {

            Debug.WriteLine("[loginSSLVPN] start");

            //retrieve username and password
            if (DataAccess.credentialAbsent())
            {
                throw new LoginException("没有指定用户名和密码");
            }
            var username = DataAccess.getLocalSettings()["username"].ToString();

            var vault = new Windows.Security.Credentials.PasswordVault();
            var password = vault.Retrieve("Tsinghua_Learn_Website", username).Password;


            //login to sslvpn.tsinghua.edu.cn
            var loginResponse = await POST(
                loginSslvpnUri,
                $"tz_offset=480&username={username/*should be numeral ID*/}&password={password}&realm=ldap&btnSubmit=登录");


            //another sslvpn session exist?
            if (loginResponse.IndexOf("btnContinue") != -1)
            {
                Debug.WriteLine("[loginSSLVPN] another sslvpn session exist");

                HtmlDocument htmlDoc = new HtmlDocument();
                htmlDoc.LoadHtml(loginResponse);
                string formDataStr = htmlDoc.GetElementbyId("DSIDFormDataStr").Attributes["value"].Value;

                loginResponse = await POST(
                    loginSslvpnUri,
                    $"btnContinue=继续会话&FormDataStr={Uri.EscapeDataString(formDataStr)}");
            }

            //get xauth token
            var xsauthGroups = Regex.Match(loginResponse, @"name=""xsauth"" value=""([^""]+)""").Groups;
            if (xsauthGroups.Count < 2)
            {
                throw new ParsePageException("find_xsauth_from_sslvpn");
            }
            var xsauth = xsauthGroups[1];

            //second step, invoking xsauth token
            var timestamp = UnixTime().TotalSeconds;

            loginResponse = await POST(
                loginSslvpnCheckUri,
                $"xsauth={xsauth}&tz_offset=480&clienttime={timestamp}&url=&activex_enabled=0&java_enabled=0&power_user=0&grab=1&browserproxy=&browsertype=&browserproxysettings=&check=yes");



            Debug.WriteLine("[loginSSLVPN] finish");
            return 0;
        }

        private static DateTime lastLogin = DateTime.MinValue;
        private static string lastLoginUsername = "";
        private static int LOGIN_TIMEOUT_MINUTES = 5;

        private static string loginSslvpnUri = "https://sslvpn.tsinghua.edu.cn/dana-na/auth/url_default/login.cgi";
        private static string logoutSslvpnUrl = "https://sslvpn.tsinghua.edu.cn/dana-na/auth/logout.cgi";
        private static string loginSslvpnCheckUri = "https://sslvpn.tsinghua.edu.cn/dana/home/starter0.cgi";
        private static string loginSslvpnCheckUriCheck = "https://sslvpn.tsinghua.edu.cn/dana/home/starter0.cgi?check=yes";
        private static string loginUri = "https://learn.tsinghua.edu.cn/MultiLanguage/lesson/teacher/loginteacher.jsp";






        // remote object URLs and wrappers

        private static string courseListUrl = "http://learn.tsinghua.edu.cn/MultiLanguage/lesson/student/MyCourse.jsp?language=cn";
        private static string hostedCalendarUrl = "http://static.nullspace.cn/thuCalendar.json";
        public static string helpUrl = "http://static.nullspace.cn/thuUwpHelp.html";

        public static async Task<string> getHomeworkListPage(string courseId)
        {
            return await GET($"http://learn.tsinghua.edu.cn/MultiLanguage/lesson/student/hom_wk_brw.jsp?course_id={courseId}");
        }
        public static async Task<string> getAncListPage(string courseId)
        {
            return await GET($"http://learn.tsinghua.edu.cn/MultiLanguage/public/bbs/getnoteid_student.jsp?course_id={courseId}");
        }

        public static async Task<string> getHomeworkListPageNew(string courseId)
        {
            var timestamp = UnixTime().TotalMilliseconds;
            string url = $"http://learn.cic.tsinghua.edu.cn/b/myCourse/homework/list4Student/{courseId}/0?_={timestamp}";
            return await GET(url);
        }

        public static async Task<string> getCourseListPage()
        {
            return await GET(courseListUrl);
        }

        private static async Task<string> getCalendarPage()
        {
            return await GET("http://learn.cic.tsinghua.edu.cn/b/myCourse/courseList/getCurrentTeachingWeek");
        }

        //Anc in cic
        public static async Task<string> getNewAncPage(string courseId)
        {
            await login();
            // return await GET("http://learn.cic.tsinghua.edu.cn/b/myCourse/notice/listForStudent/{courseId}");
            var http = new HttpClient();
            var Uri= new Uri($"http://learn.cic.tsinghua.edu.cn/b/myCourse/notice/listForStudent/{courseId}");
            var response = await http.GetAsync(Uri);
            var result = await response.Content.ReadAsStringAsync();

            return result;


        }







        // parse HTML or JSON, and return corresponding Object

        public static List<Deadline> parseHomeworkListPage(string page)
        {
            try
            {
                HtmlDocument htmlDoc = new HtmlDocument();
                htmlDoc.LoadHtml(page);

                string _name, _due, _course;

                _course = htmlDoc.DocumentNode.Descendants("td")/*MAGIC*/.First().InnerText;
                _course = _course.Trim();
                _course = _course.Substring(6/*MAGIC*/);
                _course = WebUtility.HtmlDecode(_course);

                HtmlNode[] nodes = htmlDoc.DocumentNode.Descendants("tr")/*MAGIC*/.ToArray();


                List<Deadline> deadlines = new List<Deadline>();
                for (int i = 4/*MAGIC*/; i < nodes.Length - 1/*MAGIC*/; i++)
                {
                    HtmlNode node = nodes[i];

                    var tds = node.Descendants("td");

                    var _isFinished = (tds.ElementAt(3/*MAGIC*/).InnerText.Trim() == "已经提交");

                    _due = tds.ElementAt(2/*MAGIC*/).InnerText;

                    var link_to_detail = node.Descendants("a")/*MAGIC*/.First();
                    _name = link_to_detail.InnerText;
                    _name = WebUtility.HtmlDecode(_name);

                    var _href = link_to_detail.Attributes["href"].Value;
                    var _id = Regex.Match(_href, @"[^_]id=(\d+)").Groups[1].Value;

                    deadlines.Add(new Deadline
                    {
                        name = _name,
                        ddl = _due,
                        course = _course,
                        hasBeenFinished = _isFinished,
                        id = "@" + _id
                    });
                }
                return deadlines;
            }
            catch (Exception)
            {
                throw new ParsePageException("AssignmentList");
            }

        }

        public static async Task<List<Deadline>> parseHomeworkListPageNew(string page)
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
                {
                    foreach (var course in await DataAccess.getCourses())
                    {
                        if (course.id == _courseId)
                            _course = course.name;
                    }
                }

                _course = WebUtility.HtmlDecode(_course);
                _name = WebUtility.HtmlDecode(_name);

                deadlines.Add(new Deadline
                {
                    name = _name,
                    ddl = _due,
                    course = _course,
                    hasBeenFinished = _isFinished,
                    id = "_" + item.courseHomeworkInfo.homewkId
                });
            }
            return deadlines;
        }

       public static async Task<List<Deadline>> parseAncPageNew(string page)
        {

            List<Deadline> deadlines = new List<Deadline>();
            var root = JSON.parse<AncRootObject>(page);
           foreach (var item in root.paginationList.recordList)
            {
               
                var _isFinished = (item.status != "0" );

                string _due = item.courseNotice.regDate;
                string _name = item.courseNotice.title;
                string _courseId = item.courseNotice.courseId;

                string _detail = item.courseNotice.detail;
           /*     if (_course == "")
                    _course = _courseId;
                if (_course == _courseId)
                {
                    foreach (var course in await DataAccess.getCourses())
                    {
                        if (course.id == _courseId)
                            _course = course.name;
                    }
                }*/

              //  _course = WebUtility.HtmlDecode(_course);
              //  _name = WebUtility.HtmlDecode(_name);

                deadlines.Add(new Deadline
                {
                    name = _name,
                   ddl = _due,
                    course = _name,
                    hasBeenFinished = _isFinished,
                    id = "_" + _courseId,
                    detail=_detail
                });
            }
       
            return deadlines; 
        }
        
        public static List<Course> parseCourseList(string page)
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

        public static List<Deadline> parseAncListPage(string page)
        {
            try
            {
                HtmlDocument htmlDoc = new HtmlDocument();
                htmlDoc.LoadHtml(page);

                string _name, _due, _course, _detail;

                _course = htmlDoc.DocumentNode.Descendants("td")/*MAGIC*/.First().InnerText;
                _course = _course.Trim();
                _course = _course.Substring(6/*MAGIC*/);
                _course = WebUtility.HtmlDecode(_course);

                HtmlNode[] nodes = htmlDoc.DocumentNode.Descendants("tr")/*MAGIC*/.ToArray();


                List<Deadline> deadlines = new List<Deadline>();
                for (int i = 3/*MAGIC*/; i < nodes.Length - 1/*MAGIC*/; i++)
                {
                    HtmlNode node = nodes[i];

                    var tds = node.Descendants("td");

                    var _isFinished = (tds.ElementAt(4/*MAGIC*/).InnerText.Trim() == "已读");

                    _due = tds.ElementAt(3/*MAGIC*/).InnerText;

                    var link_to_detail = node.Descendants("a")/*MAGIC*/.First();
                    _name = link_to_detail.InnerText;
                    _name = WebUtility.HtmlDecode(_name);
                    //
                    var _href = link_to_detail.Attributes["href"].Value;
                    var _id = Regex.Match(_href, @"[^_]id=(\d+)").Groups[1].Value;

                    var _cplhref = "http://learn.tsinghua.edu.cn/MultiLanguage/public/bbs/" + _href;
                    // _detail= parseAncListContent(_cplhref);

                    deadlines.Add(new Deadline
                    {
                        name = _name,
                        ddl = _due,
                        course = _course,
                        hasBeenFinished = _isFinished,
                        id = "@" + _id,
                        detail = _cplhref
                    });
                }
                return deadlines;

            }
            catch (Exception)
            {
                throw new ParsePageException("AssignmentList");
            }

        }
        public static string parseAncListContent(string page)
        {
            try
            {
                HtmlDocument htmlDoc = new HtmlDocument();
                htmlDoc.LoadHtml(page);

                string _name, _due, _course;

                //  _course = htmlDoc.DocumentNode.Descendants("td")/*MAGIC*/.First().InnerText;
                //  _course = _course.Trim();
                //  _course = _course.Substring(6/*MAGIC*/);
                //  _course = WebUtility.HtmlDecode(_course);

                HtmlNode[] nodes = htmlDoc.DocumentNode.Descendants("tr")/*MAGIC*/.ToArray();


                //  List<Deadline> deadlines = new List<Deadline>();
                string Content = "";
                for (int i = 3/*MAGIC*/; i < 5/*MAGIC*/; i++)
                {
                    HtmlNode node = nodes[i];

                    var tds = node.Descendants("td");

                    // var _isFinished = (tds.ElementAt(4/*MAGIC*/).InnerText.Trim() == "已读");

                    Content = tds.ElementAt(1/*MAGIC*/).InnerText;

                    // var link_to_detail = node.Descendants("a")/*MAGIC*/.First();
                    // _name = link_to_detail.InnerText;
                    //  _name = WebUtility.HtmlDecode(_name);
                    //
                    //  var _href = link_to_detail.Attributes["href"].Value;
                    //  var _id = Regex.Match(_href, @"[^_]id=(\d+)").Groups[1].Value;

                    // var _cplhref = "http://learn.tsinghua.edu.cn/MultiLanguage/public/bbs/" + _href;

                }
                return Content;

            }
            catch (Exception)
            {
                throw new ParsePageException("AssignmentList");
            }

        }
        private static SemestersRootObject parseSemestersPage(string page)
        {
            return JSON.parse<SemestersRootObject>(page);
        }

        private static Timetable parseTimetablePage(string page)
        {
            if (page.Length < "_([])".Length)
                throw new ParsePageException("timetable_javascript");
            string json = page.Substring(2, page.Length - 3);
            return JSON.parse<Timetable>(json);
        }







        // utilities
        private static Windows.Web.Http.Filters.HttpBaseProtocolFilter bpf = new Windows.Web.Http.Filters.HttpBaseProtocolFilter();
        private static HttpClient m_httpClient = new HttpClient(bpf);
        static public HttpCookieManager getCookieManager()
        {
            return bpf.CookieManager;
        }

        private static HttpResponseMessage httpResponse = new HttpResponseMessage();
        private static async Task<string> GET(string url)
        {
            //getPage
            httpResponse = await m_httpClient.GetAsync(new Uri(url));
            httpResponse.EnsureSuccessStatusCode();
            return await httpResponse.Content.ReadAsStringAsync();
        }

        private static async Task<string> POST(string url, string form_string)
        {
            HttpStringContent stringContent = new HttpStringContent(
                form_string,
                Windows.Storage.Streams.UnicodeEncoding.Utf8,
                "application/x-www-form-urlencoded");

            httpResponse = await m_httpClient.PostAsync(new Uri(url), stringContent);
            httpResponse.EnsureSuccessStatusCode();
            return await httpResponse.Content.ReadAsStringAsync();
        }

        private static TimeSpan UnixTime()
        {
            return (DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1)));
        }

        private static async Task 十１ｓ(double seconds = 1)
        {
            await Task.Delay(TimeSpan.FromSeconds(seconds));
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
                throw new ParsePageException("JSON " + typeof(T).ToString());
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