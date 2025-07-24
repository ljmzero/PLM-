using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Globalization;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Threading;

namespace PLM信息导出
{
    public class HttpHelper
    {
        /// <summary>
        /// Http同步Get同步请求
        /// </summary>
        /// <param name="url">Url地址，如果要传参数的话，需要在url加?key1=valule1&key2=value2..</param>
        /// <param name="encode">编码(默认UTF8)</param>
        /// <returns></returns>
        public static string HttpGet(string url, int timeout = 0, Encoding encode = null)
        {
            //WebClient支持下载进度(client.DownloadProgressChanged)等功能
            using (var webClient = new WebClientX { Encoding = encode ?? Encoding.UTF8 })//encode不能为参数默认值，要求是编译时常量
            {
                if (timeout > 0) webClient.Timeout = timeout;
                var result = webClient.DownloadString(url);
                return result;
            }
        }

        /// <summary>
        /// Http同步Get异步请求
        /// </summary>
        /// <param name="url">Url地址，如果要传参数的话，需要在url加?key1=valule1&key2=value2..</param>
        /// <param name="callCompleted">回调事件</param>
        /// <param name="encode">编码(默认UTF8)</param>
        public static void HttpGetAsync(string url, DownloadStringCompletedEventHandler callCompleted = null, int timeout = 0, Encoding encode = null)
        {
            using (var webClient = new WebClientX { Encoding = encode ?? Encoding.UTF8 })
            {
                if (timeout > 0) webClient.Timeout = timeout;
                if (callCompleted != null) webClient.DownloadStringCompleted += callCompleted;
                /* e.g.
                 webClient.DownloadStringCompleted += (senderobj, es) =>
                 {
                     var obj = es.Result;
                 };
                 */
                webClient.DownloadStringAsync(new Uri(url));
            }
        }

        /// <summary>
        ///  Http同步Post同步请求
        /// </summary>
        /// <param name="url">Url地址</param>
        /// <param name="postStr">请求Url数据</param>
        /// <param name="contentType">Content-Type默认是"application/json"。其它类型e.g. "application/x-www-form-urlencoded"</param>
        /// <param name="encode">编码(默认UTF8)</param>
        /// <returns></returns>
        public static string HttpPost(string url, string postStr = "", NameValueCollection headers = null, string contentType = "application/json", int timeout = 0, Encoding encode = null)
        {
            var result = string.Empty;
            using (var webClient = new WebClientX { Encoding = encode ?? Encoding.UTF8 })
            {
                try
                {
                    if (headers != null) webClient.Headers.Add(headers);
                    if (timeout > 0) webClient.Timeout = timeout;
                    var sendData = webClient.Encoding.GetBytes(postStr);
                    //webClient.Headers["Accept"] = "application/json";
                    webClient.Headers.Add("Content-Type", contentType);
                    webClient.Headers.Add("ContentLength", sendData.Length.ToString(CultureInfo.InvariantCulture));

                    var readData = webClient.UploadData(url, "POST", sendData);
                    result = webClient.Encoding.GetString(readData);
                }
                catch (Exception ex)
                {
                    result = ex.Message;
                }
                return result;
            }
        }

        /// <summary>
        /// Http同步Post异步请求
        /// </summary>
        /// <param name="url">Url地址</param>
        /// <param name="postStr">请求Url数据</param>
        /// <param name="callBackUploadDataCompleted">回调事件</param>
        /// <param name="contentType">Content-Type默认是"application/json"。其它类型e.g. "application/x-www-form-urlencoded"</param>
        /// <param name="encode"></param>
        public static void HttpPostAsync(string url, string postStr = "", Action<string> callCompleted = null, string contentType = "application/json", int timeout = 0, Encoding encode = null)
        {
            using (var webClient = new WebClientX { Encoding = encode ?? Encoding.UTF8 })
            {
                if (timeout > 0) webClient.Timeout = timeout;
                var sendData = webClient.Encoding.GetBytes(postStr);
                webClient.Headers.Add("Content-Type", contentType);
                webClient.Headers.Add("ContentLength", sendData.Length.ToString(CultureInfo.InvariantCulture));
                if (callCompleted != null)
                {
                    //webClient.UploadDataCompleted += callCompleted;
                    webClient.UploadDataCompleted += (sender, es) =>
                    {
                        var resultStr = webClient.Encoding.GetString(es.Result);
                        //var result = JsonConvert.DeserializeObject<T>(resultStr);
                        //callCompleted?.Invoke(result);
                        callCompleted(resultStr);
                    };
                }
                webClient.UploadDataAsync(new Uri(url), "POST", sendData);
            }
            /*postStr:
             StringBuilder postData = new StringBuilder();
    postData.AppendFormat("{0}={1}&", "username", "admin");
    postData.AppendFormat("{0}={1}&", "password", "123456");
    postData.AppendFormat("{0}={1}", "nickname", UrlEncode("辉耀"));
             */
        }

        /// <summary>
        /// 使用HttpWebRequest的同步post请求
        /// </summary>
        /// <param name="contentType">Content-Type默认是"application/json"。其它类型e.g. "application/x-www-form-urlencoded"</param>
        public static string SendPost(string url, string postStr = "", string contentType = "application/json", int timeout = 0, Encoding encode = null)
        {
            //1.数据准备
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
            request.Method = "POST";
            request.ContentType = contentType;
            if (timeout > 0) request.Timeout = timeout;
            if (encode == null) encode = Encoding.UTF8;
            byte[] btBodys = encode.GetBytes(postStr);
            request.ContentLength = btBodys.Length;
            Stream writeStream = request.GetRequestStream();
            writeStream.Write(btBodys, 0, btBodys.Length);
            writeStream.Close();

            //2.请求服务端，接收HTTP做出的响应。
            HttpWebResponse response = (HttpWebResponse)request.GetResponse();
            StreamReader readStream = new StreamReader(response.GetResponseStream());
            string responseContent = readStream.ReadToEnd();
            readStream.Close();
            response.Close();
            return responseContent;
        }

        public static async Task<string> SendPostAsync(string url, string postStr = "", string contentType = "application/json", int timeout = 0, Encoding encode = null)
        {
            // 数据准备
            HttpClient client = new HttpClient();
            client.Timeout = timeout > 0 ? TimeSpan.FromSeconds(timeout) : Timeout.InfiniteTimeSpan;

            if (encode == null)
            {
                encode = Encoding.UTF8;
            }

            HttpContent content = new StringContent(postStr, encode, contentType);

            // 发送POST请求并获取响应
            HttpResponseMessage response = await client.PostAsync(url, content);

            // 读取响应内容
            string responseContent = await response.Content.ReadAsStringAsync();

            return responseContent;
        }
    }

    /// <summary>
    /// WebClient，支持Timeout，ReadWriteTimeout超时时间设定。
    /// </summary>
    public class WebClientX : WebClient
    {
        /// <summary>
        /// 原始 URI 字符串。异步调用时有用。
        /// </summary>
        public string UriOriginalString { get; private set; }

        /*
         * TimeOut只要有第一次响应了，则就计时结束，而ReadWriteout是所有响应结束时才计时结束，
         * 比如下载文件时，TimeOut只要有开始下载文件了，就计时结束，而ReadWriteout是整个文件下载完成才计时结束。
         */

        /// <summary>
        /// 获取或设置 System.Net.HttpWebRequest.GetResponse 和 System.Net.HttpWebRequest.GetRequestStream方法的超时值（以毫秒为单位）。默认值是 100,000 毫秒（100 秒）。
        /// </summary>
        public int Timeout { get; set; }

        /// <summary>
        /// 获取或设置写入或读取流时的超时（以毫秒为单位）。默认值为 300,000 毫秒（5 分钟）
        /// </summary>
        public int ReadWriteTimeout { get; set; }

        public WebClientX()
        {
            //默认值和原HttpWebRequest一样。
            Timeout = 100 * 1000;
            ReadWriteTimeout = 300 * 1000;
        }

        /// <summary>
        /// 重写GetWebRequest,添加WebRequest对象超时时间
        /// </summary>
        /// <param name="address"></param>
        /// <returns></returns>
        protected override WebRequest GetWebRequest(Uri address)
        {
            //WebClinet内部其实是用HttpWebRequest 实现，WebClient有连接池的处理，不会把端口用完
            WebRequest wr = base.GetWebRequest(address);
            UriOriginalString = address.OriginalString;
            if (wr is HttpWebRequest)
            {
                HttpWebRequest request = (HttpWebRequest)wr;//HttpWebRequest 默认超时时间是100秒 100*1000
                request.Timeout = Timeout;
                request.ReadWriteTimeout = ReadWriteTimeout;
                return request;
            }
            else
            {
                FileWebRequest request = (FileWebRequest)base.GetWebRequest(address);
                request.Timeout = Timeout;
                //request.ReadWriteTimeout = Timeout;
                return request;
            }
        }
    }
}