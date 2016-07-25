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
    public class TileAndToast
    {
        static public async Task<int> update()
        {
            
            try
            {
                Debug.WriteLine("Updating tile and toast");
                
                
                var updater = TileUpdateManager.CreateTileUpdaterForApplication();
                updater.EnableNotificationQueue(true);
                updater.Clear();
                int tileCount = 0;

                var notifier = ToastNotificationManager.CreateToastNotifier();

                //deadlines
                var deadlines = await DataAccess.getDeadlines();
                foreach (var deadline in deadlines)
                {
                    continue;
                    if(!deadline.hasBeenFinished && !deadline.isPast())
                    {
                        updater.Update(new TileNotification(getTileXmlForDeadline(deadline)));
                        tileCount++;
                        notifier.Show(new ToastNotification(getToastXmlForDeadline(deadline)));
                    }
                    
                }

                //calendar
                if(tileCount < 5)
                {
                    updater.Update(new TileNotification(getTileXmlForCalendar(await DataAccess.getCalendar())));
                }
                
                Debug.WriteLine("Tile and toast updated");
            }
            catch (Exception e)
            {
                Debug.WriteLine("Error updating notifications" + e.Message);
                return 1;
            }
            


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
    <text>{deadline.timeLeft()}</text>
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
            <text hint-style=""bodySubtle"">{due}</text>
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
        static XmlDocument getTileXmlForCalendar(Calendar calendar)
        {

            var now = DateTime.Now;
            var semester = Regex.Match(calendar.currentSemester.semesterName, @"^(\d+-\d+)-(\w+)$").Groups;
            var week = $"校历第{calendar.currentTeachingWeek.weekName}周";
            string xml = $@"
<tile>
    <visual>

        <binding template=""TileMedium"">
            <text>{week}</text>
            <text hint-style=""captionSubtle"">{semester[2]}</text>
            <text hint-style=""captionSubtle"">{semester[1]}</text>
            <text hint-style=""captionSubtle"">{now.ToString("d")}</text>
        </binding>

        <binding template=""TileWide"">
            <text hint-style=""body"">{week}</text>
            <text hint-style=""captionSubtle"">{semester[0]}</text>
            <text hint-style=""captionSubtle"">{now.ToString("D")}</text>
        </binding>

        <binding template=""TileLarge"">
            <text hint-style=""title"">{week}</text>
            <text hint-style=""bodySubtle"">{semester[0]}</text>
            <text hint-style=""bodySubtle"">{now.ToString("D")}</text>
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
