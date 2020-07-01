using System.ComponentModel;
using System.Windows;
using EasyBamlAddin.Tools;

namespace EasyBamlAddin.ViewModels
{
    public class BaseWindowAwareViewModel : BaseViewModel, IViewModel
    {
        private Window window;

        public Window Window
        {
            get { return window; }
            set
            {
                if (window != value)
                {
                    if (window != null)
                    {
                        window.Loaded -= window_Loaded;
                        window.Closing -= window_Closing;
                        window.Closed -= window_Closed;
                    }

                    window = value;
                    
                    if (window != null)
                    {
                        window.Loaded += window_Loaded;
                        window.Closing += window_Closing;
                        window.Closed += window_Closed;
                    }
                }
            }
        }

        private void window_Closed(object sender, System.EventArgs e)
        {
            WindowClosed();
        }

        private void window_Closing(object sender, CancelEventArgs e)
        {
            WindowClosing(e);
        }

        private void window_Loaded(object sender, RoutedEventArgs e)
        {
            WindowLoaded();
        }

        protected virtual void WindowLoaded()
        {            
        }

        protected virtual void WindowClosing(CancelEventArgs e)
        {
        }
        
        protected virtual void WindowClosed()
        {
        }

        public void Close(bool? dialogResult = null)
        {
            Window.DialogResult = dialogResult;
            Window.Close();            
        }
    }
}
