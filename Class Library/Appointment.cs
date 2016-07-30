using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Appointments;
using Windows.UI;

namespace TsinghuaUWP
{
    public static class Appointment
    {

        static string ddl_cal_name = "作业";
        static string class_cal_name = "课程表";

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
            Debug.WriteLine("[Appointment] deadlines begin");


            var store = await AppointmentManager.RequestStoreAsync(AppointmentStoreAccessType.AppCalendarsReadWrite);
            
            try
            {
                var deadlines = await DataAccess.getAllDeadlines();
                if (deadlines.Count == 0)
                    throw new Exception();

                //get Calendar object
                AppointmentCalendar ddl_cal;
                if (DataAccess.getLocalSettings()[ddl_storedKey] != null)
                {
                    ddl_cal = await store.GetAppointmentCalendarAsync(
                        DataAccess.getLocalSettings()[ddl_storedKey].ToString());
                }
                else
                {
                    ddl_cal = await store.CreateAppointmentCalendarAsync(ddl_cal_name);
                    DataAccess.setLocalSettings(ddl_storedKey, ddl_cal.LocalId);
                }
                var color = ddl_cal.DisplayColor;

                //TODO: don't delete all and re-insert all
                var aps = await ddl_cal.FindAppointmentsAsync(DateTime.Now.AddYears(-10), TimeSpan.FromDays(365 * 20));
                foreach (var ddl_ap in aps)
                {
                    await ddl_cal.DeleteAppointmentAsync(ddl_ap.LocalId);
                }
                
                foreach (var ev in deadlines)
                {
                    await ddl_cal.SaveAppointmentAsync(getAppointment(ev));
                }
            }
            catch (Exception) { }

            Debug.WriteLine("[Appointment] deadlines finish");
        }
        public static async Task updateTimetable(bool forceRemote = false)
        {
            Debug.WriteLine("[Appointment] update start");

            //TODO: request calendar access?

            Timetable timetable;
            
            try{
                timetable = await DataAccess.getTimetable(forceRemote);
            }catch(Exception){
                try{
                    //TODO: prettier retry
                    await Task.Delay(TimeSpan.FromSeconds(1));
                    timetable = await DataAccess.getTimetable(forceRemote);
                }catch(Exception e){
                    throw e;
                }
            }

            if (timetable.Count == 0)
                throw new Exception();

            var store = await AppointmentManager.RequestStoreAsync(AppointmentStoreAccessType.AppCalendarsReadWrite);


            AppointmentCalendar cal;
            if (DataAccess.getLocalSettings()[class_storedKey] != null)
            {
                cal = await store.GetAppointmentCalendarAsync(
                    DataAccess.getLocalSettings()[class_storedKey].ToString());
            }
            else
            {
                cal = await store.CreateAppointmentCalendarAsync(class_cal_name);
                DataAccess.setLocalSettings(class_storedKey, cal.LocalId);
            }


            //TODO: don't delete all and re-insert all
            var aps = await cal.FindAppointmentsAsync(DateTime.Now.AddYears(-10), TimeSpan.FromDays(365 * 20));
            foreach (var ddl_ap in aps)
            {
                await cal.DeleteAppointmentAsync(ddl_ap.LocalId);
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
            //TODO: probably doesn't work for exam events, which may be something like "2:30", "7:00"
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
