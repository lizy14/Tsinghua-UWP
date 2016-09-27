using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Windows.ApplicationModel.Appointments;

namespace TsinghuaUWP {
    public static class Appointment {

        private static string ddl_cal_name = "作业";
        private static string class_cal_name = "课程表";
        private static string cal_cal_name = "校历";

        private static string ddl_storedKey = "appointmentCalendarForDeadlines";
        private static string class_storedKey = "appointmentCalendarForClasses";
        private static string cal_storedKey = "appointmentCalendarForTeachingWeeks";

        public static async Task deleteAllCalendars() {
            var store = await AppointmentManager.RequestStoreAsync(AppointmentStoreAccessType.AppCalendarsReadWrite);

            foreach (var old_cal in await store.FindAppointmentCalendarsAsync()) {
                await old_cal.DeleteAsync();
            }
        }

        public static async Task updateDeadlines() {
            Debug.WriteLine("[Appointment] deadlines begin");


            var store = await AppointmentManager.RequestStoreAsync(AppointmentStoreAccessType.AppCalendarsReadWrite);

            try {
                var deadlines = await DataAccess.getAllDeadlines();
                if (deadlines.Count == 0)
                    throw new Exception();

                //get Calendar object
                AppointmentCalendar ddl_cal = null;
                if (DataAccess.getLocalSettings()[ddl_storedKey] != null) {
                    ddl_cal = await store.GetAppointmentCalendarAsync(
                        DataAccess.getLocalSettings()[ddl_storedKey].ToString());
                }

                if (ddl_cal == null) {
                    ddl_cal = await store.CreateAppointmentCalendarAsync(ddl_cal_name);
                    DataAccess.setLocalSettings(ddl_storedKey, ddl_cal.LocalId);
                }

                //TODO: don't delete all and re-insert all
                var aps = await ddl_cal.FindAppointmentsAsync(DateTime.Now.AddYears(-10), TimeSpan.FromDays(365 * 20));
                foreach (var ddl_ap in aps) {
                    await ddl_cal.DeleteAppointmentAsync(ddl_ap.LocalId);
                }

                foreach (var ev in deadlines) {
                    if (ev.shouldBeIgnored())
                        continue;
                    await ddl_cal.SaveAppointmentAsync(getAppointment(ev));
                }
            } catch (Exception) { }

            Debug.WriteLine("[Appointment] deadlines finish");
        }


        private static string semester_in_system_calendar = "__";
        public static async Task updateCalendar() {
            Debug.WriteLine("[Appointment] calendar begin");


            var store = await AppointmentManager.RequestStoreAsync(AppointmentStoreAccessType.AppCalendarsReadWrite);

            var current_semester = await DataAccess.getSemester(getNextSemester: false);
            var next_semester = await DataAccess.getSemester(getNextSemester: true);

            if (current_semester.semesterEname == semester_in_system_calendar)
                return;

            //get Calendar object
            AppointmentCalendar cal = null;
            if (DataAccess.getLocalSettings()[cal_storedKey] != null) {
                cal = await store.GetAppointmentCalendarAsync(
                    DataAccess.getLocalSettings()[cal_storedKey].ToString());
            }

            if (cal == null) {
                cal = await store.CreateAppointmentCalendarAsync(cal_cal_name);
                DataAccess.setLocalSettings(cal_storedKey, cal.LocalId);
            }

            var aps = await cal.FindAppointmentsAsync(DateTime.Now.AddYears(-10), TimeSpan.FromDays(365 * 20));
            foreach (var a in aps) {
                await cal.DeleteAppointmentAsync(a.LocalId);
            }

            foreach (var ev in getAppointments(current_semester)) {
                await cal.SaveAppointmentAsync(ev);
            }

            if (next_semester.id != current_semester.id) {
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
            foreach(var e in list)
                await cal.SaveAppointmentAsync(e);

            Debug.WriteLine("[Appointment] update finished");
        }

        private static Windows.ApplicationModel.Appointments.Appointment getAppointment(Event e) {
            var a = new Windows.ApplicationModel.Appointments.Appointment();
            a.Subject = e.nr;
            a.Location = e.dd;
            //TODO: probably doesn't work for exam events, which may be something like "2:30", "7:00"
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
            a.StartTime = DateTime.Parse(e.ddl + " 23:59");
            a.AllDay = false;
            a.BusyStatus = e.hasBeenFinished? AppointmentBusyStatus.Free: AppointmentBusyStatus.Tentative;
            if (e.hasBeenFinished)
                a.Reminder = null;
            else
                a.Reminder = TimeSpan.FromHours(6);
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
                        if (starting.StartTime + starting.Duration - current.StartTime < TimeSpan.FromMinutes(21)) {
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
}
