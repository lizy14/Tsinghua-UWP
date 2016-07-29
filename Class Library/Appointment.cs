using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Appointments;

namespace TsinghuaUWP
{
    public static class Appointment
    {
        static string ddl_storedKey = "appointmentCalendarForDeadlines";
        static string class_storedKey = "appointmentCalendarForClasses";
        public static async Task deleteAllCalendars()
        {
            var store = await AppointmentManager.RequestStoreAsync(AppointmentStoreAccessType.AppCalendarsReadWrite);
            
            foreach (var old_cal in await store.FindAppointmentCalendarsAsync())
            {
                await old_cal.DeleteAsync();
            }
        }
        public static async Task updateDeadlines()
        {
            //do deadlines
            var store = await AppointmentManager.RequestStoreAsync(AppointmentStoreAccessType.AppCalendarsReadWrite);
            Debug.WriteLine("[Appointment] doing assignments");
            try
            {
                AppointmentCalendar ddl_cal;
                if (DataAccess.getLocalSettings()[ddl_storedKey] != null)
                {
                    ddl_cal = await store.GetAppointmentCalendarAsync(
                        DataAccess.getLocalSettings()[ddl_storedKey].ToString());
                }
                else
                {
                    ddl_cal = await store.CreateAppointmentCalendarAsync("课程作业");
                    DataAccess.setLocalSettings(ddl_storedKey, ddl_cal.LocalId);
                }

                var aps = await ddl_cal.FindAppointmentsAsync(DateTime.Now.AddYears(-10), TimeSpan.FromDays(365 * 20));
                foreach (var ddl_ap in aps)
                {
                    await ddl_cal.DeleteAppointmentAsync(ddl_ap.LocalId);
                }
                
                foreach (var ev in await DataAccess.getAllDeadlines())
                {
                    var appointment = getAppointment(ev);

                    await ddl_cal.SaveAppointmentAsync(appointment);
                }
            }
            catch (Exception) { }
        }
        public static async Task update(bool forceRemote = false)
        {
            Debug.WriteLine("[Appointment] update start");

            //TODO: request calendar access?

            Timetable timetable;
            
            try { timetable = await DataAccess.getTimetable(forceRemote); } catch(Exception)
            { timetable = await DataAccess.getTimetable(forceRemote); }

            var store = await AppointmentManager.RequestStoreAsync(AppointmentStoreAccessType.AppCalendarsReadWrite);


            AppointmentCalendar cal;
            if (DataAccess.getLocalSettings()[ddl_storedKey] != null)
            {
                cal = await store.GetAppointmentCalendarAsync(
                    DataAccess.getLocalSettings()[ddl_storedKey].ToString());
            }
            else
            {
                cal = await store.CreateAppointmentCalendarAsync("课程表");
                DataAccess.setLocalSettings(ddl_storedKey, cal.LocalId);
            }


            foreach (var ev in timetable)
            {
                var appointment = getAppointment(ev);
                await cal.SaveAppointmentAsync(appointment);
            }

            

            Debug.WriteLine("[Appointment] update finished");
        }

        static Windows.ApplicationModel.Appointments.Appointment getAppointment(Event e)
        {
            var a = new Windows.ApplicationModel.Appointments.Appointment();
            a.Subject = e.nr;
            a.Location = e.dd;
            a.StartTime = DateTime.Parse(e.nq + " " + e.kssj);
            a.Duration = DateTime.Parse(e.nq + " " + e.jssj) - a.StartTime;
            a.AllDay = false;
            return a;
        }
        static Windows.ApplicationModel.Appointments.Appointment getAppointment(Deadline e)
        {
            var a = new Windows.ApplicationModel.Appointments.Appointment();
            a.Subject = e.name;
            a.Location = e.course;
            a.StartTime = DateTime.Parse(e.ddl + " 23:59");
            a.AllDay = false;
            a.BusyStatus = AppointmentBusyStatus.Tentative;
            a.Reminder = TimeSpan.FromHours(6);
            return a;
        }
    }
}
