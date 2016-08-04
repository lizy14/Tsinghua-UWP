using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Diagnostics;
using Windows.Storage;

namespace TsinghuaUWP {
    // Data Access Object
    // retrieve from memory/ local settings/ remote

    static public class DataAccess {
        private static ApplicationDataContainer localSettings = ApplicationData.Current.LocalSettings;
        private static List<Course> courses = null;
        private static List<Deadline> deadlines = null;
        private static Semesters semesters = null;

        static public Windows.Foundation.Collections.IPropertySet getLocalSettings() {
            return localSettings.Values;
        }

        static public bool credentialAbsent() {
            var username = localSettings.Values["username"];
            return username == null
                || username.ToString() == "__anonymous";
        }

        static public bool supposedToWorkAnonymously() {
            var username = localSettings.Values["username"];
            return username != null
                && username.ToString() == "__anonymous";
        }

        static public bool isDemo() {
            return
                localSettings.Values["username"] != null &&
                localSettings.Values["username"].ToString() == "233333";
        }

        static public void setLocalSettings<Type>(string key, Type value) {
            localSettings.Values[key] = value;
        }

        static async public Task<int> updateAllFromRemote() {
            await getSemester(true);
            await getCourses(true);
            await getTimetable(true);
            await getAllDeadlines(true);
            return 0;
        }

        static public List<Deadline> sortDeadlines(List<Deadline> assignments, int limit = -1) {

            var future = (from assignment in assignments
                          where !assignment.isPast()
                          orderby assignment.daysFromNow() ascending
                          select assignment);

            int futureCount = future.Count();

            if (futureCount > limit && limit >= 0) {
                return future.Take(limit).ToList();
            }


            var past = (from assignment in assignments
                        where assignment.isPast()
                        orderby assignment.daysFromNow() descending
                        select assignment);


            if (limit < 0) {
                return future.Concat(past).ToList();
            }

            return future.Concat(past.Take(limit - futureCount)).ToList();
        }

        public static async Task<List<Course>> getCourses(bool forceRemote = false) {
            if (isDemo()) {
                var list = new List<Course>();
                list.Add(new Course {
                    name = "数据结构",
                    id = "demo_course_0",
                });

                list.Add(new Course {
                    name = "操作系统",
                    id = "demo_course_1",
                });

                return list;
            }

            if (!forceRemote) {
                //try memory
                if (courses != null) {
                    Debug.WriteLine("[getCourses] Returning memory");
                    return courses;
                }

                //try localSettings
                var localCourses = localSettings.Values["courses"];
                if (localCourses != null) {
                    Debug.WriteLine("[getCourses] Returning local settings");
                    courses = JSON.parse<List<Course>>((string)localCourses);
                    return courses;
                }
            }


            //fetch from remote
            var _courses = await Remote.getRemoteCourseList();
            courses = _courses;
            localSettings.Values["courses"] = JSON.stringify(_courses);
            Debug.WriteLine("[getCourses] Returning remote");
            return courses;
        }

        public static async Task<Timetable> getTimetable(bool forceRemote = false) {
            if (isDemo()) {
                var table = new Timetable();

                var start = DateTime.Now.AddDays(-20);
                while (start.DayOfWeek != DayOfWeek.Monday)
                    start = start.AddDays(-1);

                for (var i = 0; i < 10; i++) {
                    table.Add(new Event {
                        nr = "形式语言与自动机",
                        dd = "六教 6A301",
                        nq = start.AddDays(i * 7 + 2).ToString("yyyy-MM-dd"),
                        kssj = "08:00",
                        jssj = "09:35"
                    });

                    table.Add(new Event {
                        nr = "高级数据结构",
                        dd = "六教 6A301",
                        nq = start.AddDays(i * 7 + 2).ToString("yyyy-MM-dd"),
                        kssj = "09:50",
                        jssj = "11:25"
                    });

                    table.Add(new Event {
                        nr = "操作系统",
                        dd = "六教 6A303",
                        nq = start.AddDays(i * 7 + 3).ToString("yyyy-MM-dd"),
                        kssj = "09:50",
                        jssj = "11:25"
                    });

                    table.Add(new Event {
                        nr = "概率论与数理统计",
                        dd = "六教 6C102",
                        nq = start.AddDays(i * 7 + 4).ToString("yyyy-MM-dd"),
                        kssj = "15:20",
                        jssj = "16:55"
                    });

                    table.Add(new Event {
                        nr = "概率论与数理统计",
                        dd = "一教 104",
                        nq = start.AddDays(i * 7 + 1).ToString("yyyy-MM-dd"),
                        kssj = "13:30",
                        jssj = "15:05"
                    });
                }
                return table;
            }

            //fetch from remote
            var _remoteTimetable = await Remote.getRemoteTimetable();
            Debug.WriteLine("[getTimetable] Returning remote");
            return _remoteTimetable;
        }

        public static async Task<Semester> getSemester(bool forceRemote = false) {
            if (isDemo()) {
                var start = DateTime.Now.AddDays(-20);
                while (start.DayOfWeek != DayOfWeek.Monday)
                    start = start.AddDays(-1);

                return new Semester {
                    startDate = start.ToString("yyyy-MM-dd"),
                    endDate = start.AddDays(10 * 7 - 1).ToString("yyyy-MM-dd"),
                    semesterEname = "2333-2334-Spring",
                };
            }
            if (!forceRemote) {
                Semesters __semesters = null;
                //try memory
                if (semesters != null) {
                    Debug.WriteLine("[getCalendar] memory");
                    __semesters = semesters;
                } else //try localSettings
                  {
                    var localJSON = localSettings.Values["semesters"];
                    if (localJSON != null) {
                        Debug.WriteLine("[getCalendar] local settings");
                        __semesters = JSON.parse<Semesters>((string)localJSON);
                    }
                }

                //cache hit
                if (__semesters != null) {
                    if (DateTime.Parse(__semesters.currentSemester.endDate + " 23:59") < DateTime.Now) {
                        //perform a remote update
                        Task task = getSemester(true);
                        task.ContinueWith((_) => Appointment.updateCalendar());

                        Debug.WriteLine("[getCalendar] Returning cache next");
                        return __semesters.nextSemester;
                    }
                    Debug.WriteLine("[getCalendar] Returning cache");
                    return __semesters.currentSemester;
                }
            }

            //fetch from remote
            Semesters _remoteSemesters = null;

            try {
                _remoteSemesters = await Remote.getHostedSemesters();
            } catch (Exception) { }

            if (_remoteSemesters == null) {
                Debug.WriteLine("[getCalendar] hosted fail, falling back");

                if (credentialAbsent())
                    throw new LoginException("calendar_fall_back");

                _remoteSemesters = await Remote.getRemoteSemesters();
            }

            semesters = _remoteSemesters;
            localSettings.Values["semesters"] = JSON.stringify(semesters);
            Debug.WriteLine("[getCalendar] Returning remote");
            return semesters.currentSemester;
        }

        static public async Task<List<Deadline>> getAllDeadlines(bool forceRemote = false) {
            if (isDemo()) {
                var list = new List<Deadline>();
                var start = DateTime.Now.AddDays(-20);
                while (start.DayOfWeek != DayOfWeek.Monday)
                    start = start.AddDays(-1);

                for (var i = 0; i <= 3; i++) {
                    list.Add(new Deadline {
                        course = "操作系统",
                        ddl = start.AddDays(i * 7 + 4 + 7).ToString("yyyy-MM-dd"),
                        name = $"代码阅读报告{i + 1}",
                        hasBeenFinished = (i < 3),
                        id = "operating_systems_" + i.ToString(),
                    });
                }

                for (var i = 0; i <= 3; i++) {
                    list.Add(new Deadline {
                        course = "数据结构",
                        ddl = start.AddDays(i * 7 + 3 + 7).ToString("yyyy-MM-dd"),
                        name = $"数据结构习题{i + 1}",
                        hasBeenFinished = (i < 3),
                        id = "data_structure_" + i.ToString(),
                    });
                }

                return list;
            }
            if (!forceRemote) {
                //try session memory
                if (deadlines != null) {
                    Debug.WriteLine("[getAllDeadlines] Returning memory");
                    return deadlines;
                }


                //try localSettings
                var local = localSettings.Values["deadlines"];
                if (local != null) {
                    Debug.WriteLine("[getAllDeadlines] Returning local settings");
                    return JSON.parse<List<Deadline>>((string)local);
                }
            }

            //fetch from remote

            List<Deadline> _deadlines = new List<Deadline>();

            foreach (var course in await getCourses(forceRemote)) {
                Debug.WriteLine("[getAllDeadlines] Remote " + course.name);
                var id = course.id;
                List<Deadline> __deadlines;
                if (course.isNew)
                    __deadlines = await Remote.getRemoteHomeworkListNew(id);
                else
                    __deadlines = await Remote.getRemoteHomeworkList(id);
                _deadlines = _deadlines.Concat(__deadlines).ToList();
            }


            deadlines = _deadlines;
            localSettings.Values["deadlines"] = JSON.stringify(_deadlines);
            Debug.WriteLine("[getAllDeadlines] Returning remote");

            return _deadlines;
        }

    }

}
