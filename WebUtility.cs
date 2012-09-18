using System;
using System.Text;
using System.Net;
using System.IO;
using System.Drawing;
using System.Collections.Generic;


namespace ExportBlog
{
    public class WebUtility
    {
        #region private members
        HttpWebRequest request = null;
        HttpWebResponse response = null;

        Uri uri;
        Encoding encoding = Encoding.UTF8;
        int timeOut = 3000;
        CookieContainer cc = new CookieContainer();
        string referer;
        string contentType = "application/x-www-form-urlencoded";
        byte[] data;

        Stream stream = null;

        bool hasError = false;

        bool did = false;
        #endregion

        public delegate void WebCallback(string result);
        /// <summary>
        /// 回调函数（声明了回调函数则使用异步请求）
        /// </summary>
        public WebCallback Callback = null;

        public WebUtility()
        {
            uri = null;
        }
        public WebUtility(string url)
        {
            uri = new Uri(url);
        }

        #region 属性
        /// <summary>
        /// 设置要请求的URL
        /// </summary>
        public string URL
        {
            set { uri = new Uri(value); }
        }
        /// <summary>
        /// 指定编码方式(默认:GB2312)
        /// </summary>
        public Encoding Encode
        {
            set { encoding = value; }
        }
        /// <summary>
        /// 设置连接超时时间(ms)
        /// </summary>
        public int TimeOut
        {
            set { timeOut = value; }
        }
        /// <summary>
        /// 设置/获取CookieContainer
        /// </summary>
        public CookieContainer Cookies
        {
            get { return cc; }
            set { cc = value; }
        }
        /// <summary>
        /// 设置请求链接源
        /// </summary>
        public string Referer
        {
            set { referer = value; }
        }
        /// <summary>
        /// 设置请求的内容类型
        /// </summary>
        public string ContentType
        {
            set { contentType = value; }
        }
        /// <summary>
        /// 获取请求返回的cookies
        /// </summary>
        public CookieCollection GetCookies
        {
            get { return did ? cc.GetCookies(uri) : null; }
        }
        /// <summary>
        /// 获取请求返回的HTTP标头
        /// </summary>
        public WebHeaderCollection Headers
        {
            get { return did ? response.Headers : null; }
        }
        /// <summary>
        /// 是否发生错误
        /// </summary>
        public bool HasError
        {
            get { return hasError; }
        }

        #endregion

        #region 提供方法
        /// <summary>
        /// GET方式发送请求
        /// </summary>
        /// <returns></returns>
        public string Get()
        {
            string msg = Send();

            if (hasError) return msg;

            StreamReader reader = null;
            try
            {
                reader = new StreamReader(stream, encoding);
                msg = reader.ReadToEnd();
            }
            catch (Exception ex)
            {
                hasError = true;
                msg = ex.Message;
            }
            finally
            {
                if (reader != null) reader.Close();
                if (stream != null) stream.Close();
                response.Close();
            }
            return msg;
        }
        /// <summary>
        /// 获取图片(GET)
        /// </summary>
        /// <returns></returns>
        public Image GetImage()
        {
            string msg = Send();

            if (hasError) return null;

            Bitmap bitMap = null;
            try
            {
                Image original = Image.FromStream(stream);
                bitMap = new Bitmap(original);
            }
            catch (Exception ex)
            {
                hasError = true;
                msg = ex.Message;
            }
            finally
            {
                if (stream != null) stream.Close();
                response.Close();
            }
            return bitMap;
        }
        /// <summary>
        /// POST方式发送请求
        /// </summary>
        /// <param name="data">待发送数据</param>
        /// <param name="encoded">是否已进行编码</param>
        /// <returns></returns>
        public string Post(string data, bool encoded)
        {
            if (encoded) this.data = encoding.GetBytes(data);
            else this.data = encoding.GetBytes(EncodeData(data));

            return Post(this.data);
        }
        public string Post(byte[] data)
        {
            this.data = data;

            string msg = Send("POST");

            if (hasError) return msg;

            StreamReader reader = null;
            try
            {
                reader = new StreamReader(stream, encoding);
                msg = reader.ReadToEnd();
            }
            catch (Exception ex)
            {
                hasError = true;
                msg = ex.Message;
            }
            finally
            {
                if (reader != null) reader.Close();
                if (stream != null) stream.Close();
                response.Close();
            }
            return msg;
        }
        /// <summary>
        /// 下载文件
        /// </summary>
        /// <param name="fileName">文件全路径</param>
        /// <returns>返回错误消息</returns>
        public string DownloadFile(string fileName)
        {
            string msg = Send();

            if (hasError) return msg;

            int len = Headers["Content-Length"] != null ? int.Parse(Headers["Content-Length"].ToString()) : 1024;
            List<byte> data = new List<byte>(len);
            try
            {
                int i = -1;
                while ((i = stream.ReadByte()) > -1)
                {
                    data.Add((byte)i);
                }
            }
            catch (Exception ex)
            {
                hasError = true;
                return ex.Message;
            }
            finally
            {
                stream.Close();
                response.Close();
            }
            if (data.Count == 0)
            {
                return "文件长度为0";
            }
            FileStream fs = new FileStream(fileName, FileMode.OpenOrCreate);
            BinaryWriter bw = new BinaryWriter(fs);
            try
            {
                bw.Write(data.ToArray());
                return string.Empty;
            }
            catch (Exception ex)
            {
                hasError = true;
                return ex.Message;
            }
            finally
            {
                bw.Close();
                fs.Close();
            }
        }
        #endregion

        #region 私有方法
        private string Send()
        {
            return Send("GET");
        }
        /// <summary>
        /// 发送请求
        /// </summary>
        /// <param name="method">请求方式(GET/POST)</param>
        private string Send(string method)
        {
            if (uri == null)
            {
                hasError = true;
                return "URI is null";
            }
            try
            {
                if (uri.ToString().ToLower().StartsWith("https"))
                {
                    ServicePointManager.ServerCertificateValidationCallback = new System.Net.Security.RemoteCertificateValidationCallback(CheckValidationResult);
                }
                System.Net.ServicePointManager.Expect100Continue = false;

                request = (HttpWebRequest)WebRequest.Create(uri);

                if (referer != null)
                {
                    request.Referer = referer;
                }
                request.CookieContainer = cc;
                request.KeepAlive = true;
                request.UserAgent = "Mozilla/4.0 (compatible; MSIE 7.0; Windows NT 5.1; .NET CLR 2.0.50727; .NET CLR 3.0.04506.648; .NET CLR 3.5.21022)";
                request.Timeout = timeOut;
                
                if (method.ToUpper() == "POST")
                {
                    request.Method = "POST";
                    request.ContentType = contentType;
                    request.ContentLength = data.Length;

                    Stream newStream = request.GetRequestStream();
                    newStream.Write(data, 0, data.Length);
                    newStream.Close();
                }
                if (Callback == null)
                {
                    response = (HttpWebResponse)request.GetResponse();
                    stream = response.GetResponseStream();

                    hasError = false;
                    did = true;
                    return "true";
                }
                else
                {//使用异步方式
                    request.BeginGetResponse(EndRequest, null);

                    hasError = true;
                    return "loading";
                }
            }
            catch (Exception ex)
            {
                if (stream != null) stream.Close();
                if (response != null) response.Close();
                hasError = true;
                return ex.Message;
            }
        }

        private void EndRequest(IAsyncResult result)
        {
            request = (HttpWebRequest)result.AsyncState;
            response = (HttpWebResponse)request.EndGetResponse(result);
            stream = response.GetResponseStream();

            using (StreamReader reader = new StreamReader(stream, this.encoding))
            {
                StringBuilder sb = new StringBuilder();

                int c = reader.Read();
                while (c > -1)
                {
                    sb.Append((char)c);
                    c = reader.Read();
                }
                reader.Close();
                if (Callback != null)
                {
                    Callback(sb.ToString().Trim());
                }
            }
            did = true;
        }
        private string EncodeData(string data)
        {
            StringBuilder sb = new StringBuilder();
            Char[] reserved = { '=', '&' };

            int i = 0, j;
            while (i < data.Length)
            {
                j = data.IndexOfAny(reserved, i);
                if (j == -1)
                {
                    sb.Append(System.Web.HttpUtility.UrlEncode(data.Substring(i, data.Length - i), encoding));
                    break;
                }
                sb.Append(System.Web.HttpUtility.UrlEncode(data.Substring(i, j - i), encoding));
                sb.Append(data.Substring(j, 1));
                i = j + 1;
            }
            return sb.ToString();
        }
        //https
        private bool CheckValidationResult(object sender,
            System.Security.Cryptography.X509Certificates.X509Certificate certificate,
            System.Security.Cryptography.X509Certificates.X509Chain chain,
            System.Net.Security.SslPolicyErrors errors)
        { // Always accept
            return true;
        }
        #endregion
    }
}
