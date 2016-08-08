using System;
using System.Linq;
using System.Threading.Tasks;

using Windows.UI.Notifications;
using Windows.Data.Xml.Dom;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Collections.Generic;

namespace TsinghuaUWP {
    public class Notification {
        static public async Task<int> update(bool forceRemote = false, bool calendarOnly = false) {
            Debug.WriteLine("[Notification] update begin");

            var updater = TileUpdateManager.CreateTileUpdaterForApplication();
            updater.EnableNotificationQueue(true);
            updater.Clear(); //TODO: should always clear?

            int tileCount = 0;

            var notifier = ToastNotificationManager.CreateToastNotifier();

            try {
                if (!calendarOnly && !DataAccess.credentialAbsent()) {
                    Debug.WriteLine("[Notification] credential exist");

                    //deadlines
                    var deadlines = DataAccess.sortDeadlines(
                        (from a in await DataAccess.getAllDeadlines(forceRemote)
                         where !a.hasBeenFinished && !a.shouldBeIgnored()
                         select a).ToList());

                    foreach (var deadline in deadlines) {
                        if (!deadline.isPast()
                            && (tileCount + 1) < 5) {
                            var tiles = getTilesForDeadline(deadline, DateTime.Now);
                            foreach (var tile in tiles) {
                                updater.AddToSchedule(tile);
                            }
                            tileCount++;
                        }

                        if (!deadline.hasBeenFinished) {
                            if (!deadline.hasBeenToasted()) {
                                deadline.mark_as_toasted();
                                var toast = new ToastNotification(getToastXmlForDeadline(deadline));
                                notifier.Show(toast);
                            }
                        }
                    }
                }
            } catch (Exception e) {
                Debug.WriteLine("[Notification] error dealing with deadlines: " + e.Message);
                throw e;
            }

            //calendar
            try {
                if (tileCount < 5) {
                    var sem = await DataAccess.getSemester(forceRemote);
                    updater.Update(getTodayTileForCalendar(sem));
                    updater.AddToSchedule(getScheduledTileForCalendar(sem, DateTime.Now.Date.AddDays(1)));
                }
            } catch (Exception e) {
                Debug.WriteLine("[Notification] error dealing with calendar: " + e.Message);
                throw e;
            }

            Debug.WriteLine("[Notification] update finished");
            return 0;
        }



        private static TileNotification getTodayTileForCalendar(Semester sem) {
            var today = DateTime.Now.Date;
            return new TileNotification(getTileXmlForCalendar(sem, today)) {
                ExpirationTime = today.AddDays(1),
                Tag = "校历"
            };
        }

        private static ScheduledTileNotification getScheduledTileForCalendar(Semester sem, DateTime _day) {
            var day = _day.Date;
            return new ScheduledTileNotification(getTileXmlForCalendar(sem, day), day) {
                ExpirationTime = day.AddDays(1),
                Tag = "校历"
            };
        }

        private static List<ScheduledTileNotification> getScheduledTilesForCalendar(Semester sem) {

            var list = new List<ScheduledTileNotification>();

            if (sem.endDate == null) {
                return list;
            }

            var start = DateTime.Parse(sem.startDate).Date;
            var end = DateTime.Parse(sem.endDate).Date;


            for (DateTime day = start; day <= end; day = day.AddDays(1)) {
                list.Add(getScheduledTileForCalendar(sem, day));
            }

            return list;
        }

        //XML template renderings

        private static XmlDocument getToastXmlForDeadline(Deadline deadline) {

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

        private static List<ScheduledTileNotification> getTilesForDeadline(Deadline deadline, DateTime _now) {
            var list = new List<ScheduledTileNotification>();

            while (_now < deadline.due()) {
                var countdown = deadline.countdown(_now);
                var xml = getTileXmlForDeadline(deadline, countdown.text);
                list.Add(new ScheduledTileNotification(xml, countdown.validFrom.AddSeconds(5)) {
                    Tag = deadline.tag(),
                    ExpirationTime = _now.Add(countdown.validDuration)
                });


                _now += countdown.validDuration;
            }
            return list;
        }

        private static XmlDocument getTileXmlForDeadline(Deadline deadline, string text = "") {

            string name = deadline.name;
            string course = deadline.course;
            string due = deadline.ddl;
            string timeLeft = text != "" ? text : deadline.timeLeft();

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
            <text hint-style=""bodySubtle"">截止于 {due}</text>
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

        private static XmlDocument getTileXmlForCalendar(Semester sem, DateTime date) {

            string[] weekDayNames = { "日", "一", "二", "三", "四", "五", "六" };
            var weekday = "星期" + weekDayNames[Convert.ToInt32(date.DayOfWeek)];

            var shortdate = date.ToString("M 月 d 日");
            var longdate = date.ToString("yyyy 年 M 月 d 日");

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
            <text hint-style=""captionSubtle"">{longdate}</text>
        </binding>

        <binding template=""TileLarge"">
            <text hint-style=""title"">{week}</text>
            <text hint-style=""bodySubtle"">{nameGroup[0]}</text>
            <text hint-style=""body"">{weekday}</text>
            <text hint-style=""bodySubtle"">{longdate}</text>
            <text hint-style=""captionSubtle"">更新于 {DateTime.Now}</text>
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
