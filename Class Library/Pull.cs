using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TsinghuaUWP
{
    static public class Pull
    {
        static public int getOne()
        {
            return 1;
        }
        static public Deadline getDeadline()
        {
            return new Deadline {
                name = "后期检查",
                course = "程序设计实践",
                due = "2016-07-28"
            };
        }
    }
}
