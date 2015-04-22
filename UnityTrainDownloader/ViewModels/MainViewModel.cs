using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Windows.Threading;
using Microsoft.Practices.Prism.Commands;

namespace UnityTrainDownloader.ViewModels
{
    public class MainViewModel : ViewModelBase
    {
        #region 字段
        /// <summary>
        /// 计时器
        /// </summary>
        private DispatcherTimer timer;

        /// <summary>
        /// 多线程同步的字段
        /// </summary>
        private static object syncObject = new object();

        /// <summary>
        /// 统计速度的间隔
        /// </summary>
        private int interval = 1000;
        #endregion

        #region 属性
        /// <summary>
        /// 用户登录信息
        /// </summary>
        public LoginViewModel LoginModel { get; set; }

        /// <summary>
        /// 下载文件保存名
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
        /// 下载文件保存路径
        /// </summary>
        private string _DownloadDir;
        public string DownloadDir
        {
            get { return _DownloadDir; }
            set
            {
                _DownloadDir = value;
                this.RaisePropertyChanged("DownloadDir");
            }
        }

        private DelegateCommand _ChooseDirCommand;
        /// <summary>
        /// 下载文件保存路径选择
        /// </summary>
        public DelegateCommand ChooseDirCommand
        {
            get
            {
                if (_ChooseDirCommand == null)
                {
                    _ChooseDirCommand = new DelegateCommand(ChooseDir);
                }
                return _ChooseDirCommand;
            }
        }

        /// <summary>
        /// 下载文件列表
        /// </summary>
        public ObservableCollection<DownloadItemViewModel> DownloadFileList { get; private set; }

        private string _DownloadUrl;
        /// <summary>
        /// 要添加的下载路径
        /// </summary>
        public string DownloadUrl
        {
            get { return _DownloadUrl; }
            set
            {
                _DownloadUrl = value;
                this.RaisePropertyChanged("DownloadUrl");
            }
        }

        private double _Speed;
        /// <summary>
        /// 总的速度
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

        private DelegateCommand _AddFileCommand;
        /// <summary>
        /// 添加下载文件命令
        /// </summary>
        public DelegateCommand AddFileCommand
        {
            get
            {
                if (_AddFileCommand == null)
                {
                    _AddFileCommand = new DelegateCommand(AddFile);
                }
                return _AddFileCommand;
            }
        }

        private DelegateCommand _PauseCommand;
        /// <summary>
        /// 暂停所有下载命令
        /// </summary>
        public DelegateCommand PauseCommand
        {
            get
            {
                if (_PauseCommand == null)
                {
                    _PauseCommand = new DelegateCommand(Pause);
                }
                return _PauseCommand;
            }
        }

        private DelegateCommand _StartCommand;
        /// <summary>
        /// 开始下载命令
        /// </summary>
        public DelegateCommand StartCommand
        {
            get
            {
                if (_StartCommand == null)
                {
                    _StartCommand = new DelegateCommand(Start);
                }
                return _StartCommand;
            }
        }
        #endregion

        #region 构造器
        public MainViewModel()
        {
            timer = new DispatcherTimer();
            //每秒统计一次速度
            timer.Interval = TimeSpan.FromMilliseconds(interval);
            timer.Tick += timer_Tick;
            timer.Start();

            LoginModel = new LoginViewModel();
            DownloadFileList = new ObservableCollection<DownloadItemViewModel>();
            DownloadDir = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
            DownloadUrl = @"http://www.unitytrain.cn/course/40/learn#lesson/402";
        }
        #endregion

        #region 方法
        private void AddFile()
        {
            if (!string.IsNullOrEmpty(DownloadUrl))
            {
                if (LoginModel.IsLogin)
                {
                    var file = new DownloadItemViewModel(DownloadUrl, DownloadDir, Filename, LoginModel.Cookie);
                    DownloadFileList.Add(file);
                    DownloadUrl = null;
                }
                else
                    MessageBox.Show("请先登录！");
            }
        }

        private void ChooseDir()
        {
            FolderBrowserDialog fbd = new FolderBrowserDialog();
            if (fbd.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                DownloadDir = fbd.SelectedPath;
        }

        private void Pause()
        {
            DownloadFileList.AsParallel().ForAll(t =>
            {
                t.PauseDownload();
            });
        }

        private void Start()
        {
            DownloadFileList.AsParallel().ForAll(t =>
            {
                t.StartDownload();
            });
        }

        private void timer_Tick(object sender, EventArgs e)
        {
            double speed = 0;
            DownloadFileList.AsParallel().ForAll(t =>
            {
                t.InitSpeed(interval);
                speed += t.Speed;
            });
            this.Speed = speed;
        }
        #endregion
    }
}
