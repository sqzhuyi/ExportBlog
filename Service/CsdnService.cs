using System;
using System.Collections.Generic;

using System.Text;
using System.Text.RegularExpressions;

namespace ExportBlog
{
    internal class CsdnService : IFeedService
    {
        string url = null;
        Regex reg_title = new Regex(@"<span class=""link_title""><a href=""(.+?)"">([^<]+?)</a></span>", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        Regex reg_con = new Regex(@"<div id=""article_content"" class=""article_content"">([\s\S]+)</div>\s*<div class=""share_buttons"" id=""sharePanel"">", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        
        WebUtility web = null;

        public CsdnService(string un)
        {
            url = "http://blog.csdn.com/" + un + "/article/list/{0}?viewmode=contents";

            web = new WebUtility();
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
                    fd.Url = "http://blog.csdn.com" + mat.Groups[1].Value;
                    fd.Title = mat.Groups[2].Value.Trim();

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
            Match mat = reg_title.Match(html);
            if (mat.Success)
            {
                entity.Title = mat.Groups[2].Value.Trim();
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
