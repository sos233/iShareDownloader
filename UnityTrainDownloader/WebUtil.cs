using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using UnityTrainDownloader.Utils;

namespace UnityTrainDownloader
{
    public class WebUtil
    {
        /// <summary>
        /// 判断是否登录
        /// </summary>
        private bool isLogin;
        /// <summary>
        /// 登录用户名
        /// </summary>
        private string username;
        /// <summary>
        /// 登录密码
        /// </summary>
        private string password;

        /// <summary>
        /// 登录通信对象
        /// </summary>
        private static WebClient client;

        /// <summary>
        /// 登录成功后响应事件
        /// </summary>
        public event EventHandler LoginSuccessed;

        public void Login(string username, string password)
        {
            this.username = username;
            this.password = password;

            WebBrowser brower = new WebBrowser();
            brower.Navigate("http://www.unitytrain.cn/login/ajax");
            brower.ScriptErrorsSuppressed = true;
            brower.DocumentCompleted += brower_DocumentCompleted;
        }

        private void brower_DocumentCompleted(object sender, WebBrowserDocumentCompletedEventArgs e)
        {
            WebBrowser brower = sender as WebBrowser;
            string cookie = brower.Document.Cookie;

            //模拟form表单提交
            if (e.Url.ToString().Trim().Contains("login/ajax"))
            {
                string html = brower.DocumentText;
                Match match = Regex.Match(html, "_csrf_token\" value=\"([a-z0-9]+)\"");
                if (match.Success)
                {
                    string csrf_token = match.Groups[1].Captures[0].Value;
                    HtmlDocument doc = brower.Document;
                    for (int i = 0; i < doc.All.Count; i++)
                    {
                        if (doc.All[i].TagName.ToUpper().Equals("INPUT"))
                        {
                            switch (doc.All[i].Name)
                            {
                                case "_username":
                                    doc.All[i].InnerText = username;
                                    break;
                                case "_password":
                                    doc.All[i].InnerText = password;
                                    break;
                                case "_csrf_token":
                                    doc.All[i].InnerText = csrf_token;
                                    break;
                            }
                        }
                    }
                    HtmlElement formLogin = brower.Document.Forms["login-ajax-form"];
                    formLogin.InvokeMember("submit");
                }
            }

            else if (e.Url.ToString().Trim().Contains("goto"))
            {
                client = new WebClient();
                client.Headers.Add("Content-Type", "application/x-www-form-urlencoded");
                client.Headers.Add("Accept-Language", "zh-cn");
                client.Headers.Add("Cookie", cookie);

                //测试登录是否成功
                try
                {
                    if (!isLogin)
                    {
                        client.DownloadString(new Uri("http://www.unitytrain.cn/course/40/lesson/402"));
                        isLogin = true;
                        LoginSuccessed.Invoke(cookie, null);
                        System.Windows.MessageBox.Show("登录成功！");
                    }
                }
                catch (Exception ex)
                {
                    System.Windows.MessageBox.Show("登录失败！\n" + ex.Message);
                }
            }
        }
    }
}
