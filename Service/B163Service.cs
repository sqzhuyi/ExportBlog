using System;
using System.Collections.Generic;

using System.Text;
using System.Text.RegularExpressions;

namespace ExportBlog
{
    internal class B163Service : IFeedService
    {
        string url = null;
        Regex reg_title = new Regex(@"\.title=""(.+?)"";", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        Regex reg_title2 = new Regex(@"<span class=""tcnt"">(.+?)</span>", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        Regex reg_con = new Regex(@"\.content=""(.+?)"";", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        Regex reg_con2 = new Regex(@"<div class=""nbw-blog-start""></div>([\s\S]+?)<div class=""nbw-blog-end""></div>", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        Regex reg_html = new Regex(@"<.+?>");
        WebUtility web = null;
        string userId = null;

        public B163Service(string un)
        {
            url = "http://blog.163.com/" + un + "/blog/";

            web = new WebUtility();
            web.URL = url;
            string html = web.Get();
            var mat = Regex.Match(html, @"userId:(\d+)");
            if (mat.Success)
            {
                userId = mat.Groups[1].Value;
            }
            else
            {
                throw new Exception("获取博客列表失败");
            }

            url = "http://api.blog.163.com/" + un + "/dwr/call/plaincall/BlogBeanNew.getBlogs.dwr";
        }

        public IList<FeedEntity> GetList()
        {
            var list = new List<FeedEntity>();
            
            web.URL = url;
            web.Referer = "http://api.blog.163.com/crossdomain.html?t=20100205";
            web.ContentType = "text/plain";
            web.TimeOut = 300000;

            string html = web.Post(@"callCount=1
scriptSessionId=${scriptSessionId}187
c0-scriptName=BlogBeanNew
c0-methodName=getBlogs
c0-id=0
c0-param0=number:" + userId + @"
c0-param1=number:0
c0-param2=number:100
batchId=222222", true);

            var m_tit = reg_title.Matches(html);
            if (m_tit.Count == 0) return list;

            var m_con = reg_con.Matches(html);

            for (int i = 0; i < m_tit.Count; i++)
            {
                var fd = new FeedEntity();
                try
                {
                    fd.Title = App.UtoGB(m_tit[i].Groups[1].Value);
                    fd.Content = App.UtoGB(m_con[i].Groups[1].Value);
                }
                catch
                {
                    continue;
                }
                list.Add(fd);
            }

            return list;
        }

        public bool GetContent(ref FeedEntity entity)
        {
            return true;
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
            mat = reg_con2.Match(html);
            if (mat.Success)
            {
                entity.Content = mat.Groups[1].Value.Trim();
            }
            return entity;
        }
    }
}
