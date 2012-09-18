using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Text.RegularExpressions;
using iTextSharp.text;
using iTextSharp.text.pdf;

namespace ExportBlog
{
    public class TxtPackage
    {
        string baseDir = App.BaseDirectory;
        Encoding encode = Encoding.GetEncoding("UTF-8");//gb18030
        string _fileName;
        string _title;
        FeedService feedService;
        string[] artUrls;
        Action<string> _callback;
        IList<FeedEntity> items;


        public TxtPackage(string fileName, string title, FeedService fs, string[] urls, Action<string> callback)
        {
            this._fileName = fileName;
            this._title = title;
            this.feedService = fs;
            this.artUrls = urls;
            this._callback = callback;

            Init(urls == null);
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

            int cnt = items.Count;

            for (int i = cnt - 1; i >= 0; i--)
            {
                var entity = items[i];
                if (!entity.IsDown)
                {
                    continue;
                }
                _callback("获取文章 " + (cnt - i) + "/" + cnt + "：" + entity.Title);

                CreateFile(baseDir + GetFileName(entity.Title) + ".txt", GetContent(entity));
            }
        }

        private void Build2()
        {
            int cnt = artUrls.Length;

            _callback("共有【" + cnt + "】篇文章等待导出");

            int i = 0;
            
            foreach (string url in artUrls)
            {
                i++;

                var entity = feedService.GetEntity(url);
                CreateFile(baseDir + GetFileName(entity.Title) + ".txt", GetContent(entity));

                _callback("已获取文章 " + i + "/" + cnt + "：" + entity.Title);
            }
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
        Regex reg_br = new Regex(@"<(/p|/div|br[\s/]*)>[\r\n]*?", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        Regex reg_html = new Regex(@"<.+?>", RegexOptions.Compiled);

        private string GetContent(FeedEntity entity)
        {
            if (!feedService.GetContent(ref entity))
            {
                return string.Empty;
            }

            string html = entity.Content;
            html = reg_br.Replace(html, "\n");
            html = reg_html.Replace(html, string.Empty);
            html = App.ToHtmlDecoded(html).Replace("\r", string.Empty).Replace("\n", "\r\n");

            return html;
        }
        #endregion

    }
    
}
