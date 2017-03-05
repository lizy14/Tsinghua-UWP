using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TsinghuaUWP.TsinghuaTVs
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
            items.Add(new TV1 { URL = "https://iptv.tsinghua.edu.cn/hls/cctv1hd.m3u8", Category = "hd", Headline = "CCTV-1高清" });
            items.Add(new TV1 { URL = "https://iptv.tsinghua.edu.cn/hls/cctv3hd.m3u8", Category = "hd", Headline = "CCTV-3高清" });
            items.Add(new TV1 { URL = "https://iptv.tsinghua.edu.cn/hls/cctv5hd.m3u8", Category = "hd", Headline = "CCTV-5高清" });
            items.Add(new TV1 { URL = "https://iptv.tsinghua.edu.cn/hls/cctv5phd.m3u8", Category = "hd", Headline = "CCTV-5+高清" });
            items.Add(new TV1 { URL = "https://iptv.tsinghua.edu.cn/hls/cctv6hd.m3u8", Category = "hd", Headline = "CCTV-6高清" });
            items.Add(new TV1 { URL = "https://iptv.tsinghua.edu.cn/hls/cctv8hd.m3u8", Category = "hd", Headline = "CCTV-8高清" });
            items.Add(new TV1 { URL = "https://iptv.tsinghua.edu.cn/hls/btv1hd.m3u8", Category = "hd", Headline = "北京卫视高清" });
            items.Add(new TV1 { URL = "https://iptv.tsinghua.edu.cn/hls/zjhd.m3u8", Category = "hd", Headline = "浙江卫视高清" });
            items.Add(new TV1 { URL = "https://iptv.tsinghua.edu.cn/hls/jshd.m3u8", Category = "hd", Headline = "江苏卫视高清" });
            items.Add(new TV1 { URL = "https://iptv.tsinghua.edu.cn/hls/dfhd.m3u8", Category = "hd", Headline = "东方卫视高清" });
            items.Add(new TV1 { URL = "https://iptv.tsinghua.edu.cn/hls/szhd.m3u8", Category = "hd", Headline = "深圳卫视高清" });
            items.Add(new TV1 { URL = "https://iptv.tsinghua.edu.cn/hls/gdhd.m3u8", Category = "hd", Headline = "广东卫视高清" });
            items.Add(new TV1 { URL = "https://iptv.tsinghua.edu.cn/hls/tjhd.m3u8", Category = "hd", Headline = "天津卫视高清" });
            items.Add(new TV1 { URL = "https://iptv.tsinghua.edu.cn/hls/hbhd.m3u8", Category = "hd", Headline = "湖北卫视高清" });
            items.Add(new TV1 { URL = "https://iptv.tsinghua.edu.cn/hls/hunanhd.m3u8", Category = "hd", Headline = "湖南卫视高清" });
            items.Add(new TV1 { URL = "https://iptv.tsinghua.edu.cn/hls/sdhd.m3u8", Category = "hd", Headline = "山东卫视高清" });
            items.Add(new TV1 { URL = "https://iptv.tsinghua.edu.cn/hls/cqhd.m3u8", Category = "hd", Headline = "重庆卫视高清" });
            items.Add(new TV1 { URL = "https://iptv.tsinghua.edu.cn/hls/schd.m3u8", Category = "hd", Headline = "四川卫视高清" });
            items.Add(new TV1 { URL = "https://iptv.tsinghua.edu.cn/hls/jxhd.m3u8", Category = "hd", Headline = "江西卫视高清" });
            items.Add(new TV1 { URL = "https://iptv.tsinghua.edu.cn/hls/hljhd.m3u8", Category = "hd", Headline = "黑龙江卫视高清" });
            items.Add(new TV1 { URL = "https://iptv.tsinghua.edu.cn/hls/lnhd.m3u8", Category = "hd", Headline = "辽宁卫视高清" });
            items.Add(new TV1 { URL = "https://iptv.tsinghua.edu.cn/hls/ahhd.m3u8", Category = "hd", Headline = "安徽卫视高清" });
            items.Add(new TV1 { URL = "https://iptv.tsinghua.edu.cn/hls/fhwszwt.m3u8", Category = "hd", Headline = "凤凰卫视中文台" });
            items.Add(new TV1 { URL = "https://iptv.tsinghua.edu.cn/hls/discovery.m3u8", Category = "hd", Headline = "Discovery" });
            return items;
        }

    }
}
