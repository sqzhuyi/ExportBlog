using System;
using System.Collections.Generic;

using System.Text;
using System.Text.RegularExpressions;

namespace ExportBlog
{
    internal class ChinaUnixService : IFeedService
    {
        string url = null;
        Regex reg_title = new Regex(@"·<a href=""(.+?)"" target=""_blank"">([^<]+?)</a>", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        Regex reg_title2 = new Regex(@"<a href=""javascript:;"">([^<]+?)</a>", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        Regex reg_con = new Regex(@"<div id=""detail""[^>]+?>([\s\S]+)</div>\s+?</div>\s+?<div class=""cont5"">", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        
        WebUtility web = null;

        public ChinaUnixService(string un)
        {
            url = "http://blog.chinaunix.net/uid/" + un + "/frmd/-1/page/{0}.html";

            web = new WebUtility();
            web.Encode = Encoding.GetEncoding("gbk");
        }

        public IList<FeedEntity> GetList()
        {
            var list = new List<FeedEntity>();
                        
            for (int i = 1; i < 1000; i++)
            {
                web.URL = string.Format(url, i);
                string html = web.Get();
                var mats = reg_title.Matches(html);
                if (mats.Count == 0) break;
                foreach (Match mat in mats)
                {
                    var fd = new FeedEntity();
                    fd.Url = "http://blog.chinaunix.net" + mat.Groups[1].Value;
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
