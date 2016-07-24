using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Windows.UI.Notifications;
using Windows.Data.Xml.Dom;
using System.Diagnostics;

namespace TsinghuaUWP
{
    public class Tile
    {
        static public async Task<int> update()
        {
            
            try
            {

                Debug.WriteLine("Updating tile");

                var deadline = await Remote.getDeadline();
                // Create the tile notification
                var notification = new TileNotification(Tile.getTileXmlForDeadlines(deadline));

                // And send the notification
                TileUpdateManager.CreateTileUpdaterForApplication().Update(notification);
                Debug.WriteLine("Tile updated");
            }
            catch (Exception e)
            {
                Debug.WriteLine(e.Message);
                return 1;
            }
            


            return 0;
        }

        static XmlDocument getTileXmlForDeadlines(Deadline deadline)
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
    }
}
