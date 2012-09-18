using System;
using System.Collections.Generic;

using System.Text;
using System.Text.RegularExpressions;

namespace ExportBlog
{
    internal class SinaService : IFeedService
    {
        string url = null;
        Regex reg_title = new Regex(@"<span class=""atc_title"">\s+?<a [^>]+?href=""(.+?)"">([^<]+?)</a>", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        Regex reg_title2 = new Regex(@"class=""titName SG_txta"">(.+?)</h2>", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        Regex reg_con = new Regex(@"<\!-- 正文开始 -->([\s\S]+)<\!-- 正文结束 -->", RegexOptions.Compiled);

        WebUtility web = null;

        public SinaService(string un)
        {
            url = "http://photo.blog.sina.com.cn/" + un;

            web = new WebUtility();
            web.URL = url;
            string html = web.Get();
            Match mat = Regex.Match(html, @"(?i)<a href=""(.+?)"">博文目录</a>");
            if (mat.Success)
            {
                url = mat.Groups[1].Value.Replace("_1.html", "_{0}.html");
            }
            else
            {
                throw new Exception("获取博客列表失败");
            }
        }

        public IList<FeedEntity> GetList()
        {
            var list = new List<FeedEntity>();
            
            int p = 0;
            for (int i = 1; i < 1000; i++)
            {
                if (p > 0 && i > p) break;

                web.URL = string.Format(url, i);
                string html = web.Get();
                if (p == 0)
                {
                    var mp = Regex.Match(html, @"共(\d+)页");
                    if (mp.Success) p = App.ToInt(mp.Groups[1].Value);
                    else p = 1;
                }

                var mats = reg_title.Matches(html);
                if (mats.Count == 0) break;
                foreach (Match mat in mats)
                {
                    var fd = new FeedEntity();
                    fd.Url = mat.Groups[1].Value;
                    fd.Title = mat.Groups[2].Value;

                    if (fd.Title.StartsWith("[转载]"))
                    {
                        fd.Title = fd.Title.Substring(4);
                    }

                    list.Add(fd);
                }
            }
            return list;
        }

        public bool GetContent(ref FeedEntity entity)
        {
            web.URL = entity.Url;
            string html = web.Get();
            Match mat = reg_con.Match(html);
            if (mat.Success)
            {
                entity.Content = mat.Groups[1].Value.Trim();
            }
            return mat.Success;
        }
        public FeedEntity GetEntity(string url)
        {
            var entity = new FeedEntity();

            web.URL = url;
            string html = web.Get();
            Match mat = reg_title2.Match(html);
            if (mat.Success)
            {
                entity.Title = mat.Groups[1].Value.Trim();
            }
            mat = reg_con.Match(html);
            if (mat.Success)
            {
                entity.Content = mat.Groups[1].Value.Trim();
            }
            return entity;
        }
    }
}
