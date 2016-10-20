using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Practices.Prism.Events;
using Microsoft.Practices.Prism.ViewModel;

namespace iShareDownloader.ViewModels
{
    /// <summary>
    /// ViewModel基类
    /// </summary>
    public class ViewModelBase : NotificationObject
    {
        /// <summary>
        /// 事件聚合器
        /// </summary>
        protected IEventAggregator EventAggregator
        {
            get
            {
                return App.EventAggregator;
            }
        }
    }
}
