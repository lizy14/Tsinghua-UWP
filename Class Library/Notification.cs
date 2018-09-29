using System;
using System.Linq;
using System.Threading.Tasks;

using Windows.UI.Notifications;
using Windows.Data.Xml.Dom;
using System.Diagnostics;
using System.Text.RegularExpressions;

namespace TsinghuaUWP {
    public class Notification {

        static private void setBadgeNumber(int num) {

            // Get the blank badge XML payload for a badge number
            XmlDocument badgeXml =
                BadgeUpdateManager.GetTemplateContent(BadgeTemplateType.BadgeNumber);

            // Set the value of the badge in the XML to our number
            XmlElement badgeElement = badgeXml.SelectSingleNode("/badge") as XmlElement;
            badgeElement.SetAttribute("value", num.ToString());

            // Create the badge notification
            BadgeNotification badge = new BadgeNotification(badgeXml);

            // Create the badge updater for the application
            BadgeUpdater badgeUpdater =
                BadgeUpdateManager.CreateBadgeUpdaterForApplication();

            // And update the badge
            badgeUpdater.Update(badge);

        }

        static public async Task<int> update(bool forceRemote = false, bool calendarOnly = false) {
            Debug.WriteLine("[Notification] update begin");

            var updater = TileUpdateManager.CreateTileUpdaterForApplication();
            updater.EnableNotificationQueue(true);
            updater.Clear(); //TODO: should always clear?



            var semester = await DataAccess.getSemester(forceRemote);
            int tileCount = 0;

            var notifier = ToastNotificationManager.CreateToastNotifier();


            try {
                if (!calendarOnly && !DataAccess.credentialAbsent()) {
                    Debug.WriteLine("[Notification] credential exist");

                    //deadlines
                    var unfiltered = await DataAccess.getAllDeadlines(forceRemote);
                    var unsorted = (from a in unfiltered
                                    where !a.hasBeenFinished && !a.shouldBeIgnored()
                                    select a);
                    var deadlines = DataAccess.sortDeadlines(unsorted.ToList());

                    int n = (from a in deadlines where !a.isPast() select a).Count();
                    setBadgeNumber(n);

                    foreach (var deadline in deadlines) {
                        if (!deadline.isPast()
                            && (tileCount + 1) < 5) {
                            var tile = new TileNotification(getTileXmlForDeadline(deadline, semester));
                            updater.Update(tile);
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
                    updater.Update(new TileNotification(getTileXmlForCalendar(semester)));
                }
            } catch (Exception e) {
                Debug.WriteLine("[Notification] error dealing with calendar: " + e.Message);
                throw e;
            }


            Debug.WriteLine("[Notification] update finished");
            return 0;
        }
        private static string getWeekday() {
            var now = DateTime.Now;
            string[] weekDayNames = { "日", "一", "二", "三", "四", "五", "六" };
            var weekday = "星期" + weekDayNames[Convert.ToInt32(now.DayOfWeek)];
            return weekday;
        }
        private static XmlDocument getToastXmlForDeadline(Deadline deadline) {

            // TODO: all values need to be XML escaped

            // Construct the visuals of the toast
            Regex re = new Regex("&[^;]+;");
            string name = re.Replace(deadline.name, " ");
            string course = re.Replace(deadline.course, " ");

            string toastVisual = $@"
<visual>
  <binding template='ToastGeneric'>
    <text>{name}</text>
    <text>新出现 ddl: {deadline.ddl}, {course}</text>
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

        private static XmlDocument getTileXmlForDeadline(Deadline deadline, Semester semester) {

            Regex re = new Regex("&[^;]+;");
            string name = re.Replace(deadline.name, " ");
            string course = re.Replace(deadline.course, " ");

            string due = deadline.ddl;
            string timeLeft = deadline.timeLeft();

            string xml = $@"
<tile>
    <visual 
        displayName=""校历第 {semester.getWeekName()} 周{getWeekday()}"">

        <binding template=""TileMedium""
            branding=""none"">
            <text 
                hint-wrap=""true"" 
                hint-maxLines=""3"">{name}</text>
            <text hint-style=""caption"">{timeLeft}</text>                        
            <text hint-style=""captionSubtle""
                hint-wrap=""true"" 
                hint-maxLines=""2"">{course}</text>
            <text hint-style=""captionSubtle"">{due}</text>
        </binding>

        <binding template=""TileWide""
            branding=""name"">
            <text hint-style=""caption"" 
                hint-wrap=""true"" 
                hint-maxLines=""3"">{name}</text>
            <text hint-style=""caption"">{timeLeft}</text>            
            <text hint-style=""captionSubtle"">{course}</text>
            <text hint-style=""captionSubtle"">截止于 {due}</text>
        </binding>

        <binding template=""TileLarge"" 
            branding=""nameAndLogo"">
            <text hint-style=""subtitle""
                hint-wrap=""true"">{name}</text>
            <text hint-style=""bodySubtle"" 
                hint-wrap=""true"">{course}</text>
            <text hint-style=""body"">{timeLeft}</text>
            <text hint-style=""bodySubtle"">截止于 {due}</text>
            
        </binding>

    </visual>
</tile>";

            // Load the string into an XmlDocument
            XmlDocument doc = new XmlDocument();
            doc.LoadXml(xml);

            return doc;
        }

        private static XmlDocument getTileXmlForCalendar(Semester sem) {
            var now = DateTime.Now;

            var weekday = getWeekday();

            var shortdate = now.ToString("M 月 d 日");
            var date = now.ToString("yyyy 年 M 月 d 日");

            var nameGroup = Regex.Match(
                sem.semesterEname
                    .Replace("Summer", "夏季学期")
                    .Replace("Spring", "春季学期")
                    .Replace("Autumn", "秋季学期")
                    , @"^(\d+-\d+)-(\w+)$").Groups;

            var week = $"校历第{sem.getWeekName()}周";
            var weekLong = $"校历第 {sem.getWeekName()} 周";

            string xml = $@"
<tile>
    <visual 
        branding=""nameAndLogo"">

        <binding template=""TileMedium"">
            <text hint-style=""body"">{week}</text>
            <text hint-style=""captionSubtle"">{nameGroup[2]}</text>
            <text hint-style=""caption"">{weekday}</text>
            <text hint-style=""captionSubtle"">{shortdate}</text>
        </binding>

        <binding template=""TileWide"">
            <text hint-style=""body"">{weekLong}</text>
            <text hint-style=""captionSubtle"">{nameGroup[0]}</text>
            <text hint-style=""caption"">{weekday}</text>
            <text hint-style=""captionSubtle"">{date}</text>
        </binding>

        <binding template=""TileLarge"">
            <text hint-style=""title"">{weekLong}</text>
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
