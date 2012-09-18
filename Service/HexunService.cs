using System;
using System.Collections.Generic;

using System.Text;
using System.Text.RegularExpressions;

namespace ExportBlog
{
    internal class HexunService : IFeedService
    {
        string url = null;
        Regex reg_item = new Regex(@"<div class='Article'>[\s\S]+?收藏</a></div>", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        Regex reg_title = new Regex(@"<a href='(.+?)'>(.+?)</a>", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        Regex reg_title2 = new Regex(@"<span class=""ArticleTitleText"">([\s\S]+?)</span>", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        Regex reg_con = new Regex(@"<div id=""BlogArticleDetail"" style=""font-size:14px;"">([\s\S]+?)</div>\s+</div>\s+<style>", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        Regex reg_html = new Regex(@"<.+?>", RegexOptions.Compiled);

        WebUtility web = null;

        public HexunService(string un)
        {
            url = "http://" + un + ".blog.hexun.com/p{0}/default.html";

            web = new WebUtility();
            web.Encode = Encoding.GetEncoding("gb2312");
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
                    var mp = Regex.Match(html, @"第(\d+)页");
                    if (mp.Success) p = App.ToInt(mp.Groups[1].Value);
                    else
                    {
                        var mps = Regex.Matches(html, @">(\d+)</a><span class='pagesplit'>");
                        if (mps.Count > 0)
                        {
                            p = App.ToInt(mps[mps.Count - 1].Groups[1].Value);
                        }
                        else p = 1;
                    }
                }

                var mats = reg_item.Matches(html);
                if (mats.Count == 0) break;
                foreach (Match mat in mats)
                {
                    var m_t = reg_title.Match(mat.Value);
                    var fd = new FeedEntity();
                    fd.Url = m_t.Groups[1].Value;
                    fd.Title = m_t.Groups[2].Value;

                    if (fd.Title == "本文被设定为主人好友可见")
                    {
                        continue;
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
