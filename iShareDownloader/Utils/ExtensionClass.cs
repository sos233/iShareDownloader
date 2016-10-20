using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Threading;

namespace iShareDownloader.Utils
{
    public static class ExtensionClass
    {
        //扩展方法进行界面刷新
        public static void DoEvents(this Window win)
        {
            DispatcherFrame frame = new DispatcherFrame();
            Dispatcher.CurrentDispatcher.BeginInvoke(DispatcherPriority.Background,
                new DispatcherOperationCallback(ExitFrames), frame);
            Dispatcher.PushFrame(frame);
        }

        public static object ExitFrames(object f)
        {
            ((DispatcherFrame)f).Continue = false;
            return null;
        }
    }
}
