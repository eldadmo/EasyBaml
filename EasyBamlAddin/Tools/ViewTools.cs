using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Interop;

namespace EasyBamlAddin.Tools
{
    public static class ViewTools
    {
        public static bool? ShowModalWindow(Window view, object viewModel)
        {
            view.DataContext = viewModel;
            return view.ShowDialog();
        }

        public static bool? ShowModalWindow(Window view, IViewModel viewModel)
        {
            viewModel.Window = view;
            view.DataContext = viewModel;
            return view.ShowDialog();
        }

        public static bool? ShowModalWindow<TWindow>(IViewModel viewModel) where TWindow : Window, new()
        {
            var view = new TWindow();
            return ShowModalWindow(view, viewModel);
        }

        public static bool? ShowModalWindow<TWindow>(IViewModel viewModel, IntPtr ownerHwnd) where TWindow : Window, new()
        {
            var view = new TWindow();
            var helper = new WindowInteropHelper(view);
            helper.Owner = ownerHwnd;
            return ShowModalWindow(view, viewModel);
        }

        public static bool? ShowModalWindow<TWindow>(IViewModel viewModel, Window ownerWindow) where TWindow : Window, new()
        {
            var view = new TWindow();
            view.Owner = ownerWindow;
            return ShowModalWindow(view, viewModel);
        }
    }
}
