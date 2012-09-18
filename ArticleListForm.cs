using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.Threading;
using System.Runtime.InteropServices;

namespace ExportBlog
{
    public partial class ArticleListForm : Form
    {
        FeedService feedService = null;

        public ArticleListForm(FeedService feedService)
        {
            this.feedService = feedService;
            InitializeComponent();
        }

        private void ArticleListForm_Load(object sender, EventArgs e)
        {
            checkBox1.Checked = true;

            new Thread(() =>
            {
                Thread.Sleep(50);
                SetChkItem();
            })
            {
                IsBackground = true
            }.Start();
        }

        private void checkBox1_CheckedChanged(object sender, EventArgs e)
        {
            for (int i = 0; i < checkedListBox1.Items.Count; i++)
            {
                checkedListBox1.SetItemChecked(i, checkBox1.Checked);
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            var list = feedService.GetList();
            int total = 0;
            int cnt = checkedListBox1.Items.Count;
            for (int i = 0; i < cnt; i++)
            {
                if (!checkedListBox1.GetItemChecked(i))
                {
                    list[cnt - i - 1].IsDown = false;
                    total++;
                }
            }
            if (total == checkedListBox1.Items.Count)
            {
                MessageBox.Show("请选择要导出的文章。");
                return;
            }
            this.DialogResult = DialogResult.OK;
            this.Dispose();
        }

        private void button2_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.Cancel;
            this.Dispose();
        }

        private delegate void SetItem();
        private void SetChkItem()
        {
            SetItem st = new SetItem(delegate()
            {
                var list = feedService.GetList();
                label1.Invoke(new SetItem(delegate()
                {
                    label1.Visible = false;
                }));
                for (int i = list.Count - 1; i >= 0; i--)
                {
                    checkedListBox1.Items.Add(list[i].Title, true);
                }
            });

            checkedListBox1.Invoke(st);
        }

        #region move

        [DllImport("user32.dll")]
        public static extern bool ReleaseCapture();
        [DllImport("user32.dll")]
        public static extern bool SendMessage(IntPtr hwnd, int wMsg, int wParam, int lParam);
        public const int WM_SYSCOMMAND = 0x0112;
        public const int SC_MOVE = 0xF010;
        public const int HTCAPTION = 0x0002;

        private void ArticleListForm_MouseMove(object sender, MouseEventArgs e)
        {
            ReleaseCapture();
            SendMessage(this.Handle, WM_SYSCOMMAND, SC_MOVE + HTCAPTION, 0);
        }
        #endregion
    }
}
