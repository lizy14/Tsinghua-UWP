using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Windows.UI.Notifications;
using Windows.Data.Xml.Dom;
using System.Diagnostics;
using System.Globalization;
using System.Text.RegularExpressions;

namespace TsinghuaUWP
{
    public class Notification
    {
        static public async Task<int> update(bool forceRemote = false, bool calendarOnly = false)
        {
            Debug.WriteLine("[Notification] update begin");

            var updater = TileUpdateManager.CreateTileUpdaterForApplication();
            updater.EnableNotificationQueue(true);
            updater.Clear(); //TODO: should always clear?

            int tileCount = 0;

            var notifier = ToastNotificationManager.CreateToastNotifier();

            try
            {
                if (calendarOnly == false && ! DataAccess.credentialAbsent())
                {
                    Debug.WriteLine("[Notification] credential exist");

                    //deadlines
                    var deadlines = await DataAccess.getDeadlinesFiltered(forceRemote, limit: 3); 

                    foreach (var deadline in deadlines)
                    {
                        if (! deadline.isPast() && ! deadline.shouldBeIgnored())
                        {
                            var tile = new TileNotification(getTileXmlForDeadline(deadline));
                            updater.Update(tile);
                            tileCount++;
                        }

                        if (! deadline.hasBeenFinished)
                        {
                            if (! deadline.hasBeenToasted())
                            {
                                deadline.mark_as_toasted();
                                var toast = new ToastNotification(getToastXmlForDeadline(deadline));
                                notifier.Show(toast);
                            }
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Debug.WriteLine("[Notification] error dealing with deadlines: " + e.Message);
                throw e;
            }

            //calendar
            try
            {
                if (tileCount < 5)
                {
                    updater.Update(new TileNotification(getTileXmlForCalendar(await DataAccess.getSemester(forceRemote))));
                }
            }
            catch(Exception e)
            {
                Debug.WriteLine("[Notification] error dealing with calendar: " + e.Message);
                throw e;
            }
            

            Debug.WriteLine("[Notification] update finished");
            return 0;
        }
        static XmlDocument getToastXmlForDeadline(Deadline deadline)
        {

            // TODO: all values need to be XML escaped

            // Construct the visuals of the toast
            string toastVisual = $@"
<visual>
  <binding template='ToastGeneric'>
    <text>{deadline.name}</text>
    <text>{deadline.ddl}, {deadline.course}</text>
</binding>
</visual>";

            string toastXmlString =
$@"<toast>
    {toastVisual}
</toast>";
            // Parse to XML
            XmlDocument toastXml = new XmlDocument();
            toastXml.LoadXml(toastXmlString);
            return toastXml;
        }
        static XmlDocument getTileXmlForDeadline(Deadline deadline)
        {

            string name = deadline.name;
            string course = deadline.course;
            string due = deadline.ddl;
            string timeLeft = deadline.timeLeft();
                
            string xml = $@"
<tile>
    <visual>

        <binding template=""TileMedium"">
            <text>{name}</text>
            <text hint-style=""captionSubtle"">{course}</text>
            <text hint-style=""captionSubtle"">{due}</text>
            <text hint-style=""captionSubtle"">{timeLeft}</text>
        </binding>

        <binding template=""TileWide"">
            <text hint-style=""body"">{name}</text>
            <text hint-style=""captionSubtle"">{course}</text>
            <text hint-style=""captionSubtle"">{due}, {timeLeft}</text>
            <text hint-style=""captionSubtle"">更新于 {DateTime.Now}</text>
        </binding>

        <binding template=""TileLarge"">
            <text hint-style=""title"">{name}</text>
            <text hint-style=""bodySubtle"">{course}</text>
            <text hint-style=""bodySubtle"">Deadline {due}</text>
            <text hint-style=""body"">{timeLeft}</text>
            <text hint-style=""captionSubtle"">更新于 {DateTime.Now}</text>
        </binding>

    </visual>
</tile>";

            // Load the string into an XmlDocument
            XmlDocument doc = new XmlDocument();
            doc.LoadXml(xml);

            return doc;
        }
        static XmlDocument getTileXmlForCalendar(Semester sem)
        {
            var now = DateTime.Now;

            string[] weekDayNames = {"日", "一", "二", "三", "四", "五", "六"};
            var weekday = "星期" + weekDayNames[Convert.ToInt32(now.DayOfWeek)];

            var shortdate = now.ToString("M 月 d 日");
            var date = now.ToString("yyyy 年 M 月 d 日");

            var nameGroup = Regex.Match(
                sem.semesterEname
                    .Replace("Summer", "夏季学期")
                    .Replace("Spring", "春季学期")
                    .Replace("Autumn", "秋季学期")
                    , @"^(\d+-\d+)-(\w+)$").Groups;

            var week = $"校历第 {sem.getWeekName()} 周";

            string xml = $@"
<tile>
    <visual>

        <binding template=""TileMedium"">
            <text hint-style=""body"">{week}</text>
            <text hint-style=""captionSubtle"">{nameGroup[2]}</text>
            <text hint-style=""caption"">{weekday}</text>
            <text hint-style=""captionSubtle"">{shortdate}</text>
        </binding>

        <binding template=""TileWide"">
            <text hint-style=""body"">{week}</text>
            <text hint-style=""captionSubtle"">{nameGroup[0]}</text>
            <text hint-style=""caption"">{weekday}</text>
            <text hint-style=""captionSubtle"">{date}</text>
        </binding>

        <binding template=""TileLarge"">
            <text hint-style=""title"">{week}</text>
            <text hint-style=""bodySubtle"">{nameGroup[0]}</text>
            <text hint-style=""body"">{weekday}</text>
            <text hint-style=""bodySubtle"">{date}</text>
        </binding>

    </visual>
</tile>";

            // Load the string into an XmlDocument
            XmlDocument doc = new XmlDocument();
            doc.LoadXml(xml);

            return doc;
        }
    }
}
