using System;
using System.IO;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Web;
using System.Windows;
using CookieHandler;
using DotNet4.Utilities;
using System.Windows.Forms;
using System.Windows.Media;
using System.ComponentModel;
using System.Windows.Threading;

namespace UnityTrainDownloader
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private bool state = false;
        private string lessonList;
        private string fileDir;
        private string fileName;
        private WebClient client;
        private BackgroundWorker worker;

        public string LessonUrl { get { return txtUrl.Text.Replace("learn#", ""); } }

        public MainWindow()
        {
            InitializeComponent();

            txtUrl.Text = "http://www.unitytrain.cn/course/40/learn#lesson/402";
            txtUser.Text = "";
            txtPassword.Password = "";
            btnChooseDir.Click += btnChooseDir_Click;
            btnLogin.Click += btnLogin_Click;
        }

        public delegate void UIRefreshHandler(string val);
        public void RefreshUI(string val)
        {
            txtProgress.Text = val + "%";
            progressBar.Value = int.Parse(val);
        }

        #region 网站登录及验证
        void btnLogin_Click(object sender, RoutedEventArgs e)
        {
            //第三方dll获取Cookie
            //var a = FullWebBrowserCookie.GetCookieList(new Uri("http://www.unitytrain.cn/login"), false);
            //var b = FullWebBrowserCookie.GetCookieInternal(new Uri("http://www.unitytrain.cn/login"), false);
            //var c = FullWebBrowserCookie.GetCookieValue(new Uri("http://www.unitytrain.cn/login"), false);

            WebBrowser brower = new WebBrowser();
            brower.Navigate("http://www.unitytrain.cn/login/ajax");
            //brower.Navigated += brower_Navigated;

            brower.ScriptErrorsSuppressed = true;
            brower.DocumentCompleted += new WebBrowserDocumentCompletedEventHandler(brower_DocumentCompleted);
        }

        void brower_DocumentCompleted(object sender, WebBrowserDocumentCompletedEventArgs e)
        {
            if (state)
                return;

            WebBrowser brower = sender as WebBrowser;
            //dynamic bridge = brower.Document.InvokeScript("eval", new String[] { "_CNZZDbridge_1253759411.bobject.A.cnzz_eid" });
            string cookie = brower.Document.Cookie;

            if (e.Url.ToString().Trim().Contains("login/ajax"))
            {
                string html = brower.DocumentText;
                Match match = Regex.Match(html, "_csrf_token\" value=\"([a-z0-9]+)\"");
                string csrf_token = match.Groups[1].Captures[0].Value;
                HtmlDocument doc = brower.Document;
                for (int i = 0; i < doc.All.Count; i++)
                {
                    if (doc.All[i].TagName.ToUpper().Equals("INPUT"))
                    {
                        switch (doc.All[i].Name)
                        {
                            case "_username":
                                doc.All[i].InnerText = txtUser.Text;
                                break;
                            case "_password":
                                doc.All[i].InnerText = txtPassword.Password;
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

            else if (e.Url.ToString().Trim().Contains("goto"))
            {
                client = new WebClient();
                client.Headers.Add("Content-Type", "application/x-www-form-urlencoded");
                client.Headers.Add("Accept-Language", "zh-cn");
                client.Headers.Add("Cookie", cookie);

                //测试链接
                client.DownloadStringAsync(new Uri("http://www.unitytrain.cn/course/40/lesson/402"));
                client.DownloadStringCompleted += Client_DownloadStringCompleted;
            }
        }

        private void Client_DownloadStringCompleted(object sender, DownloadStringCompletedEventArgs e)
        {
            try
            {
                if (!state)
                {
                    txtState.Foreground = new SolidColorBrush(Colors.Green);
                    txtState.Text = "已登录";
                    state = true;
                    System.Windows.MessageBox.Show("登录成功！");
                }
            }
            catch { System.Windows.MessageBox.Show("登录失败！"); }
        }
        #endregion

        void btnChooseDir_Click(object sender, RoutedEventArgs e)
        {
            FolderBrowserDialog fbd = new FolderBrowserDialog();
            if (fbd.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                txtFileDir.Text = fbd.SelectedPath;
            }
        }

        private void btnDownload_Click(object sender, RoutedEventArgs e)
        {
            if (client == null)
            {
                System.Windows.MessageBox.Show("请先登陆！");
                return;
            }

            if (txtFileName.Text.Length == 0)
            {
                System.Windows.MessageBox.Show("文件名不能为空！");
                return;
            }

            string jsonUrl = txtUrl.Text.Replace("learn#", "");
            client.DownloadStringAsync(new Uri(jsonUrl));
            client.DownloadStringCompleted += client_DownloadStringCompleted;
        }

        void client_DownloadStringCompleted(object sender, DownloadStringCompletedEventArgs e)
        {
            try
            {
                Match match = Regex.Match(e.Result, "\"mediaHLSUri\":\"(.+?)\"");
                if (match.Success)
                {
                    string listUrl = match.Groups[1].Captures[0].Value.Replace("\\", "");
                    string listTxt = client.DownloadString(listUrl);
                    match = Regex.Match(listTxt, "\n(.+?stream/hd.+?)$");
                    if (match.Success)
                    {
                        string hdLessonUrl = match.Groups[1].Captures[0].Value.Replace("\\", "");
                        lessonList = client.DownloadString(hdLessonUrl);

                        fileDir = txtFileDir.Text;
                        fileName = txtFileName.Text;

                        //开启后台线程，无法直接调用界面控件相关属性
                        worker = new BackgroundWorker();
                        worker.DoWork += worker_DoWork;
                        worker.ProgressChanged += worker_ProgressChanged;
                        worker.WorkerReportsProgress = true;
                        worker.RunWorkerAsync();
                    }
                }
            }
            catch (Exception ex) { System.Windows.MessageBox.Show(ex.Message); }
        }

        void worker_DoWork(object sender, DoWorkEventArgs e)
        {
            string directory = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
            if (fileDir.Length > 0)
                directory = fileDir;
            string receivePath = directory + "\\" + fileName + ".flv";
            FileStream fstr = new FileStream(receivePath, FileMode.Create, FileAccess.Write); ;

            MatchCollection collects = Regex.Matches(lessonList, "URI=\"(.+?)\",IV=0x([a-z0-9]+)\n.+?\n(http://esd1a8b9c079-pub.alcdn.edusoho.net/courselesson.+?)\n");
            string keyUrl = collects[0].Groups[1].Captures[0].Value;
            string keyStr = client.DownloadString(keyUrl);
            for (int i = 0; i < collects.Count; i++)
            {
                string iv = collects[i].Groups[2].Captures[0].Value;
                string flvUrl = collects[i].Groups[3].Captures[0].Value;

                byte[] ivs = new byte[16];
                for (int j = 0; j < iv.Length / 2; j++)
                {
                    string temp = iv.Substring(j * 2, 2);
                    ivs[j] = Convert.ToByte(temp, 16);
                }
                byte[] oriDatas = client.DownloadData(flvUrl);

                byte[] datas = AESManager.AESDecrypt(oriDatas, ivs, keyStr);
                fstr.Write(datas, 0, datas.Length);
                fstr.Flush();

                //界面更新方式一
                //this.Dispatcher.Invoke(new UIRefreshHandler(RefreshUI), //同步执行
                //    DispatcherPriority.Normal, //优先级设置
                //    new string[] { i.ToString() });
                //this.DoEvents();

                //界面更新方式二
                worker.ReportProgress(i * 100 / (collects.Count - 1));

                System.Diagnostics.Debug.WriteLine(keyStr + "==" + iv + "==" + flvUrl);
            }

            fstr.Close();
            //client.DownloadString("http://www.unitytrain.cn/logout");
            System.Windows.MessageBox.Show("下载完成！");
        }

        void worker_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            progressBar.Value = e.ProgressPercentage;//获取进度百分比
            txtProgress.Text = e.ProgressPercentage + "%";
        }
    }
}
