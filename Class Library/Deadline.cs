using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace TsinghuaUWP
{
    public class Course
    {
        public string id;
        public string name;
        public bool isNew;
        public string semester;

        override public string ToString()
        {
            return "#" + id + ": " + name;
        }
    }
    public class Deadline
    {
        public string name;
        public string ddl;
        public string course;
        public string detail;
        public bool hasBeenFinished;

        private string f(int digit)
        {
            
            return digit == 1 ? "" : "s";
        }
        public string timeLeftEnglish()
        {
            TimeSpan timeDelta = DateTime.Parse(ddl + " 23:59") - DateTime.Now;

            var daysLeft = timeDelta.TotalDays;
            string timeLeft = "";

            if (daysLeft > 1)
            {
                var d = timeDelta.Days + 1;
                timeLeft = d.ToString() + "- day" + f(d) + " left";
            }
            else if (daysLeft > 0)
            {
                var d = timeDelta.Hours + 1;
                timeLeft = d.ToString() + "- hour" + f(d) + " left";
            }
            else if (daysLeft > -1)
            {
                var d = (-timeDelta.Hours);
                timeLeft = d.ToString() + "+ hour" + f(d) + " ago";
            }
            else
            {
                var d = (-timeDelta.Days);
                timeLeft = d.ToString() + "+ day" + f(d) + " ago";
            }
                

            return timeLeft;
        }

        public string timeLeft()
        {
            return timeLeftChinese();
        }
        public string timeLeftChinese()
        {
            TimeSpan timeDelta = DateTime.Parse(ddl + " 23:59") - DateTime.Now;

            var daysLeft = timeDelta.TotalDays;
            string timeLeft = "";

            if (daysLeft > 1)
            {
                var d = timeDelta.Days + 1;
                timeLeft = "只剩 " + d.ToString() + " 天";
            }
            else if (daysLeft > 0)
            {
                var d = timeDelta.Hours + 1;
                timeLeft = "只剩 " + d.ToString() + " 小时";
            }
            else if (daysLeft > -1)
            {
                var d = (-timeDelta.Hours);
                timeLeft = "已经过去" + d.ToString() + " 小时";
            }
            else
            {
                var d = (-timeDelta.Days);
                timeLeft = "已经过去 " + d.ToString() + " 天";
            }


            return timeLeft;
        }

    }

}
