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
                        if (!deadline.hasBeenFinished) {
                            if (!deadline.hasBeenToasted()) {
                                deadline.mark_as_toasted();
                                var toast = new ToastNotification(getToastXmlForDeadline(deadline));
                                notifier.Show(toast);
                            }
                        }
                    }

                    //tile 
                    try
                    {
                        updater.Update(new TileNotification(getTileXmlForDeadlines(deadlines, semester)));
                    }
                    catch (Exception e)
                    {
                        Debug.WriteLine("[Notification] error dealing with calendar: " + e.Message);
                        throw e;
                    }
                }
            } catch (Exception e) {
                Debug.WriteLine("[Notification] error dealing with deadlines: " + e.Message);
                throw e;
            }



            Debug.WriteLine("[Notification] update finished");
            return 0;
        }
        private static string getWeekday() {
            var now = DateTime.Now;
            string[] weekDayNames = { "日", "一", "二", "三", "四", "五", "六" };
            var weekday = "周" + weekDayNames[Convert.ToInt32(now.DayOfWeek)];
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

        private static XmlDocument getTileXmlForDeadlines(List<Deadline> deadlines, Semester semester) {
            
            string medium;
            string wide;
            string large;
            if (deadlines.Count == 0) {
                medium = @"
            <text hint-style=""title""  hint-align=""center"">🎉</text>
            <text hint-style=""body"" hint-align=""center"">Hooray!</text>
            <text hint-style=""captionSubtle""  hint-align=""center"">暂无未交作业</text>
";
                wide = medium;
                large = @"
            <text hint-style=""bodySubtle"" hint-align=""center""/>
            <text hint-style=""header"" hint-align=""center"">🎉</text>
            <text hint-style=""subheader"" hint-align=""center"">Hooray!</text>
            <text hint-style=""bodySubtle"" hint-align=""center"">暂无未交作业</text>
";
            }
            else {
                medium = $@"
<text 
    hint-wrap=""true"" 
    hint-maxLines=""3"">{deadlines[0].name}</text>
<text hint-style=""captionSubtle"">{deadlines[0].timeLeft()}</text>
<text hint-style=""captionSubtle""
    hint-wrap=""true"" 
    hint-maxLines=""2"">{deadlines[0].course}</text>
";
                wide = "";
                large = "";
                bool first = true;
                int counter = 0;
                foreach(var deadline in deadlines)
                {
                    counter++;
                    if (counter > 5){
                        break;
                    }
                        wide += $@"
<text hint-style=""caption{(first? "": "Subtle")}"">{deadline.timeLeft().Replace("只剩", "").Replace("还有", "")} · {deadline.name} - {deadline.course}</text>
";
                    
                    large += $@"
<group>
    <subgroup>
            <text hint-style=""caption"">{deadline.timeLeft().Replace("只剩", "").Replace("还有", "")} · {deadline.name}</text>
            <text hint-style=""captionSubtle"" hint-align=""right"">{deadline.course}</text>
    </subgroup>
</group>
";
                    first = false;
                }
            }
            
            string semesterName= Regex.Match(semester.semesterEname
                    .Replace("Summer", "夏")
                    .Replace("Spring", "春")
                    .Replace("Autumn", "秋")
                    , @"^(\d+-\d+)-(\w+)$").Groups[0].ToString();

            string xml = $@"
<tile>
    <visual 
        displayName=""第 {semester.getWeekName()} 周{getWeekday()}, {semesterName}"">

        <binding template=""TileMedium""
            branding=""name"">
            {medium}
        </binding>

        <binding template=""TileWide""
            branding=""nameAndLogo"">
            {wide}
        </binding>

        <binding template=""TileLarge""
            branding=""nameAndLogo"">
            {large}
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
