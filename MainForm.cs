using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.IO;
using System.Diagnostics;
using System.Threading;
using System.Text.RegularExpressions;
using System.Runtime.InteropServices;

namespace ExportBlog
{
    public partial class MainForm : Form
    {
        public MainForm()
        {
            InitializeComponent();
        }

        private void MainForm_Load(object sender, EventArgs e)
        {
            textBox2.ReadOnly = true;
            tbUser.Select();

            foreach (Source src in Enum.GetValues(typeof(Source)))
            {
                comboBox1.Items.Add(App.GetDescription(src));
            }
            comboBox1.SelectedIndex = (int)Source.csdn - 1;
            label9.Text = string.Empty;
        }

        private void StartExport()
        {
            var put = GetInput();
            if (!put.Status) return;

            Source src = Source.csdn;
            switch (put.Type)
            {
                case Type.Blog:
                    src = (Source)(comboBox1.SelectedIndex + 1);
                    break;
                case Type.Url:
                    src = GetSourceByDomain();
                    break;
                case Type.Column:
                    src = Source.csdn;
                    break;
            }
            string user = put.Text;
            if (put.Type == Type.Url) user = src.ToString();
            string title = App.GetDescription(src);

            FeedService fs = new FeedService(src, user);

            if (put.Type == Type.Blog)
            {
                ArticleListForm frm = new ArticleListForm(fs);
                if (frm.ShowDialog() != DialogResult.OK)
                {
                    return;
                }
            }
            button1.Enabled = false;

            Thread thd = new Thread(() =>
            {
                string[] urls = null;
                if (put.Type == Type.Url)
                {
                    urls = put.Text.Replace("\r", "").Split(new char[] { '\n', ',', '，', ';', '；' }, StringSplitOptions.RemoveEmptyEntries);
                }
                else if (put.Type == Type.Column)
                {//抓取URL 
                    urls = GetColumnUrls(put.Text);
                }
                if ((put.Format & Format.CHM) != 0)
                {
                    SetLbText("开始生成CHM文档");
                    ChmPackage chm = new ChmPackage(user, title, fs, urls, SetLbText);
                    if (chm.Build())
                        SetLbText("CHM文档生成成功！");
                }
                if ((put.Format & Format.PDF) != 0)
                {
                    SetLbText("开始生成PDF文档");
                    PdfPackage pdf = new PdfPackage(user, title, fs, urls, SetLbText);
                    pdf.Build();
                    SetLbText("PDF文档生成成功！");
                }
                if ((put.Format & Format.HTML) != 0)
                {
                    SetLbText("开始生成HTML文档");
                    HtmlPackage htm = new HtmlPackage(user, title, fs, urls, SetLbText);
                    htm.Build();
                    SetLbText("HTML文档生成成功！");
                }
                if ((put.Format & Format.TXT) != 0)
                {
                    SetLbText("开始生成TXT文档");
                    TxtPackage txt = new TxtPackage(user, title, fs, urls, SetLbText);
                    txt.Build();
                    SetLbText("TXT文档生成成功！");
                }
                if ((put.Format & Format.EPUB) != 0)
                {
                    SetLbText("开始生成EPUB文档");
                    EpubPackage epub = new EpubPackage(user, title, fs, urls, SetLbText);
                    epub.Build();
                    SetLbText("EPUB文档生成成功！");
                }
                MessageBox.Show("全部导出完成！");

                button1.Invoke(new SetText(delegate(string s)
                {
                    button1.Enabled = true;
                }), string.Empty);
            });
            thd.Start(); 
        }

        #region helper

        private Input GetInput()
        {
            var put = new Input();
            switch (tabControl1.SelectedIndex)
            {
                case 0:
                    put.Type = Type.Blog;
                    put.Text = tbUser.Text.Trim();
                    break;
                case 1:
                    put.Type = Type.Url;
                    put.Text = tbUrl.Text.Trim();
                    break;
                case 2:
                    put.Type = Type.Column;
                    put.Text = tbColumn.Text.Trim();
                    break;
            }

            if (put.Text == string.Empty)
            {
                string msg = string.Empty;
                if (put.Type == Type.Blog) msg = "请输入博客用户名";
                else if (put.Type == Type.Url) msg = "请输入要导出的博客URL";
                else if (put.Type == Type.Column) msg = "请输入专栏别名";
                MessageBox.Show(msg);
                return put;
            }
            if (chk1_chm.Checked) put.Format |= Format.CHM;
            if (chk1_pdf.Checked) put.Format |= Format.PDF;
            if (chk1_htm.Checked) put.Format |= Format.HTML;
            if (chk1_txt.Checked) put.Format |= Format.TXT;
            if (chk1_epub.Checked) put.Format |= Format.EPUB;

            if ((int)put.Format == 0)
            {
                MessageBox.Show("请选择要导出的格式");
                return put;
            }

            FolderDialog fDialog = new FolderDialog();
            var re = fDialog.DisplayDialog();
            if (re != DialogResult.OK)
            {
                return put;
            }
            App.BaseDirectory = fDialog.Path + "\\";

            put.Status = true;

            return put;
        }

        private delegate void SetText(string text);
        private void SetLbText(string str)
        {
            str = "[" + DateTime.Now.ToString("HH:mm:ss") + "] " + str + "\n";

            if (textBox2.InvokeRequired)
            {
                SetText st = new SetText(delegate(string text)
                {
                    textBox2.AppendText(text);
                });
                textBox2.Invoke(st, str);
            }
            else
            {
                textBox2.AppendText(str);
            }
        }
        Regex reg_title = new Regex(@"<a name=""\d+"" href=""(.+?)"" target=""_blank"">.+?</a>", RegexOptions.Compiled);
        private string[] GetColumnUrls(string alias)
        {
            IList<string> urls = new List<string>();

            string url = "http://blog.csdn.net/column/details/" + alias + ".html?page={0}";
            int p = 0;
            WebUtility web = new WebUtility();
            web.Encode = Encoding.UTF8;
            for (int i = 1; i < 100; i++)
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
                    urls.Add(mat.Groups[1].Value);
                } 
            }
            string[] ss = new string[urls.Count];
            for (int i = 0; i < urls.Count; i++)
            {
                ss[i] = urls[i];
            }
            return ss;
        }
        private Source GetSourceByDomain()
        {
            Source src = Source.csdn;
            string url = tbUrl.Text.Split('\n')[0];
            if (url.Contains("163.com")) src = Source._163;
            else if (url.Contains("51cto.com")) src = Source._51cto;
            else if (url.Contains("chinaunix.net")) src = Source.chinaunix;
            else if (url.Contains("cnblogs.com")) src = Source.cnblogs;
            else if (url.Contains("csdn.net")) src = Source.csdn;
            else if (url.Contains("hexun.com")) src = Source.hexun;
            else if (url.Contains("iteye.com")) src = Source.iteye;
            else if (url.Contains("oschina.net")) src = Source.oschina;
            else if (url.Contains("sina.com.cn")) src = Source.sina;
            else if (url.Contains("sohu.com")) src = Source.sohu;

            return src;
        }
        #endregion

        #region close button
        private void linkLabel1_MouseHover(object sender, EventArgs e)
        {
            linkLabel1.ImageIndex = 3;
        }

        private void linkLabel1_MouseLeave(object sender, EventArgs e)
        {
            linkLabel1.ImageIndex = 2;
        }

        private void linkLabel1_Click(object sender, EventArgs e)
        {
            this.WindowState = FormWindowState.Minimized;
        }

        private void linkLabel2_MouseHover(object sender, EventArgs e)
        {
            linkLabel2.ImageIndex = 1;
        }

        private void linkLabel2_MouseLeave(object sender, EventArgs e)
        {
            linkLabel2.ImageIndex = 0;
        }
        
        private void linkLabel2_Click(object sender, EventArgs e)
        {
            this.Close();
            Application.Exit();
        }
        #endregion

        #region move

        [DllImport("user32.dll")]
        public static extern bool ReleaseCapture();
        [DllImport("user32.dll")]
        public static extern bool SendMessage(IntPtr hwnd, int wMsg, int wParam, int lParam);
        public const int WM_SYSCOMMAND = 0x0112;
        public const int SC_MOVE = 0xF010;
        public const int HTCAPTION = 0x0002;

        private void MainForm_MouseDown(object sender, MouseEventArgs e)
        {
            ReleaseCapture();
            SendMessage(this.Handle, WM_SYSCOMMAND, SC_MOVE + HTCAPTION, 0);
        }
        #endregion

        #region events
        private void button1_Click(object sender, EventArgs e)
        {
            StartExport();
        }
        private void button2_Click(object sender, EventArgs e)
        {
            StartExport();
        }
        private void button3_Click(object sender, EventArgs e)
        {
            StartExport();
        }
        private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            string s = null;
            Source src = (Source)(comboBox1.SelectedIndex + 1);
            switch (src)
            {
                case Source._163:
                    s = "http://blog.163.com/,";
                    break;
                case Source._51cto:
                    s = "http://,.blog.51cto.com/";
                    break;
                case Source.chinaunix:
                    s = "http://blog.chinaunix.net/uid/,.html";
                    break;
                case Source.cnblogs:
                    s = "http://www.cnblogs.com/,";
                    break;
                case Source.csdn:
                    s = "http://blog.csdn.net/,";
                    break;
                case Source.hexun:
                    s = "http://,.blog.hexun.com/";
                    break;
                case Source.iteye:
                    s = "http://,.iteye.com/";
                    break;
                case Source.oschina:
                    s = "http://my.oschina.net/,/blog";
                    break;
                case Source.sina:
                    s = "http://blog.sina.com.cn/,";
                    break;
                case Source.sohu:
                    s = "http://,.blog.sohu.com/";
                    break;
            }
            label1.Text = s.Split(',')[0];
            label9.Text = s.Split(',')[1];

            tbUser.Location = new Point(label1.Location.X + label1.Width, tbUser.Location.Y);
            label9.Location = new Point(tbUser.Location.X + tbUser.Width, label9.Location.Y);
        }
        
        private void tbUrl_Click(object sender, EventArgs e)
        {
            if (tbUrl.Text == "回车分割多个URL")
            {
                tbUrl.Text = string.Empty;
            }
        }
        #endregion
    }

    public class Input
    {
        public Input()
        {
            Status = false;
        }
        public Type Type { get; set; }
        public string Text { get; set; }
        public Format Format { get; set; }
        public bool Status { get; set; }
    }
}
