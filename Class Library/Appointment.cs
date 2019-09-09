using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Windows.ApplicationModel.Appointments;

namespace TsinghuaUWP {
    public static class Appointment {

        private static string ddl_cal_name = "作业";
        private static string class_cal_name = "课程表";
        private static string cal_cal_name = "校历";
        private static string lec_cal_name = "文素讲座";

        private static string ddl_storedKey = "appointmentCalendarForDeadlines";
        private static string class_storedKey = "appointmentCalendarForClasses";
        private static string cal_storedKey = "appointmentCalendarForTeachingWeeks";
        private static string lec_storedKey = "appointmentCalendarForLectures";

        public static async Task deleteAllCalendars() {
            var store = await AppointmentManager.RequestStoreAsync(AppointmentStoreAccessType.AppCalendarsReadWrite);

            foreach (var old_cal in await store.FindAppointmentCalendarsAsync()) {
                await old_cal.DeleteAsync();
            }
        }

        public static async Task deleteAllAppointments(AppointmentCalendar cal) {
            var aps = await cal.FindAppointmentsAsync(DateTime.Now.AddYears(-10), TimeSpan.FromDays(365 * 20));
            foreach (var a in aps) {
                await cal.DeleteAppointmentAsync(a.LocalId);
            }
        }

        static async Task<AppointmentCalendar> getAppointmentCalendar(string name, string key) {
            var store = await AppointmentManager.RequestStoreAsync(AppointmentStoreAccessType.AppCalendarsReadWrite);
            AppointmentCalendar cal = null;

            if (DataAccess.getLocalSettings()[key] != null) {
                cal = await store.GetAppointmentCalendarAsync(
                    DataAccess.getLocalSettings()[key].ToString());
            }

            if (cal == null) {
                cal = await store.CreateAppointmentCalendarAsync(name);
                DataAccess.setLocalSettings(key, cal.LocalId);
            }

            return cal;
        }
        public static async Task updateDeadlines() {
            Debug.WriteLine("[Appointment] deadlines begin");

            try {
                var deadlines = await DataAccess.getAllDeadlines();
                if (deadlines.Count == 0)
                    throw new Exception();

                //get Calendar object
                AppointmentCalendar ddl_cal = await getAppointmentCalendar(ddl_cal_name, ddl_storedKey);

                var aps = await ddl_cal.FindAppointmentsAsync(DateTime.Now.AddYears(-10), TimeSpan.FromDays(365 * 20));
                foreach (var ddl_ap in aps) {
                    if (ddl_ap.Details == "") {
                        await ddl_cal.DeleteAppointmentAsync(ddl_ap.LocalId);
                        Debug.WriteLine("[updateDeadlines] deleting " + ddl_ap.Subject);
                    }
                }

                var existing = new List<Windows.ApplicationModel.Appointments.Appointment>();
                aps = await ddl_cal.FindAppointmentsAsync(DateTime.Now.AddYears(-10), TimeSpan.FromDays(365 * 20));
                foreach (var a in aps) {
                    existing.Add(a);
                }

                var waiting = new List<Windows.ApplicationModel.Appointments.Appointment>();
                foreach (var ev in deadlines) {
                    if (ev.shouldBeIgnored())
                        continue;
                    if (ev.hasBeenFinished)
                        continue; //TODO: should be user-configurable
                    waiting.Add(getAppointment(ev));
                }

                var to_be_deleted = existing.Except(waiting, new AppointmentComparer());
                var to_be_inserted = waiting.Except(existing, new AppointmentComparer());


                foreach (var i in to_be_deleted) {
                    Debug.WriteLine("[updateDeadlines] deleting' " + i.Subject);
                    await ddl_cal.DeleteAppointmentAsync(i.LocalId);
                }

                foreach (var i in to_be_inserted) {
                    Debug.WriteLine("[updateDeadlines] inserting " + i.Subject);
                    
                    if (i.StartTime - DateTime.Now < TimeSpan.FromHours(7)) {
                        // WTF ??
                        Debug.WriteLine("[updateDeadlines] ignoring " + i.Subject);
                        // continue;
                    }
                    await ddl_cal.SaveAppointmentAsync(i);
                }

            } catch (Exception) { }

            Debug.WriteLine("[Appointment] deadlines finish");
        }


        private static string semester_in_system_calendar = "__";
        public static async Task updateCalendar() {
            Debug.WriteLine("[Appointment] calendar begin");

            //TODO: possible duplication, lock?

            var current_semester = await DataAccess.getSemester(getNextSemester: false);
            var next_semester = await DataAccess.getSemester(getNextSemester: true);

            if (current_semester.semesterEname == semester_in_system_calendar)
                return;

            //get Calendar object
            AppointmentCalendar cal = await getAppointmentCalendar(cal_cal_name, cal_storedKey);

            await deleteAllAppointments(cal);

            foreach (var ev in getAppointments(current_semester)) {
                await cal.SaveAppointmentAsync(ev);
            }

            if (next_semester != null && next_semester.id != current_semester.id) {
                foreach (var ev in getAppointments(next_semester)) {
                    await cal.SaveAppointmentAsync(ev);
                }
            }

            semester_in_system_calendar = current_semester.semesterEname;

            Debug.WriteLine("[Appointment] calendar finish");
        }

        public static async Task updateTimetable(bool forceRemote = false) {
            Debug.WriteLine("[Appointment] update start");

            //TODO: request calendar access?

            Timetable timetable = await DataAccess.getTimetable(forceRemote);

            if (timetable.Count == 0)
                throw new Exception();

            var store = await AppointmentManager.RequestStoreAsync(AppointmentStoreAccessType.AppCalendarsReadWrite);


            AppointmentCalendar cal = null;

            if (DataAccess.getLocalSettings()[class_storedKey] != null) {
                cal = await store.GetAppointmentCalendarAsync(
                    DataAccess.getLocalSettings()[class_storedKey].ToString());
            }

            if (cal == null) {
                cal = await store.CreateAppointmentCalendarAsync(class_cal_name);
                DataAccess.setLocalSettings(class_storedKey, cal.LocalId);
            }


            //TODO: don't delete all and re-insert all
            //
            var aps = await cal.FindAppointmentsAsync(DateTime.Now.AddYears(-10), TimeSpan.FromDays(365 * 20));
            foreach (var ddl_ap in aps) {
                await cal.DeleteAppointmentAsync(ddl_ap.LocalId);
            }


            var list = new List<Windows.ApplicationModel.Appointments.Appointment>();
            foreach (var ev in timetable)
                list.Add(getAppointment(ev));
            list = mergeAppointments(list);
            foreach (var e in list)
                await cal.SaveAppointmentAsync(e);

            Debug.WriteLine("[Appointment] update finished");
        }


        private static Windows.ApplicationModel.Appointments.Appointment getAppointment(Event e) {
            var a = new Windows.ApplicationModel.Appointments.Appointment();
            a.Subject = e.nr;
            a.Location = e.dd;

            a.StartTime = DateTime.Parse(e.nq + " " + e.kssj);
            a.Duration = DateTime.Parse(e.nq + " " + e.jssj) - a.StartTime;

            a.AllDay = false;
            return a;
        }

        private static Windows.ApplicationModel.Appointments.Appointment getAppointment(Deadline e) {
            var a = new Windows.ApplicationModel.Appointments.Appointment();
            Regex re = new Regex("&[^;]+;");
            a.Subject = re.Replace(e.name, " ");
            a.Location = re.Replace(e.course, " ");
            a.StartTime = DateTime.Parse(e.ddl);
            a.AllDay = false;
            a.BusyStatus = e.hasBeenFinished ? AppointmentBusyStatus.Free : AppointmentBusyStatus.Tentative;
            if (e.hasBeenFinished || a.StartTime.AddHours(-6) < DateTime.Now)
                a.Reminder = null;
            else
                a.Reminder = TimeSpan.FromHours(6);
            a.Details = e.id;
            return a;
        }

        private static List<Windows.ApplicationModel.Appointments.Appointment> mergeAppointments(List<Windows.ApplicationModel.Appointments.Appointment> input) {
            int n = input.Count;
            var output = new List<Windows.ApplicationModel.Appointments.Appointment>();
            for (int i = 0; i < n;) {
                var starting = input[i];
                int offset = 1;
                while (true) {
                    if (i + offset >= n) break;
                    var current = input[i + offset];
                    if (current.Subject == starting.Subject && current.Location == starting.Location) {
                        var gap = current.StartTime - starting.StartTime - starting.Duration;
                        if (gap < TimeSpan.FromMinutes(21) && gap > TimeSpan.FromMinutes(-1)) {
                            starting.Duration = current.StartTime + current.Duration - starting.StartTime;
                            offset++;
                        } else {
                            break;
                        }
                    } else {
                        break;
                    }
                }
                output.Add(starting);
                i += offset;
            }
            return output;
        }

        private static List<Windows.ApplicationModel.Appointments.Appointment> getAppointments(Semester s) {
            var l = new List<Windows.ApplicationModel.Appointments.Appointment>();

            DateTime start = DateTime.Parse(s.startDate);
            if (start.DayOfWeek != DayOfWeek.Monday) {
                //TODO
                return l;
            }

            DateTime end;
            if (s.endDate != null) {
                end = DateTime.Parse(s.endDate).AddDays(-1);
                if (end < start)
                    throw new Exception();
            } else {
                //try to auto-complete, assuming 18 weeks per semester
                if (s.semesterEname.IndexOf("Autumn") != -1
                    || s.semesterEname.IndexOf("Spring") != -1) {
                    end = start.AddDays(18 * 7 - 1);
                } else {
                    return l;
                }
            }

            int i = 0;
            var day = start;
            while (233 > 0) {
                i++;
                if (day > end)
                    break;

                var a = new Windows.ApplicationModel.Appointments.Appointment();
                a.Subject = $"校历第{i}周";
                a.Details = s.semesterEname
                    .Replace("Summer", "夏季学期")
                    .Replace("Spring", "春季学期")
                    .Replace("Autumn", "秋季学期");
                a.StartTime = day;
                a.AllDay = true;
                a.BusyStatus = AppointmentBusyStatus.Free;

                l.Add(a);

                day = day.AddDays(7);
            }

            return l;
        }
    }

    public class AppointmentComparer : IEqualityComparer<Windows.ApplicationModel.Appointments.Appointment> {

        public bool Equals(Windows.ApplicationModel.Appointments.Appointment x, Windows.ApplicationModel.Appointments.Appointment y) {
            return
                x.Details == y.Details;
        }

        public int GetHashCode(Windows.ApplicationModel.Appointments.Appointment a) {
            return a.Details.GetHashCode();
        }
    }
}



