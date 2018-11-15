﻿using System;
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
        public bool isNew; //uses (`learn.cic` or `learn`?) .tsinghua.edu.cn
        public string semester;

        override public string ToString() {
            return "#" + id + ": " + name;
        }
    }

    public class Deadline {
        public string id;
        public string name;
        public string ddl;
        public string course;
        public string detail;
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
            return (DateTime.Parse(ddl + " 23:59") - DateTime.Now).TotalDays;
        }

        public string timeLeft() {
            return timeLeftChinese();
        }

        public bool isPast() {
            return DateTime.Parse(ddl + " 23:59") < DateTime.Now;
        }

        public string timeLeftChinese() {
            TimeSpan timeDelta = DateTime.Parse(ddl + " 23:59") - DateTime.Now;

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
                timeLeft = "已经过去 " + d.ToString() + " 小时";
            } else if (daysLeft > -10) {
                var d = (-timeDelta.Days);
                timeLeft = "已经过去 " + d.ToString() + " 天";
            } else {
                var d = Math.Round(timeDelta.TotalDays / -7);
                timeLeft = "已经过去 " + d.ToString() + " 周";
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
            var semesterStart = DateTime.Parse(startDate);
            var delta = DateTime.Now - semesterStart;
            var days = delta.TotalDays;

            /* 0 ~  6.9 -> 1
               7 ~ 13.9 -> 2 */
            return (Math.Floor(days / 7) + 1).ToString();
        }
    }

    // the following classes are generated from JSON by Visual Studio, 
    // for JSON parser only
    public class CourseAssignmentsRootobject {
        public Resultlist[] resultList { get; set; }
    }

    public class Resultlist {
        public Coursehomeworkrecord courseHomeworkRecord { get; set; }
        public Coursehomeworkinfo courseHomeworkInfo { get; set; }
    }

    public class Coursehomeworkrecord {
        public string status { get; set; }
    }

    public class Coursehomeworkinfo {
        public int homewkId { get; set; }
        public long endDate { get; set; }
        public string title { get; set; }
        public string detail { get; set; }
        public string courseId { get; set; }
    }

    public class SemestersRootObject {
        public Currentteachingweek currentTeachingWeek { get; set; }
        public Semester currentSemester { get; set; }
        public string currentDate { get; set; }
        public Semester nextSemester { get; set; }
    }

    public class Currentteachingweek {
        public int teachingWeekId { get; set; }
        public string weekName { get; set; }
        public string beginDate { get; set; }
        public string endDate { get; set; }
        public string semesterId { get; set; }
    }


    public class Timetable : List<Event> {

    }

    public class Event {
        public string dd { get; set; }
        public string fl { get; set; }
        public int grrlID { get; set; }
        public string jssj { get; set; }
        public string kssj { get; set; }
        public string nq { get; set; }
        public string nr { get; set; }
        public string sfSjtz { get; set; }
        public string skjc { get; set; }
        public string sm { get; set; }
    }

    public class Lectures {
        public Lecture[] Property1 { get; set; }
    }

    public class Lecture {
        public string description { get; set; }
        public string summary { get; set; }
        public string location { get; set; }
        public string dtstart { get; set; }
        public string dtend { get; set; }
        public long uid { get; set; }
    }

}