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
        static string storedKey = "appointmentCalendar";
        public static async Task update(bool forceRemote = false)
        {
            Debug.WriteLine("[Appointment] update start");

            //TODO: request calendar access?

            var timetable = await DataAccess.getTimetable(forceRemote);

            var store = await AppointmentManager.RequestStoreAsync(AppointmentStoreAccessType.AppCalendarsReadWrite);

            //delete previously created
            var storedId = DataAccess.getLocalSettings()[storedKey];
            if(storedId != null)
            {
                await store.GetAppointmentCalendarAsync(storedId.ToString());
            }
            foreach (var old_cal in await store.FindAppointmentCalendarsAsync())
            {
                await old_cal.DeleteAsync();
            }

            //create new
            var cal = await store.CreateAppointmentCalendarAsync("清华课表");
            DataAccess.getLocalSettings()[storedKey] = cal.LocalId;

            foreach(var ev in timetable)
                await cal.SaveAppointmentAsync(getAppointment(ev));
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
    }
}
