using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Diagnostics;

namespace ExportBlog
{
    public class ChmPackage
    {
        string baseDir = App.BaseDirectory;
        Encoding encode = Encoding.GetEncoding("GB18030");//gb18030
        string _fileName;
        string _title;
        FeedService feedService;
        string[] artUrls;
        Action<string> _callback;

        IList<FeedEntity> items;
        bool isok = true;

        public ChmPackage(string fileName, string title, FeedService fs, string[] urls, Action<string> callback)
        {
            this._fileName = fileName.Replace(".chm", string.Empty);
            this._title = title;
            this.feedService = fs;
            this.artUrls = urls;
            this._callback = callback;

            Init(urls == null);
        }

        private void Init(bool getList)
        {
            if (!CheckChmInstalled())
            {
                isok = false;
                _callback("抱歉，您的电脑没有安装CHM阅读器，无法生成。");
                return;
            }
            baseDir += _fileName;
            if (Directory.Exists(baseDir))
            {
                Directory.Delete(baseDir, true);
            }
            Directory.CreateDirectory(baseDir);
            baseDir += "\\";
            Directory.CreateDirectory(baseDir + "html");

            if (getList)
            {
                _callback("正在获取文章列表");
                items = feedService.GetList();
                _callback("共获取到【" + items.Count + "】篇文章");
            }
        }

        public bool Build()
        {
            if (!isok) return false;

            try
            {
                if (artUrls == null)
                {
                    Build1();
                }
                else
                {
                    Build2();
                }

                Compile();

                Directory.Delete(baseDir, true);

                return true;
            }
            catch (Exception ex)
            {
                _callback("生成失败：" + ex.Message);
                return false;
            }
        }
        private void Build1()
        {
            string fn, content;

            fn = baseDir + _fileName + ".hhp";
            content = string.Format(hhpString, _fileName, _title);
            CreateFile(fn, content);

            fn = baseDir + _fileName + ".hhc";
            content = string.Format(hhcString, _title, GetItems(itemString));
            CreateFile(fn, content);

            fn = baseDir + _fileName + ".hhk";
            content = string.Format(hhcString, _title, GetItems(hhkString));
            CreateFile(fn, content);

            fn = baseDir + "html\\all.htm";
            content = htmString.Replace("{0}", GetItems(item2String));
            CreateFile(fn, content);

            int cnt = items.Count;
            for (int i = cnt - 1; i >= 0; i--)
            {
                var entity = items[i];
                if (!entity.IsDown)
                {
                    continue;
                }
                _callback("获取文章 " + (cnt - i) + "/" + cnt + "：" + entity.Title);

                if (feedService.GetContent(ref entity))
                {
                    string name = entity.Url.Substring(entity.Url.LastIndexOf("/") + 1);
                    fn = baseDir + "html\\" + name + ".htm";

                    content = htmString.Replace("{0}", entity.Content);

                    CreateFile(fn, content);
                }
            } 
        }
        /// <summary>
        /// 根据url列表获取文章内容
        /// </summary>
        private void Build2()
        {
            int cnt = artUrls.Length;

            _callback("共有【" + cnt + "】篇文章等待导出");
            int i = 0;
            StringBuilder sb = new StringBuilder();
            string fn, content;
            foreach(string url in artUrls)
            {
                i++;
                var entity = feedService.GetEntity(url);                

                string name = entity.Url.Substring(entity.Url.LastIndexOf("/") + 1);
                fn = baseDir + "html\\" + name + ".htm";
                content = htmString.Replace("{0}", entity.Content);
                CreateFile(fn, content);

                _callback("已获取文章 " + i + "/" + cnt + "：" + entity.Title);
            }
            items = feedService.GetList();

            fn = baseDir + _fileName + ".hhp";
            content = string.Format(hhpString, _fileName, _title);
            CreateFile(fn, content);

            fn = baseDir + _fileName + ".hhc";
            content = string.Format(hhcString, _title, GetItems2(itemString));
            CreateFile(fn, content);

            fn = baseDir + _fileName + ".hhk";
            content = string.Format(hhcString, _title, GetItems2(hhkString));
            CreateFile(fn, content);

            fn = baseDir + "html\\all.htm";
            content = htmString.Replace("{0}", GetItems2(item2String));
            CreateFile(fn, content);

        }

        private bool Compile()
        {
            string _chmFile = baseDir.TrimEnd('\\') + ".chm";
            Process helpCompileProcess = new Process();
            try
            {

                ProcessStartInfo processStartInfo = new ProcessStartInfo();
                processStartInfo.WindowStyle = ProcessWindowStyle.Hidden;
                processStartInfo.FileName = hhcexe;
                processStartInfo.Arguments = "\"" + baseDir + _fileName + ".hhp\"";
                processStartInfo.UseShellExecute = false;
                helpCompileProcess.StartInfo = processStartInfo;
                helpCompileProcess.Start();
                helpCompileProcess.WaitForExit();

                if (helpCompileProcess.ExitCode == 0)
                {
                    return false;
                }
            }
            finally
            {
                helpCompileProcess.Close();
            }
            return true;
        }

        #region helper
        private string GetItems(string template)
        {
            StringBuilder sb = new StringBuilder();
            for (int i = items.Count - 1; i >= 0; i--)
            {
                var entity = items[i];
                if (entity.IsDown)
                {
                    string name = entity.Url.Substring(entity.Url.LastIndexOf("/") + 1);

                    sb.AppendFormat(template, entity.Title, name);
                }
            }
            return sb.ToString();
        }
        private string GetItems2(string template)
        {
            StringBuilder sb = new StringBuilder();
            foreach (var entity in items)
            {
                if (entity.IsDown)
                {
                    string name = entity.Url.Substring(entity.Url.LastIndexOf("/") + 1);

                    sb.AppendFormat(template, entity.Title, name);
                }
            }
            return sb.ToString();
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

        private bool CheckChmInstalled()
        {
            if (!File.Exists(hhcexe))
            {
                hhcexe = hhcexe.Replace(" (x86)", "");
            }

            return File.Exists(hhcexe);
        }
        #endregion

        #region 文件内容
        string hhcexe = @"C:\Program Files (x86)\HTML Help Workshop\hhc.exe";

        string hhpString = @"[OPTIONS]
Compatibility=1.1 or later
Compiled file=../{0}.chm
Contents file={0}.hhc
Default Font=宋体,10,0
Default topic=html\all.htm
Display compile progress=Yes
Index file={0}.hhk
Language=0x804 中文(中国)
Title={1}

[FILES]
html\all.htm

[INFOTYPES]";
        string hhcString = @"<!DOCTYPE HTML PUBLIC ""-//IETF//DTD HTML//EN"">
<HTML>
<HEAD>
<meta name=""GENERATOR"" content=""Microsoft&reg; HTML Help Workshop 4.1"">
<!-- Sitemap 1.0 -->
</HEAD>
<BODY>
<OBJECT type=""text/site properties"">
<param name=""Window Styles"" value=""0x800025"">
</OBJECT>
<UL>
<LI><OBJECT type=""text/sitemap"">
<param name=""Name"" value=""{0}"">
<param name=""Local"" value=""html\all.htm"">
</OBJECT>
{1}
</UL>
</BODY>
</HTML>";

        string hhkString = @"<!DOCTYPE HTML PUBLIC ""-//IETF//DTD HTML//EN"">
<HTML>
<HEAD>
<meta name=""GENERATOR"" content=""Microsoft&reg; HTML Help Workshop 4.1"">
<!-- Sitemap 1.0 -->
</HEAD>
<BODY>
<UL>
{0}
</UL>
</BODY>
</HTML>";
        //<param name=""ImageNumber"" value=""21"">
        string itemString = @"<LI><OBJECT type=""text/sitemap"">
<param name=""Name"" value=""{0}"">
<param name=""Local"" value=""html\{1}.htm"">
</OBJECT>";
        string item2String = @"<LI><a href=""html/{1}.htm"">{0}</a>
</LI>";

        string htmString = @"<!DOCTYPE HTML PUBLIC ""-//IETF//DTD HTML//EN"">
<HTML>
<HEAD>
<style type=""text/css"">
body {font-size:14px; font-family:宋体; line-height:180%;}
textarea,pre {font-family:Courier; font-size:12px;}
</style>
</HEAD>
<BODY>
{0}
</BODY>
</HTML>";
        #endregion

    }
}
