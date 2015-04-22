using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using Microsoft.Practices.Prism.Commands;

namespace UnityTrainDownloader.ViewModels
{
    public class LoginViewModel : ViewModelBase
    {
        #region 属性
        private string _Username;
        /// <summary>
        /// 用户名
        /// </summary>
        public string Username
        {
            get { return _Username; }
            set
            {
                _Username = value;
                this.RaisePropertyChanged("Username");
            }
        }

        private string _Password;
        /// <summary>
        /// 密码
        /// </summary>
        public string Password
        {
            get { return _Password; }
            set
            {
                _Password = value;
                this.RaisePropertyChanged("Password");
            }
        }

        /// <summary>
        /// 登录状态
        /// </summary>
        public bool IsLogin { get; private set; }

        private string _LoginState;
        /// <summary>
        /// 登录状态文字
        /// </summary>
        public string LoginState
        {
            get { return _LoginState; }
            private set
            {
                _LoginState = value;
                this.RaisePropertyChanged("LoginState");
            }
        }

        /// <summary>
        /// 登录后cookie;
        /// </summary>
        public string Cookie { get; private set; }

        private DelegateCommand _LoginCommand;
        /// <summary>
        /// 登录命令
        /// </summary>
        public DelegateCommand LoginCommand
        {
            get
            {
                if (_LoginCommand == null)
                {
                    _LoginCommand = new DelegateCommand(Login);
                }
                return _LoginCommand;
            }
        }
        #endregion

        public LoginViewModel()
        {
            Username = "";
            Password = "";
            LoginState = "未登录";
            IsLogin = false;
        }

        private void Login()
        {
            if (!IsLogin)
            {
                WebUtil weber = new WebUtil();
                weber.Login(Username, Password);
                weber.LoginSuccessed += weber_LoginSuccessed;
            }
            else
                MessageBox.Show("用户已登录！");
        }

        void weber_LoginSuccessed(object sender, EventArgs e)
        {
            IsLogin = true;
            Cookie = sender as string;
            LoginState = "已登录";
        }
    }
}
