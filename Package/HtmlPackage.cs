using System;
using System.Collections.Generic;

using System.Text;
using System.IO;
using System.Text.RegularExpressions;

namespace ExportBlog
{
    public class HtmlPackage
    {
        string baseDir = App.BaseDirectory;
        Encoding encode = Encoding.GetEncoding("UTF-8");//gb18030
        string _fileName;
        string _title;
        FeedService feedService;
        string[] artUrls;
        Action<string> _callback;

        IList<FeedEntity> items;


        public HtmlPackage(string fileName, string title, FeedService fs, string[] urls, Action<string> callback)
        {
            this._fileName = fileName;
            this._title = title;
            this.feedService = fs;
            this.artUrls = urls;
            this._callback = callback;

            Init(urls==null);
        }

        private void Init(bool getList)
        {
            baseDir += _fileName;
            if (Directory.Exists(baseDir))
            {
                Directory.Delete(baseDir, true);
            }
            Directory.CreateDirectory(baseDir);
            baseDir += "\\";

            if (getList)
            {
                _callback("正在获取文章列表");
                items = feedService.GetList();
                _callback("共获取到【" + items.Count + "】篇文章");
            }
        }

        public void Build()
        {
            if (artUrls != null)
            {
                Build2();
                return;
            }
            StringBuilder sb = new StringBuilder();
            sb.Append("<ol>");
            int cnt = items.Count;
            for (int i = cnt - 1; i >= 0; i--)
            {
                var entity = items[i];
                if (!entity.IsDown)
                {
                    continue;
                }
                _callback("获取文章 " + (cnt - i) + "/" + cnt + "：" + entity.Title);

                string fileName = GetFileName(entity.Title) + ".htm";

                sb.AppendFormat("<li><a href='{0}'>{1}</a></li>"
                    , fileName, entity.Title);

                if (feedService.GetContent(ref entity))
                {
                    string content = htmlString.Replace("{0}", entity.Title).Replace("\n{1}", entity.Content);
                    CreateFile(baseDir + fileName, content);
                }
            }
            sb.Append("</ol>");
            string content2 = htmlString.Replace("{0}", this._title).Replace("\n{1}", sb.ToString());
            CreateFile(baseDir + "_index.htm", content2);
        }
        private void Build2()
        {
            int cnt = artUrls.Length;

            _callback("共有【" + cnt + "】篇文章等待导出");

            int i = 0;

            StringBuilder sb = new StringBuilder();
            sb.Append("<ol>");
            foreach (string url in artUrls)
            {
                i++;

                var entity = feedService.GetEntity(url);

                string fileName = GetFileName(entity.Title) + ".htm";

                sb.AppendFormat("<li><a href='{0}'>{1}</a></li>"
                    , fileName, entity.Title);

                string content = htmlString.Replace("{0}", entity.Title).Replace("\n{1}", entity.Content);
                CreateFile(baseDir + fileName, content);

                _callback("已获取文章 " + i + "/" + cnt + "：" + entity.Title);
            }
            sb.Append("</ol>");
            string content2 = htmlString.Replace("{0}", this._title).Replace("\n{1}", sb.ToString());
            CreateFile(baseDir + "_index.htm", content2);
        }

        #region helper
        Regex reg_w = new Regex(@"\W+", RegexOptions.Compiled);
        private string GetFileName(string title)
        {
            return reg_w.Replace(title, string.Empty);
        }

        private void CreateFile(string fileName, string content)
        {
            FileStream fs = new FileStream(fileName, FileMode.Create);
            using (StreamWriter writer = new StreamWriter(fs, encode))
            {
                writer.Write(content);
            }
            fs.Dispose(); 
        }
        #endregion

        #region string
        string htmlString = @"<!doctype html>
<html dir=""ltr"" lang=""zh-CN"">
<head>
<title>{0}</title>
<meta http-equiv=""Content-Type"" content=""text/html; charset=utf-8"" />
<style type=""text/css"">
body {font:normal 12px/24px Arial, Helvetica, sans-serif; background:#D9F0DB;}
textarea,pre {font-family:Courier; font-size:12px;}
</style>
</head>
<body>
<p><a href='_index.htm'>&lt;&lt;目录</a></p>
{1}
<p><a href='_index.htm'>&lt;&lt;目录</a></p>
</body>
</html>";
        #endregion

    }
}
