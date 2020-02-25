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
    public static class Remote {






        // exposes access to remote objects
        public static async Task<List<CourseDetail>> getRemoteCourseDetail(string courseId) {
            Debug.WriteLine("[getRemoteTimetable] start");
            await login();


            var page = await GET($"http://learn2018.tsinghua.edu.cn/b/kc/v_wlkc_xk_sjddb/detail?id={courseId}");
            var strs = JSON.parse<string[]>(page);
            var result = new List<CourseDetail> { };
            foreach (var str in strs) {
                var groups = Regex.Match(str, "星期([一二三四五六日])第(\\d)节\\((.+)\\)，(.+)").Groups;
                var segment = new CourseDetail { };
                switch (groups[1].Value) {
                    case "一": segment.skxq = 1; break;
                    case "二": segment.skxq = 2; break;
                    case "三": segment.skxq = 3; break;
                    case "四": segment.skxq = 4; break;
                    case "五": segment.skxq = 5; break;
                    case "六": segment.skxq = 6; break;
                    case "日": segment.skxq = 7; break;
                }
                segment.skjc = int.Parse(groups[2].Value);
                segment.skzc = parseWeekString(groups[3].Value);
                segment.skdd = groups[4].Value;
                segment.skxs = 2;
                result.Add(segment);
            }
            
            Debug.WriteLine("[getRemoteTimetable] returning direct");
            return result;
        }
    
        private static string parseWeekString(string weekString) {
            // reference: https://github.com/TennyZhuang/CamusAPI/blob/master/app/thulib/curriculum.js
            var result = new List<int> { };
            switch (weekString) {
                case "全周":
                    result.AddRange(range(1, 16));
                    break;
                case "前八周":
                    result.AddRange(range(1, 8));
                    break;
                case "后八周":
                    result.AddRange(range(9, 16));
                    break;
                case "单周":
                    result.AddRange(range(1, 16, 2));
                    break;
                case "双周":
                    result.AddRange(range(2, 16, 2));
                    break;
                default:
                    var slices = Regex.Split(weekString, "[ ]*[,，][ ]*|周");
                    foreach (var slice in slices) {
                        int start = -1;
                        int end = -1;
                        if (slice == "") {
                        } else if (slice.Contains("-") == false) {
                            result.Add(int.Parse(slice));
                        } else {
                            var splits = slice.Split('-');
                            result.AddRange(range(int.Parse(splits[0]), int.Parse(splits[1])));
                        }
                    }
                    break;
            }
            var str = new List<string> { };
            foreach (var i in range(1, 16)) {
                str.Add("0");
            }
            foreach (var w in result) {
                str[w - 1] = "1";
            }
            return string.Join("", str);
        }

        private static List<int> range(int start, int end, int step = 1) {
            // inclusive
            var result = new List<int> { };
            for (var i = start; i <= end; ++i) {
                result.Add(i);
            }
            return result;
        }

        public static async Task<List<Deadline>> getRemoteHomeworkList(string courseId) {
            await login();
            var result = new List<Deadline> { };
            result = result.Concat(await parseHomeworkListPage(await getHomeworkListPage(courseId, "Wj"))).ToList();
            return result;
        }


        public static async Task<List<Course>> getRemoteCourseList(string semester) {
            await login();
            return parseCourseList(await getCourseListPage(semester));
        }

        public static async Task<Semesters> getHostedSemesters() {
            return JSON.parse<Semesters>(await GET(hostedCalendarUrl));
        }

        public static async Task<Semesters> getRemoteSemesters() {
            await login();
            await 十１ｓ();
            var _remoteCalendar = parseSemestersPage(await getSemesterPage());
            if (_remoteCalendar.result.xnxq.Equals("2019-2020-2")) {
                _remoteCalendar.result.kssj = "2020-02-17"; // 修复网络学堂错误
            }
            return new Semesters {
                currentSemester = new Semester {
                    id = _remoteCalendar.result.id,
                    semesterEname = Regex.Replace(Regex.Replace(Regex.Replace(_remoteCalendar.result.id, "-1$", "-Autumn"), "-2$", "-Spring"), "-3$", "-Summer"),
                    semesterName = _remoteCalendar.result.xnxqmc,
                    startDate = _remoteCalendar.result.kssj,
                    endDate = _remoteCalendar.result.jssj
                },
                /*
                nextSemester = new Semester {
                    id = _remoteCalendar.resultList[0].id,
                    semesterEname = Regex.Replace(Regex.Replace(Regex.Replace(_remoteCalendar.resultList[0].id, "-1$", "-Autumn"), "-2$", "-Spring"), "-3$", "-Summer"),
                    semesterName = _remoteCalendar.resultList[0].xnxqmc,
                    startDate = _remoteCalendar.resultList[0].kssj,
                    endDate = _remoteCalendar.resultList[0].jssj
                },
                */
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
        private static async Task<int> login(bool useLocalSettings = true, string username = "", string password = "") {
            Debug.WriteLine("[login] begin");

            //retrieve username and password
            if (useLocalSettings) {
                if (DataAccess.credentialAbsent()) {
                    throw new LoginException("没有指定用户名和密码");
                }
                username = DataAccess.getLocalSettings()["username"].ToString();

                if (username == "__anonymous") {
                    throw new LoginException("没有指定用户名和密码");
                }

                var vault = new Windows.Security.Credentials.PasswordVault();
                password = vault.Retrieve("Tsinghua_Learn_Website", username).Password;
            }

            while (occupied) {
                await 十１ｓ(.1);
            }

            occupied = true;
            //check for last login
            if ((DateTime.Now - lastLogin).TotalMinutes < LOGIN_TIMEOUT_MINUTES
                && lastLoginUsername == username) {
                Debug.WriteLine("[login] reuses recent session");
                occupied = false;
                return 2;
            }


            try {
                string loginResponse;

                //login to learn2018.tsinghua.edu.cn

                loginResponse = await POST(
                    loginUri,
                    $"i_user={username}&i_pass={password}&atOnce=true");

                //check if successful
                var ticketGroup = Regex.Match(loginResponse, @"window.location.replace\(""(.+)""\);").Groups;

                var redirectUrl = ticketGroup[1].Value;

                var redirectStatus = Regex.Match(redirectUrl, @"status=([^&]+)(&|$)").Groups[1].Value;
                var redirectTicket = Regex.Match(redirectUrl, @"ticket=([^&]+)(&|$)").Groups[1].Value;
                if (redirectStatus != "SUCCESS") {
                    throw new LoginException("登录失败：" + redirectStatus);
                }
                await GET($"http://learn2018.tsinghua.edu.cn/b/j_spring_security_thauth_roaming_entry?ticket={redirectTicket}");


            } catch (Exception e) {
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


        private static DateTime lastLogin = DateTime.MinValue;
        private static string lastLoginUsername = "";
        private static int LOGIN_TIMEOUT_MINUTES = 5;
        private static string loginUri = "https://id.tsinghua.edu.cn/do/off/ui/auth/login/post/bb5df85216504820be7bba2b0ae1535b/0?/login.do";






        // remote object URLs and wrappers

        
        private static string hostedCalendarUrl = "https://static.nullspace.cn/thuCalendar.json";
        private static string hostedLectureUrl = "http://vultr.nullspace.cn:8000/test.json";
        public static string helpUrl = "https://static.nullspace.cn/thuUwpHelp.html";
        private static async Task<string> getHomeworkListPage(string courseId, string category) {
            var payload = $"aoData=[{{\"name\":\"wlkcid\",\"value\":\"{courseId}\"}},{{\"name\":\"iDisplayStart\",\"value\":0}},{{\"name\":\"iDisplayLength\",\"value\":-1}}]";
            return await POST($"http://learn2018.tsinghua.edu.cn/b/wlxt/kczy/zy/student/zyList" + category, payload);
        }

        private static async Task<string> getCourseListPage(string semester) {
            return await GET("http://learn2018.tsinghua.edu.cn/b/wlxt/kc/v_wlkc_xs_xkb_kcb_extend/student/loadCourseBySemesterId/" + semester);
        }

        private static async Task<string> getSemesterPage() {
            return await GET("http://learn2018.tsinghua.edu.cn/b/kc/zhjw_v_code_xnxq/getCurrentAndNextSemester");
        }








        // parse HTML or JSON, and return corresponding Object

        private static async Task<List<Deadline>> parseHomeworkListPage(string page) {
            var deadlines = new List<Deadline> { };
            string _course = "";
            Regex re = new Regex("&[^;]+;");

            var parsed = JSON.parse<HomeworkDetailRootobject>(page.Replace("\"object\":", "\"objects\":"));
            foreach (var a in parsed.objects.aaData){
                var _isFinished = false;

                string _due = a.jzsjStr;

                string _name = a.bt;
                string _courseId = a.wlkcid;

                if (_course == "")
                    _course = _courseId;
                if (_course == _courseId) {
                    foreach (var course in await DataAccess.getCourses()) {
                        if (course.id == _courseId)
                            _course = course.name;
                    }
                }

                _course = WebUtility.HtmlDecode(_course);
                _name = WebUtility.HtmlDecode(_name);

                deadlines.Add(new Deadline {
                    name = re.Replace(_name, " "),
                    ddl = _due,
                    course = re.Replace(_course, " "),
                    hasBeenFinished = _isFinished,
                    id = a.wlkcid + "_" + a.zyid
                });
            }
            return deadlines;
        }

        private static List<Course> parseCourseList(string page) {
            var result = new List<Course> { };
            foreach (var c in JSON.parse<RemoteCourseRootObject>(page).resultList) {
                result.Add(new Course {
                    id = c.wlkcid,
                    name = c.kcm,
                });
            }
            return result;
        }

        private static SemestersRootObject parseSemestersPage(string page) {
            return JSON.parse<SemestersRootObject>(page);
        }

        private static Timetable parseTimetablePage(string page) {
            if (page.Length < "_([])".Length)
                throw new ParsePageException("timetable_javascript");
            string json = page.Substring(2, page.Length - 3);
            return JSON.parse<Timetable>(json);
        }







        // utilities
        private static Windows.Web.Http.Filters.HttpBaseProtocolFilter bpf = new Windows.Web.Http.Filters.HttpBaseProtocolFilter();
        private static HttpClient m_httpClient = new HttpClient(bpf);
        static public HttpCookieManager getCookieManager() {
            return bpf.CookieManager;
        }

        private static HttpResponseMessage httpResponse = new HttpResponseMessage();
        private static async Task<string> GET(string url) {
            Debug.WriteLine(url);
            //getPage
            httpResponse = await m_httpClient.GetAsync(new Uri(url));
            httpResponse.EnsureSuccessStatusCode();
            return await httpResponse.Content.ReadAsStringAsync();
        }

        private static async Task<string> POST(string url, string form_string) {
            Debug.WriteLine(url);
            HttpStringContent stringContent = new HttpStringContent(
                form_string,
                Windows.Storage.Streams.UnicodeEncoding.Utf8,
                "application/x-www-form-urlencoded");

            httpResponse = await m_httpClient.PostAsync(new Uri(url), stringContent);
            httpResponse.EnsureSuccessStatusCode();
            return await httpResponse.Content.ReadAsStringAsync();
        }

        private static TimeSpan UnixTime() {
            return (DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1)));
        }

        private static async Task 十１ｓ(double seconds = 1) {
            await Task.Delay(TimeSpan.FromSeconds(seconds));
        }

    }

    // wrapped JSON parser
    public class JSON {
        public static T parse<T>(string jsonString) {
            try {
                using (var ms = new MemoryStream(Encoding.UTF8.GetBytes(jsonString))) {
                    return (T)new DataContractJsonSerializer(typeof(T)).ReadObject(ms);
                }
            } catch (Exception) {
                throw new ParsePageException("JSON " + typeof(T).ToString());
            }
        }

        public static string stringify(object jsonObject) {
            using (var ms = new MemoryStream()) {
                new DataContractJsonSerializer(jsonObject.GetType()).WriteObject(ms, jsonObject);
                return Encoding.UTF8.GetString(ms.ToArray());
            }
        }
    }
}