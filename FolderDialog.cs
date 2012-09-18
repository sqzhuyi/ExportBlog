using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Forms;
using System.Windows.Forms.Design;

namespace ExportBlog
{
    public class FolderDialog : FolderNameEditor
    {
        FolderBrowser fDialog = new FolderBrowser();

        public FolderDialog()
        {
        }

        public DialogResult DisplayDialog()
        {
            return DisplayDialog("请选择一个文件夹");
        }
        public DialogResult DisplayDialog(string description)
        {
            fDialog.Description = description;
            return fDialog.ShowDialog();
        }
        public string Path
        {
            get { return fDialog.DirectoryPath; }
        }
        ~FolderDialog()
        {
            fDialog.Dispose();
        }
    }
}
