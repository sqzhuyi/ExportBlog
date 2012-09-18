using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;
using System.Text.RegularExpressions;

namespace ExportBlog
{
    public class App
    {
        public static string BaseDirectory = AppDomain.CurrentDomain.BaseDirectory;

        public static int ToInt(string s)
        {
            int i = 0;
            int.TryParse(s, out i);
            return i;
        }

        public static string ToHtmlDecoded(string s)
        {
            return System.Web.HttpUtility.HtmlDecode(s);
        }

        public static string GetDescription(Enum cur)
        {
            var fi = cur.GetType().GetField(cur.ToString());

            var da = (DescriptionAttribute)Attribute.GetCustomAttribute(fi, typeof(DescriptionAttribute));

            return da != null ? da.Description : cur.ToString();
        }

        static Regex reg_unicode = new Regex(@"\\u[a-f0-9]{4}", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        public static string UtoGB(string str)
        {
            Match mat = reg_unicode.Match(str);
            while (mat.Success)
            {
                char c = Convert.ToChar(Convert.ToInt32(mat.Value.Substring(2), 16));
                str = str.Replace(mat.Value, c.ToString());
                mat = reg_unicode.Match(str);
            }
            return str;
        }

    }

    public enum Type
    {
        Blog = 1,
        Url = 2,
        Column = 4,
    }

    [Flags]
    public enum Format
    {
        CHM = 1,
        PDF = 2,
        HTML = 4,
        TXT = 8,
        EPUB = 16,
    }
}
