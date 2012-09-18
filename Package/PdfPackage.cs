using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Text.RegularExpressions;
using iTextSharp.text;
using iTextSharp.text.pdf;

//http://www.cnblogs.com/CareySon/archive/2011/11/07/2239017.html

namespace ExportBlog
{
    public class PdfPackage
    {
        string baseDir = App.BaseDirectory;
        Encoding encode = Encoding.GetEncoding("GB18030");//gb18030
        string _fileName;
        string _title;
        FeedService feedService;
        string[] artUrls;
        Action<string> _callback;
        IList<FeedEntity> items;


        public PdfPackage(string fileName, string title, FeedService fs, string[] urls, Action<string> callback)
        {
            this._fileName = fileName + ".pdf";
            this._title = title;
            this.feedService = fs;
            this.artUrls = urls;
            this._callback = callback;

            Init(urls == null);
        }

        private void Init(bool getList)
        {
            if (File.Exists(baseDir + _fileName))
            {
                File.Delete(baseDir + _fileName);
            }
            if (getList)
            {
                _callback("正在获取文章列表");
                items = feedService.GetList();
                _callback("共获取到【" + items.Count + "】篇文章");
            }
            baseFT = CreateChineseFont();
            codeFT = FontFactory.GetFont("Courier", 10, BaseColor.DARK_GRAY);
        }

        BaseFont baseFT = null;
        Font codeFT = null;

        public void Build()
        {
            if (artUrls != null)
            {
                Build2();
                return;
            }
            Document document = new Document();
            PdfWriter.GetInstance(document, new FileStream(baseDir + _fileName, FileMode.Create));
            document.Open();
            document.AddTitle(_title);
            
            Font ft = new Font(baseFT, 12);

            int cnt = items.Count;

            for (int i = cnt - 1; i >= 0; i--)
            {
                var entity = items[i];
                if (!entity.IsDown)
                {
                    continue;
                }
                _callback("获取文章 " + (cnt - i) + "/" + cnt + "：" + entity.Title);
                
                document.Add(GetChapter(entity));
            }
            document.Close();
        }

        private void Build2()
        {
            int cnt = artUrls.Length;

            _callback("共有【" + cnt + "】篇文章等待导出");

            int i = 0;

            Document document = new Document();
            PdfWriter.GetInstance(document, new FileStream(baseDir + _fileName, FileMode.Create));
            document.Open();
            document.AddTitle(_title);

            BaseFont baseFT = CreateChineseFont();

            foreach (string url in artUrls)
            {
                i++;

                var entity = feedService.GetEntity(url);
                
                document.Add(GetChapter(entity));

                _callback("已获取文章 " + i + "/" + cnt + "：" + entity.Title);
            }
            document.Close();
        }

        #region helper
        int chp_idx = 1;
        private Chapter GetChapter(FeedEntity entity)
        {
            Font ft = new Font(baseFT, 12);

            Chapter chp = new Chapter(new Paragraph(entity.Title, new Font(baseFT, 18)) { Alignment = Element.ALIGN_CENTER }, chp_idx++);
            chp.Add(new Paragraph(" "));
            chp.Add(new Paragraph(" "));

            string text = GetContent(entity);
            foreach (string line in text.Split(new char[] { '\n' }, StringSplitOptions.RemoveEmptyEntries))
            {
                try
                {
                    if (line == "[code_begin]")
                    {
                        ft = codeFT;
                    }
                    else if (line == "[code_end]")
                    {
                        ft = new Font(baseFT, 12);
                    }
                    else if (line.StartsWith("[img]"))
                    {
                        Image img = Image.GetInstance(line.Substring(5));
                        if (img.Width > 40)
                        {
                            if (img.Width > 560) img.ScaleToFit(560, 560);
                            img.Alignment = Element.ALIGN_CENTER;
                            chp.Add(img);
                        }
                    }
                    else
                    {
                        chp.Add(new Paragraph(line, ft));
                    }
                }
                catch { }
            }
            return chp;
        }

        Regex reg_code1 = new Regex(@"(<(pre|textarea) [^>]+?>)", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        Regex reg_code2 = new Regex(@"(</(pre|textarea))>", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        Regex reg_img = new Regex(@"<img[^>]+?src=['""]([^>]+?)['""][^>]+?>", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        Regex reg_br = new Regex(@"<(/p|/div|br[\s/]*)>[\r\n]*?", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        Regex reg_html = new Regex(@"<.+?>", RegexOptions.Compiled);

        private string GetContent(FeedEntity entity)
        {
            if (!feedService.GetContent(ref entity))
            {
                return string.Empty;
            }

            string html = entity.Content;
            html = reg_code1.Replace(html, "\n[code_begin]\n");
            html = reg_code2.Replace(html, "\n[code_end]\n");
            html = reg_img.Replace(html, "\n[img]$1\n");
            html = reg_br.Replace(html, "\n");
            html = reg_html.Replace(html, string.Empty);
            html = App.ToHtmlDecoded(html);

            return html;
        }
        private BaseFont CreateChineseFont()
        {
            BaseFont.AddToResourceSearch("iTextAsian.dll");
            BaseFont.AddToResourceSearch("iTextAsianCmaps.dll"); //"STSong-Light", "UniGB-UCS2-H", 
            BaseFont baseFT = BaseFont.CreateFont("STSong-Light", "UniGB-UCS2-H", BaseFont.EMBEDDED);

            return baseFT;
        }
        #endregion

    }
    
}
