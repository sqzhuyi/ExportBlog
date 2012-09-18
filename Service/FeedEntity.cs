using System;
using System.ComponentModel;

namespace ExportBlog
{
    /// <summary>
    /// 文章实体
    /// </summary>
    public class FeedEntity
    {
        public FeedEntity()
        {
            Content = string.Empty;
            IsDown = true;
        }
        public string Url { get; set; }

        public string Title { get; set; }

        public string Content { get; set; }

        public bool IsDown { get; set; }

        public void Dispose()
        {
            Url = null;
            Title = null;
            Content = null;
        }
    }
    public enum Source
    {
        [Description("博客园")]
        cnblogs = 1,
        [Description("ITEYE")]
        iteye,
        [Description("新浪")]
        sina,
        [Description("搜狐")]
        sohu,
        [Description("和讯")]
        hexun,
        [Description("ChinaUnix")]
        chinaunix,
        [Description("网易")]
        _163,
        [Description("51CTO")]
        _51cto,
        [Description("CSDN")]
        csdn,
        [Description("开源中国")]
        oschina,
    }
}
