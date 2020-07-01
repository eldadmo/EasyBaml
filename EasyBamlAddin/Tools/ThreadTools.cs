using System;
using System.Windows;
using System.Windows.Threading;

namespace EasyBamlAddin.Tools
{
    public static class ThreadTools
    {
        public static void InvokeInUIThread(Window window, Action action)
        {
            window.Dispatcher.Invoke(DispatcherPriority.Normal, action);
        }
    }
}
