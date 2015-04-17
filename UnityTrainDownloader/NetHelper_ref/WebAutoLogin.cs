using System;
using System.IO;
using System.Net;
using System.Text;
using System.Web;
using System.Windows;
using System.Text.RegularExpressions;
using System.Windows.Controls;
using System.Threading;

namespace UnityTrainDownloader
{
    public class WebAutoLogin
    {
        #region 属性
        /// <summary>
        /// 登陆后返回的Html
        /// </summary>
        public static string ResultHtml { get; set; }
        /// <summary>
        /// 下一次请求的Url
        /// </summary>
        public static string NextRequestUrl { get; set; }
        /// <summary>
        /// 若要从远程调用中获取COOKIE一定要为request设定一个CookieContainer用来装载返回的cookies
        /// </summary>
        public static CookieContainer CookieContainer { get; set; }

        public static CookieCollection CurCookie { get; set; }

        /// <summary>
        /// Cookies 字符串
        /// </summary>
        public static string CookiesString { get; set; }

        public static WebClient Client { get; set; }
        #endregion

        #region 方法
        /// <summary>
        /// 用户登陆指定的网站
        /// </summary>
        /// <param name="loginUrl">"http://www.unitytrain.cn/login_check"</param>
        /// <param name="account"></param>
        /// <param name="password"></param>
        /// <param name="cookie"></param>
        public static void PostLogin(string loginUrl, string account, string password, string cookie, string csrf_token)
        {
            //string csrf_token = GetToken();
            HttpWebRequest request = null;
            HttpWebResponse response = null;
            try
            {
                string postdata = "_username=" + account + "&_password=" + password         //模拟请求数据，数据样式可以用FireBug插件得到。
                  + "&_csrf_token=" + csrf_token;
                request = (HttpWebRequest)WebRequest.Create(loginUrl);//实例化web访问类  

                request.Method = "POST";//数据提交方式为POST
                request.Accept = "text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8";
                request.Headers.Add("Accept-Encoding", "gzip, deflate");
                request.Headers.Add("Accept-Language", "zh-CN,zh;q=0.8,en-US;q=0.5,en;q=0.3");
                request.KeepAlive = true;
                request.Headers.Add("Cookie", cookie);
                request.Host = "www.unitytrain.cn";
                request.Headers.Add("Origin", "www.unitytrain.cn");
                request.Referer = "http://www.unitytrain.cn/login/ajax";
                request.UserAgent = "Mozilla/5.0 (Windows NT 6.1; rv:37.0) Gecko/20100101 Firefox/37.0";

                request.ContentType = "application/x-www-form-urlencoded";
                request.AllowAutoRedirect = true;   // 不用需自动跳转

                request.CookieContainer = new CookieContainer();

                //提交请求  
                byte[] postdatabytes = Encoding.UTF8.GetBytes(postdata);
                request.ContentLength = postdatabytes.Length;
                Stream stream;
                stream = request.GetRequestStream();
                //设置POST 数据
                stream.Write(postdatabytes, 0, postdatabytes.Length);
                stream.Close();


                //接收响应  
                response = (HttpWebResponse)request.GetResponse();
                CookiesString = response.Headers["Set-Cookie"];
                var s = response.Headers["Server"];
                ////保存返回cookie  
                //response.Cookies = request.CookieContainer.GetCookies(request.RequestUri);

                StreamReader sr = new StreamReader(response.GetResponseStream(), Encoding.Unicode);
                string content = sr.ReadToEnd();
                sr.Close();

                foreach (Cookie ck in response.Cookies)
                {
                    System.Diagnostics.Debug.WriteLine(ck.Name + "=" + ck.Value);
                }

                ////CookieContainer.Add(response.Cookies);
                //CookieCollection cook = response.Cookies;
                //string strcrook = request.CookieContainer.GetCookieHeader(request.RequestUri);
                //CookiesString = response.Headers["Set-Cookie"];
                //var coo = response.Headers["Set-Cookie"];

                //NextRequestUrl = "http://www.unitytrain.cn/course/40/lesson/402";

                //GetPage();

                //var s = ResultHtml;

                Client = new WebClient();
                Client.Headers.Add("Content-Type", "application/x-www-form-urlencoded");
                Client.Headers.Add("Accept-Language", "zh-cn");

                //CookiesString = "PHPSESSID=uhnc9j6ktg0kv2tdh89kj9t4k6; path=/";
                Client.Headers.Add("Cookie", CookiesString);

                System.Diagnostics.Debug.WriteLine(CookiesString);

                Client.DownloadStringAsync(new Uri("http://www.unitytrain.cn/course/40/lesson/402"));//测试链接
                Client.DownloadStringCompleted += Client_DownloadStringCompleted;

                ////取下一次GET跳转地址
                //StreamReader sr = new StreamReader(response.GetResponseStream(), Encoding.UTF8);
                //string content = sr.ReadToEnd();
                //sr.Close();
                //request.Abort();
                //response.Close();
                ////依据登陆成功后返回的Page信息，求出下次请求的url
                ////每个网站登陆后加载的Url和顺序不尽相同，以下两步需根据实际情况做特殊处理，从而得到下次请求的URL
                //string[] substr = content.Split(new char[] { '"' });
                //NextRequestUrl = substr[1];
            }
            catch (WebException ex)
            {
                MessageBox.Show(string.Format("登陆时出错，详细信息：{0}", ex.Message));
            }
        }

        private static string GetToken()
        {
            string getapiUrl = "http://www.unitytrain.cn/";
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(getapiUrl);

            //必须设置CookieContainer存储请求返回的Cookies
            if (CookieContainer != null)
                request.CookieContainer = CookieContainer;
            else
            {
                request.CookieContainer = new CookieContainer();
                CookieContainer = request.CookieContainer;
            }

            request.Method = "GET";
            HttpWebResponse response = (HttpWebResponse)request.GetResponse();
            StreamReader sr = new StreamReader(response.GetResponseStream());
            string respHtml = sr.ReadToEnd();

            CurCookie = response.Cookies;
            foreach (Cookie ck in response.Cookies)
            {
                CookiesString = ck.Name + "=" + ck.Value;
                System.Diagnostics.Debug.WriteLine(ck.Name + "=" + ck.Value);
            }

            WebClient ct = new WebClient();
            string ori = ct.DownloadString("http://s11.cnzz.com/stat.php?id=1253759411");

            CookieContainer.Add(response.Cookies);
            Match match = Regex.Match(respHtml, "meta content=\"([a-z0-9]+)\" name=\"csrf-token\"");
            string csrf_token = match.Groups[1].Captures[0].Value;

            return csrf_token;
        }

        static void Client_DownloadStringCompleted(object sender, DownloadStringCompletedEventArgs e)
        {
            try
            {
                var a = e.Result;
                MessageBox.Show("登录成功！");
            }
            catch { MessageBox.Show("登录失败！"); }
        }

        /// <summary>
        /// 获取用户登陆后下一次请求返回的内容
        /// </summary>
        public static void GetPage()
        {
            HttpWebRequest request = null;
            HttpWebResponse response = null;
            try
            {
                request = (HttpWebRequest)WebRequest.Create(NextRequestUrl);
                request.Headers["Cache-control"] = "no-cache";
                request.Headers["Accept-Language"] = "zh-cn";
                request.UserAgent = "Mozilla/5.0 (Windows NT 6.1; WOW64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/41.0.2272.76 Safari/537.36";
                request.ContentType = "application/x-www-form-urlencoded";
                request.Accept = "*/*";
                request.Method = "GET";
                request.KeepAlive = true;
                request.Headers.Add("Cookie", "PHPSESSID=htdkkbmph2d98pcn11iqn83dg6; path=/");
                request.CookieContainer = new System.Net.CookieContainer();
                request.AllowAutoRedirect = false;
                response = (HttpWebResponse)request.GetResponse();
                //设置cookie  
                CookiesString = request.CookieContainer.GetCookieHeader(request.RequestUri);
                //取再次跳转链接  
                StreamReader sr = new StreamReader(response.GetResponseStream(), Encoding.UTF8);
                string ss = sr.ReadToEnd();
                sr.Close();
                request.Abort();
                response.Close();
                //依据登陆成功后返回的Page信息，求出下次请求的url
                //每个网站登陆后加载的Url和顺序不尽相同，以下两步需根据实际情况做特殊处理，从而得到下次请求的URL
                //string[] substr = ss.Split(new char[] { '"' });
                //NextRequestUrl = substr[1];
                ResultHtml = ss;
            }
            catch (WebException ex)
            {
                MessageBox.Show(string.Format("获取页面HTML信息出错，详细信息：{0}", ex.Message));
            }
        }
        #endregion

        #region 服务器文件下载
        //TransmitFile实现下载
        protected void Button1_Click(object sender, EventArgs e)
        {
            /*
             微软为Response对象提供了一个新的方法TransmitFile来解决使用Response.BinaryWrite
             下载超过400mb的文件时导致Aspnet_wp.exe进程回收而无法成功下载的问题。
             代码如下：
             */
            HttpResponse Response = HttpContext.Current.Response;
            Response.ContentType = "application/x-zip-compressed";
            Response.AddHeader("Content-Disposition", "attachment;filename=z.zip");
            string filename = HttpContext.Current.Server.MapPath("DownLoad/z.zip");
            Response.TransmitFile(filename);
        }

        //WriteFile实现下载
        protected void Button2_Click(object sender, EventArgs e)
        {
            string fileName = "asd.txt";//客户端保存的文件名
            string filePath = HttpContext.Current.Server.MapPath("DownLoad/aaa.txt");//路径

            FileInfo fileInfo = new FileInfo(filePath);
            HttpResponse Response = HttpContext.Current.Response;
            Response.Clear();
            Response.ClearContent();
            Response.ClearHeaders();
            Response.AddHeader("Content-Disposition", "attachment;filename=" + fileName);
            Response.AddHeader("Content-Length", fileInfo.Length.ToString());
            Response.AddHeader("Content-Transfer-Encoding", "binary");
            Response.ContentType = "application/octet-stream";
            Response.ContentEncoding = System.Text.Encoding.GetEncoding("gb2312");
            Response.WriteFile(fileInfo.FullName);
            Response.Flush();
            Response.End();
        }

        //WriteFile分块下载
        protected void Button3_Click(object sender, EventArgs e)
        {
            string fileName = "aaa.txt";//客户端保存的文件名
            string filePath = HttpContext.Current.Server.MapPath("DownLoad/aaa.txt");//路径
            HttpResponse Response = HttpContext.Current.Response;
            System.IO.FileInfo fileInfo = new System.IO.FileInfo(filePath);

            if (fileInfo.Exists == true)
            {
                const long ChunkSize = 102400;//100K 每次读取文件，只读取100K，这样可以缓解服务器的压力
                byte[] buffer = new byte[ChunkSize];

                Response.Clear();
                System.IO.FileStream iStream = System.IO.File.OpenRead(filePath);
                long dataLengthToRead = iStream.Length;//获取下载的文件总大小
                Response.ContentType = "application/octet-stream";
                Response.AddHeader("Content-Disposition", "attachment; filename=" + HttpUtility.UrlEncode(fileName));
                while (dataLengthToRead > 0 && Response.IsClientConnected)
                {
                    int lengthRead = iStream.Read(buffer, 0, Convert.ToInt32(ChunkSize));//读取的大小
                    Response.OutputStream.Write(buffer, 0, lengthRead);
                    Response.Flush();
                    dataLengthToRead = dataLengthToRead - lengthRead;
                }
                Response.Close();
            }
        }

        //流方式下载
        protected void Button4_Click(object sender, EventArgs e)
        {
            string fileName = "aaa.txt";//客户端保存的文件名
            string filePath = HttpContext.Current.Server.MapPath("DownLoad/aaa.txt");//路径
            HttpResponse Response = HttpContext.Current.Response;
            //以字符流的形式下载文件
            FileStream fs = new FileStream(filePath, FileMode.Open);
            byte[] bytes = new byte[(int)fs.Length];
            fs.Read(bytes, 0, bytes.Length);
            fs.Close();
            Response.ContentType = "application/octet-stream";
            //通知浏览器下载文件而不是打开
            Response.AddHeader("Content-Disposition", "attachment;  filename=" + HttpUtility.UrlEncode(fileName, System.Text.Encoding.UTF8));
            Response.BinaryWrite(bytes);
            Response.Flush();
            Response.End();
        }
        #endregion
    }
}