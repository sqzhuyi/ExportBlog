using System;
using System.Collections.Generic;

using System.Text;
using System.Text.RegularExpressions;

namespace ExportBlog
{
    internal class SohuService : IFeedService
    {
        string url = null;
        Regex reg_title = new Regex(@"\|&nbsp;<a href=""(.+?)"" target=""_blank"">([^<]+?)</a>", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        Regex reg_title2 = new Regex(@"<h1>(.+?)</h1>", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        Regex reg_con = new Regex(@"<div class=""item-content"" id=""main-content"">([\s\S]+?)<div style=""_width:92px;", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        
        WebUtility web = null;

        public SohuService(string un)
        {
            url = "http://" + un + ".blog.sohu.com/entry/";

            web = new WebUtility();
            web.URL = url;
            web.Encode = Encoding.GetEncoding("gbk");
            string html = web.Get();
            Match mat = Regex.Match(html, @"var _ebi = '(.+?)';");
            if (mat.Success)
            {
                url = "http://" + un + ".blog.sohu.com/action/v_frag-ebi_" + mat.Groups[1].Value + "-pg_{0}/entry/";
            }
            else
            {
                throw new Exception("获取博客列表失败");
            }
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
                    fd.Url = mat.Groups[1].Value;
                    fd.Title = mat.Groups[2].Value;

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
