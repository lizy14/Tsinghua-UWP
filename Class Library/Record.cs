using System;
using System.Collections.Generic;

namespace TsinghuaUWP {

    /*
     用词对照

        课程：Course
        作业：Deadline, assignment
        学期：Semester
        校历：Semesters, calendar
        课表：Timetable, AppointmentCalendar
        课表项：Event, Appointment
        讲座：Lecture

         */
    public class Password {
        public string username;
        public string password;
    }

    public class Course {
        public string id;
        public string name;

        override public string ToString() {
            return "#" + id + ": " + name;
        }
    }

    public class Deadline {
        public string id;
        public string name;
        public string ddl;
        public string course;
        public bool hasBeenFinished;
        public bool hasBeenToasted() {
            string toasted = "";
            if (DataAccess.getLocalSettings()["toasted_assignments"] != null) {
                toasted = DataAccess.getLocalSettings()["toasted_assignments"].ToString();
            }
            return toasted.IndexOf(this.id) != -1;
        }

        public bool shouldBeIgnored() {
            string[] keywords = {
                "补交",
                "迟交"
            };
            foreach (var keyword in keywords) {
                if (name.IndexOf(keyword) != -1)
                    return true;
            }

            string[] courses = {
                "实验室科研探究"
            };
            foreach (var _course in courses) {
                if (course.IndexOf(_course) != -1)
                    return true;
            }

            return false;
        }

        public void mark_as_toasted() {
            string toasted = "";
            if (DataAccess.getLocalSettings()["toasted_assignments"] != null) {
                toasted = DataAccess.getLocalSettings()["toasted_assignments"].ToString();
            }
            toasted += "," + this.id;
            DataAccess.setLocalSettings("toasted_assignments", toasted);
        }

        public double daysFromNow() {
            return (DateTime.Parse(ddl) - DateTime.Now).TotalDays;
        }

        public string timeLeft() {
            return timeLeftChinese();
        }

        public bool isPast() {
            return DateTime.Parse(ddl) < DateTime.Now;
        }

        public string timeLeftChinese() {
            TimeSpan timeDelta = DateTime.Parse(ddl) - DateTime.Now;

            var daysLeft = timeDelta.TotalDays;
            string timeLeft = "";

            if (daysLeft > 10) {
                var d = Math.Round(daysLeft / 7);
                timeLeft = "还有 " + d.ToString() + " 周";
            } else if (daysLeft > 1) {
                var d = Math.Round(daysLeft);
                timeLeft = "只剩 " + d.ToString() + " 天";
            } else if (daysLeft > 0) {
                var d = timeDelta.Hours;
                if (d > 0)
                    timeLeft = "只剩 " + d.ToString() + " 小时";
                else
                    timeLeft = "即将到期！";
            } else if (daysLeft > -1) {
                var d = (-timeDelta.Hours);
                timeLeft = "已过期 " + d.ToString() + " 小时";
            } else if (daysLeft > -10) {
                var d = (-timeDelta.Days);
                timeLeft = "已过期 " + d.ToString() + " 天";
            } else {
                var d = Math.Round(timeDelta.TotalDays / -7);
                timeLeft = "已过期 " + d.ToString() + " 周";
            }


            return timeLeft;
        }

    }

    public class Semesters {
        public Semester currentSemester { get; set; }
        public Semester nextSemester { get; set; }
    }

    public class Semester {
        public string id { get; set; }
        public string semesterName { get; set; }
        public string startDate { get; set; }
        public string endDate { get; set; }
        public string semesterEname { get; set; }

        public string getWeekName() {
            var start = DateTime.Parse(startDate);
            for (; start.DayOfWeek != DayOfWeek.Monday; start = start.AddDays(1)) { }
            var delta = DateTime.Now - start;
            var days = delta.TotalDays;

            /* 0 ~  6.9 -> 1
               7 ~ 13.9 -> 2 */
            return (Math.Floor(days / 7) + 1).ToString();
        }
    }

    // the following classes are generated from JSON by Visual Studio, 
    // for JSON parser only

    public class RemoteSemester {
        public string xnxq { get; set; }
        public string xnxqmc { get; set; }
        public string kssj { get; set; }
        public string jssj { get; set; }
        public string id { get; set; }
    }
    public class SemestersRootObject {
        public RemoteSemester result { get; set; }
        public string message { get; set; }
        public RemoteSemester[] resultList { get; set; }
    }

    public class Timetable : List<Event> {

    }

    public class Event {
        public string dd { get; set; }
        public string jssj { get; set; }
        public string kssj { get; set; }
        public string nq { get; set; }
        public string nr { get; set; }

    }

    public class RemoteCourseRootObject {
        public string currentUser { get; set; }
        public string message { get; set; }
        public RemoteCourse[] resultList { get; set; }
    }

    public class RemoteCourse {

        public string wlkcid { get; set; }
        public string kcm { get; set; }
        public string kch { get; set; }
        public int kxh { get; set; }

        public string jsm { get; set; }

    }

    public class HomeworkDetailRootobject {
        public string result { get; set; }
        public HomeworkDetailObject objects { get; set; }
    }

    public class HomeworkDetailObject {
        public string iTotalDisplayRecords { get; set; }
        public HomeworkDetailAadata[] aaData { get; set; }
    }

    public class HomeworkDetailAadata {
        public long jzsj { get; set; }
        public string jzsjStr { get; set; }

        public string bt { get; set; }

        public string wlkcid { get; set; }

        public string zyid { get; set; }

    }

    public class CourseDetail {
        /*
        public string id { get; set; }
        public string wlkcid { get; set; }

        public string xnxq { get; set; }
        public string kch { get; set; }
        public string kxh { get; set; }
        */

        public string skzc { get; set; }
        public int skxq { get; set; }
        public int skjc { get; set; }
        public int skxs { get; set; }

        public string skdd { get; set; }
        
    }

}