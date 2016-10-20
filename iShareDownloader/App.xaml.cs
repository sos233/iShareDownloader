using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using Microsoft.Practices.Prism.Events;

namespace iShareDownloader
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public static IEventAggregator EventAggregator { get; set; }
        static App()
        {
            EventAggregator = new EventAggregator();
        }
    }
}
