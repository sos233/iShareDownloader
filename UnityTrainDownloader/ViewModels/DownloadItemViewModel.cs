using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.ComponentModel;
using UnityTrainDownloader.Utils;

namespace UnityTrainDownloader.ViewModels
{
    public class DownloadItemViewModel : ViewModelBase
    {
        #region 字段
        /// <summary>
        /// 用户多线程同步的对象
        /// </summary>
        private static object syncObject = new object();

        /// <summary>
        /// 用于计算速度的临时变量
        /// </summary>
        private long downloadedBytes = 0;

        /// <summary>
        /// 用户下载的通信对象
        /// </summary>
        private readonly WebClient client;

        /// <summary>
        /// 每次读取的字节数
        /// </summary>
        private int readBytes = 100 * 1024;

        /// <summary>
        /// 获取到的课列表信息
        /// </summary>
        private string lessonList;

        /// <summary>
        /// 后台下载线程
        /// </summary>
        private BackgroundWorker worker;
        #endregion

        #region 属性
        private string _DownloadUrl;
        /// <summary>
        /// 传入的下载地址
        /// </summary>
        public string DownloadUrl
        {
            get { return _DownloadUrl; }
            private set
            {
                _DownloadUrl = value;
                this.RaisePropertyChanged("DownloadUrl");
            }
        }

        private double _FileProgress;
        /// <summary>
        /// 文件下载进度
        /// </summary>
        public double FileProgress
        {
            get { return _FileProgress; }
            private set
            {
                _FileProgress = value;
                this.RaisePropertyChanged("FileProgress");
            }
        }

        private long _DownloadBytes;
        /// <summary>
        /// 已下载的字节数
        /// </summary>
        public long DownloadBytes
        {
            get { return _DownloadBytes; }
            set
            {
                _DownloadBytes = value;
                this.RaisePropertyChanged("DownloadBytes", "Progress");
            }
        }

        private long _TotalBytes;
        /// <summary>
        /// 要下载的字节总数(文件大小)
        /// </summary>
        public long TotalBytes
        {
            get { return _TotalBytes; }
            set
            {
                _TotalBytes = value;
                this.RaisePropertyChanged("TotalBytes", "Progress");
            }
        }

        private bool _IsDownloading;
        /// <summary>
        /// 是否正在下载
        /// </summary>
        public bool IsDownloading
        {
            get { return _IsDownloading; }
            set
            {
                _IsDownloading = value;
                this.RaisePropertyChanged("IsDownloading");
            }
        }

        /// <summary>
        /// 当前进度
        /// </summary>
        public double Progress
        {
            get { return DownloadBytes * 100.0 / TotalBytes; }
        }

        private double _Speed;
        /// <summary>
        /// 即时速度
        /// </summary>
        public double Speed
        {
            get { return _Speed; }
            set
            {
                _Speed = value;
                this.RaisePropertyChanged("Speed");
            }
        }

        private bool _IsCompleted;
        /// <summary>
        /// 是否已完成
        /// </summary>
        public bool IsCompleted
        {
            get { return _IsCompleted; }
            set
            {
                _IsCompleted = value;
                this.RaisePropertyChanged("IsCompleted");
            }
        }

        /// <summary>
        /// 保存在本地的文件名称
        /// </summary>
        private string _Filename;
        public string Filename
        {
            get { return _Filename; }
            set
            {
                _Filename = value;
                this.RaisePropertyChanged("Filename");
            }
        }

        /// <summary>
        /// 保存在本地的文件夹路径
        /// </summary>
        public string DownloadDir { get; set; }
        #endregion

        #region 构造器
        //public DownloadItemViewModel(string downloadUrl)
        //{
        //    this.DownloadUrl = downloadUrl;
        //    client = new WebClient();
        //    client.DownloadProgressChanged += client_DownloadProgressChanged;
        //    client.DownloadFileCompleted += client_DownloadFileCompleted;
        //    this.Filename = string.Format(@"C:\Users\Administrator.WIN-FRSB8EK192B\Desktop\Test\{0}.tmp", Guid.NewGuid());
        //    StartDownload();
        //}

        public DownloadItemViewModel(string downloadUrl, string downloadDir, string filename, string cookie)
        {
            this.DownloadUrl = downloadUrl;
            this.DownloadDir = downloadDir;
            this.Filename = filename;

            client = new WebClient();
            client.Headers.Add("Content-Type", "application/x-www-form-urlencoded");
            client.Headers.Add("Accept-Language", "zh-cn");
            client.Headers.Add("Cookie", cookie);

            string jsonUrl = downloadUrl.Replace("learn#", "");
            client.DownloadStringAsync(new Uri(jsonUrl));
            client.DownloadStringCompleted += client_DownloadStringCompleted;
        }

        void client_DownloadFileCompleted(object sender, System.ComponentModel.AsyncCompletedEventArgs e)
        {
            this.IsDownloading = false;
            this.Speed = 0;
            if (!e.Cancelled)
            {
                this.IsCompleted = true;
            }
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
                        this.lessonList = client.DownloadString(hdLessonUrl);

                        //开启后台线程，无法直接调用界面控件相关属性
                        worker = new BackgroundWorker();
                        worker.DoWork += worker_DoWork;
                        worker.ProgressChanged += worker_ProgressChanged;
                        worker.WorkerReportsProgress = true;
                        worker.RunWorkerAsync();
                    }
                }
            }
            catch (Exception ex) { System.Windows.MessageBox.Show("错误\n" + ex.TargetSite); }
        }

        void worker_DoWork(object sender, DoWorkEventArgs e)
        {
            WebClient wc = new WebClient();

            string receivePath = this.DownloadDir + "\\" + this.Filename + ".flv";
            if (File.Exists(receivePath) || string.IsNullOrWhiteSpace(this.Filename))
                receivePath = DownloadDir + "\\" + Guid.NewGuid() + ".flv";
            FileStream fs = new FileStream(receivePath, FileMode.Create, FileAccess.Write); ;

            MatchCollection collects = Regex.Matches(lessonList, "URI=\"(.+?)\",IV=0x([a-z0-9]+)\n.+?\n(http://esd1a8b9c079-pub.alcdn.edusoho.net/courselesson.+?)\n");
            string keyUrl = collects[0].Groups[1].Captures[0].Value;
            string keyStr = wc.DownloadString(keyUrl);
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

                byte[] oriDatas = wc.DownloadData(flvUrl);
                byte[] datas = AESManager.AESDecrypt(oriDatas, ivs, keyStr);
                fs.Write(datas, 0, datas.Length);
                fs.Flush();

                //界面更新方式一
                //this.Dispatcher.Invoke(new UIRefreshHandler(RefreshUI), //同步执行
                //    DispatcherPriority.Normal, //优先级设置
                //    new string[] { i.ToString() });
                //this.DoEvents();

                //界面更新方式二
                worker.ReportProgress(i * 100 / (collects.Count - 1));

                //System.Diagnostics.Debug.WriteLine(keyStr + "==" + iv + "==" + flvUrl);
            }

            fs.Close();
            //client.DownloadString("http://www.unitytrain.cn/logout");
            System.Windows.MessageBox.Show("下载完成！");
        }

        void worker_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            FileProgress = e.ProgressPercentage;
        }
        #endregion

        /// <summary>
        /// 下载进度变化时触发的方法
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void client_DownloadProgressChanged(object sender, DownloadProgressChangedEventArgs e)
        {
            this.TotalBytes = e.TotalBytesToReceive;
            this.DownloadBytes = e.BytesReceived;
        }

        /// <summary>
        /// 计算速度
        /// </summary>
        /// <param name="milliseconds"></param>
        public void InitSpeed(int milliseconds)
        {
            if (IsCompleted) return;
            if (!IsDownloading) return;
            if (milliseconds <= 0) return;
            lock (syncObject)
            {
                var haveDownloaded = this.DownloadBytes - downloadedBytes;
                this.Speed = (haveDownloaded * 1000.0) / milliseconds;
                downloadedBytes = this.DownloadBytes;
            }
        }

        /// <summary>
        /// 开始下载
        /// </summary>
        public void StartDownload()
        {
            IsDownloading = true;
            HttpWebRequest request = (HttpWebRequest)FileWebRequest.Create(this.DownloadUrl);
            if (DownloadBytes > 0)
            {
                request.AddRange(DownloadBytes);
            }
            request.BeginGetResponse(ar =>
            {
                var response = request.EndGetResponse(ar);
                if (this.TotalBytes == 0) this.TotalBytes = response.ContentLength;
                using (var writer = new FileStream(this.Filename, FileMode.OpenOrCreate))
                {
                    using (var stream = response.GetResponseStream())
                    {
                        while (IsDownloading)
                        {
                            byte[] data = new byte[readBytes];
                            int readNumber = stream.Read(data, 0, data.Length);
                            if (readNumber > 0)
                            {
                                writer.Write(data, 0, readNumber);
                                this.DownloadBytes += readNumber;

                            }
                            if (this.DownloadBytes == this.TotalBytes)
                            {
                                Complete();
                            }
                        }
                    }
                }
            }, null);
        }

        public void Complete()
        {
            this.IsCompleted = true;
            this.IsDownloading = false;
            this.Speed = 0;
        }

        public void PauseDownload()
        {
            IsDownloading = false;
            this.Speed = 0;
        }

        public void DeleteFile()
        {

        }
    }
}
