using System;
using System.Collections.Generic;

using System.Text;
using System.Text.RegularExpressions;

namespace ExportBlog
{
    internal class ItEyeService : IFeedService
    {
        string url = null;
        Regex reg_item = new Regex(@"<div class=""blog_main"">[\s\S]+?<div class=""blog_bottom"">", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        Regex reg_title = new Regex(@"<h3><a href='(.+?)'>([^<]+?)</a></h3>", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        Regex reg_title2 = new Regex(@"<h3>[^>]+?>(.+?)</a>", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        Regex reg_con = new Regex(@"<div id=""blog_content"" class=""blog_content"">([\s\S]+?)</div>\s+?((<IFRAME SRC)|(<div>\s+<script)|(<div id=""bottoms""))", RegexOptions.IgnoreCase | RegexOptions.Compiled);

        WebUtility web = null;

        public ItEyeService(string un)
        {
            url = "http://" + un + ".iteye.com/?page={0}";

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
                    var mp = Regex.Match(html, @"<a href=""/\?page=(\d+)""[^>]*>[^<]+?</a> <a href=""/\?page=2"" class=""next_page""");
                    if (mp.Success) p = App.ToInt(mp.Groups[1].Value);
                    else p = 1;
                }
                var mats = reg_item.Matches(html);
                if (mats.Count == 0) break;
                foreach (Match mat in mats)
                {
                    Match m_t = reg_title.Match(mat.Value);
                    if (!m_t.Success) continue;

                    var fd = new FeedEntity();
                    fd.Url = url.Split('?')[0] + m_t.Groups[1].Value.TrimStart('/');
                    fd.Title = m_t.Groups[2].Value;
                    
                    list.Add(fd);
                }
                System.Threading.Thread.Sleep(1000);
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
