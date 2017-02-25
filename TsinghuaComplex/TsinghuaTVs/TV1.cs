using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TsinghuaComplex.TsinghuaTVs
{
    public class TV1
    {
        public string URL { get; set; }
        public string Category { get; set; }
        public string Headline { get; set; }
        public string Categoty { get; internal set; }
    }

    public class TVManager
    {
        public static List<TV1> GETTV(string category, ObservableCollection<TV1> newsItems)
        {
            var allItems = getNewsItems();
            var filteredNewsItems = allItems.Where(p => p.Category == category).ToList();
            newsItems.Clear();
            filteredNewsItems.ForEach(p => newsItems.Add(p));
            return filteredNewsItems;
        }
        private static List<TV1> getNewsItems()
        {
            var items = new List<TV1>();
            items.Add(new TV1 { URL = "https://iptv.tsinghua.edu.cn/player.html?vid=cctv1hd", Category = "hd", Headline = "CCTV-1高清" });
            items.Add(new TV1 { URL = "https://iptv.tsinghua.edu.cn/player.html?vid=cctv3hd", Category = "hd", Headline = "CCTV-3高清" });
            items.Add(new TV1 { URL = "https://iptv.tsinghua.edu.cn/player.html?vid=cctv5hd", Category = "hd", Headline = "CCTV-5高清" });
            items.Add(new TV1 { URL = "https://iptv.tsinghua.edu.cn/player.html?vid=cctv5phd", Category = "hd", Headline = "CCTV-5+高清" });
            items.Add(new TV1 { URL = "https://iptv.tsinghua.edu.cn/player.html?vid=cctv6hd", Category = "hd", Headline = "CCTV-6高清" });
            items.Add(new TV1 { URL = "https://iptv.tsinghua.edu.cn/player.html?vid=cctv8hd", Category = "hd", Headline = "CCTV-8高清" });
            items.Add(new TV1 { URL = "https://iptv.tsinghua.edu.cn/player.html?vid=btv1hd", Category = "hd", Headline = "北京卫视高清" });
            items.Add(new TV1 { URL = "https://iptv.tsinghua.edu.cn/player.html?vid=zjhd", Category = "hd", Headline = "浙江卫视高清" });
            items.Add(new TV1 { URL = "https://iptv.tsinghua.edu.cn/player.html?vid=jshd", Category = "hd", Headline = "江苏卫视高清" });
            items.Add(new TV1 { URL = "https://iptv.tsinghua.edu.cn/player.html?vid=dfhd", Category = "hd", Headline = "东方卫视高清" });
            items.Add(new TV1 { URL = "https://iptv.tsinghua.edu.cn/player.html?vid=szhd", Category = "hd", Headline = "深圳卫视高清" });
            items.Add(new TV1 { URL = "https://iptv.tsinghua.edu.cn/player.html?vid=gdhd", Category = "hd", Headline = "广东卫视高清" });
            items.Add(new TV1 { URL = "https://iptv.tsinghua.edu.cn/player.html?vid=tjhd", Category = "hd", Headline = "天津卫视高清" });
            items.Add(new TV1 { URL = "https://iptv.tsinghua.edu.cn/player.html?vid=hbhd", Category = "hd", Headline = "湖北卫视高清" });
            items.Add(new TV1 { URL = "https://iptv.tsinghua.edu.cn/player.html?vid=hunanhd", Category = "hd", Headline = "湖南卫视高清" });
            items.Add(new TV1 { URL = "https://iptv.tsinghua.edu.cn/player.html?vid=sdhd", Category = "hd", Headline = "山东卫视高清" });
            items.Add(new TV1 { URL = "https://iptv.tsinghua.edu.cn/player.html?vid=cqhd", Category = "hd", Headline = "重庆卫视高清" });
            items.Add(new TV1 { URL = "https://iptv.tsinghua.edu.cn/player.html?vid=schd", Category = "hd", Headline = "四川卫视高清" });
            items.Add(new TV1 { URL = "https://iptv.tsinghua.edu.cn/player.html?vid=jxhd", Category = "hd", Headline = "江西卫视高清" });
            items.Add(new TV1 { URL = "https://iptv.tsinghua.edu.cn/player.html?vid=hljhd", Category = "hd", Headline = "黑龙江卫视高清" });
            items.Add(new TV1 { URL = "https://iptv.tsinghua.edu.cn/player.html?vid=lnhd", Category = "hd", Headline = "辽宁卫视高清" });
            items.Add(new TV1 { URL = "https://iptv.tsinghua.edu.cn/player.html?vid=ahhd", Category = "hd", Headline = "安徽卫视高清" });
            items.Add(new TV1 { URL = "https://iptv.tsinghua.edu.cn/player.html?vid=fhwszwt", Category = "hd", Headline = "凤凰卫视中文台" });
            items.Add(new TV1 { URL = "https://iptv.tsinghua.edu.cn/player.html?vid=discovery", Category = "hd", Headline = "Discovery" });
            return items;
        }

    }
}
