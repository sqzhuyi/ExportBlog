using System;
using System.Collections.Generic;

using System.Text;
using System.Text.RegularExpressions;

namespace ExportBlog
{
    internal class B51CtoService : IFeedService
    {
        string url = null;
        Regex reg_title = new Regex(@"<li><span>(.+?)</span><span class=""artList_tit""><a href=""(.+?)"">(.+?)</a>", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        Regex reg_title2 = new Regex(@"<div class=""showTitle"">([\s\S]+?)</div>", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        Regex reg_con = new Regex(@"<div class=""showContent"">([\s\S]+?)</div><\!--正文 end-->", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        Regex reg_html = new Regex(@"<.+?>", RegexOptions.Compiled);

        WebUtility web = null;

        public B51CtoService(string un)
        {
            url = "http://" + un + ".blog.51cto.com";
            
            web = new WebUtility();
            web.Encode = Encoding.GetEncoding("gb2312");
            web.URL = url;
            string html = web.Get();
            var m = Regex.Match(html, @"<a href=""/all/(.+?)"" class=""fr"">");
            if (m.Success)
            {
                url += "/all/" + m.Groups[1].Value + "/page/{0}";
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
                    var mp = Regex.Match(html, @"页数 \( 1/(\d+) \)");
                    if (mp.Success) p = App.ToInt(mp.Groups[1].Value);
                    else p = 1;
                }

                var mats = reg_title.Matches(html);
                if (mats.Count == 0) break;
                foreach (Match mat in mats)
                {
                    var fd = new FeedEntity();
                    fd.Url = "http://" + url.Split('/')[2] + mat.Groups[2].Value;
                    fd.Title = mat.Groups[3].Value;
                    
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
                entity.Title = reg_html.Replace(mat.Groups[1].Value, string.Empty).Trim();
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
