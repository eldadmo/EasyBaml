using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Autofac;
using EasyBamlAddin.Services;
using EasyBamlAddin.Services.Settings;
using EasyBamlAddin.Tools;
using EasyBamlAddin.ViewModels;
using EasyBamlAddin.Views;
using EnvDTE80;

namespace EasyBamlAddin
{
    public class Bootstrapper : IDisposable, IViewFactory
    {
        private IntPtr ownerHwnd;
        private IContainer container;

        public void Initialize(DTE2 applicationObject)
        {
            var builder = new ContainerBuilder();
            
            builder.RegisterType<SettingsService>().As<ISettingsService>().SingleInstance();
            builder.RegisterType<LocalizabilityChecker>().As<ILocalizabilityChecker>().SingleInstance();

            builder.RegisterType<SettingsViewModel>();
            builder.RegisterType<MainWindowViewModel>();
            builder.RegisterType<ManageUidViewModel>();

            builder.RegisterInstance(new VisualStudioAdapter(applicationObject)).As<IVisualStudioAdapter>();
            builder.RegisterInstance(this).As<IViewFactory>();
            ownerHwnd = (IntPtr)applicationObject.MainWindow.HWnd;

            container = builder.Build();
        }

        public void Run()
        {
            if (!CheckSettingsExists()) return;

            var mainWindowVm = container.Resolve<MainWindowViewModel>();
            var mainWindow = new MainWindowView();
            mainWindowVm.Window = mainWindow;
            mainWindow.DataContext = mainWindowVm;
            mainWindow.ShowModal(); //Need to use DialogWindow.ShowModal to correctly open modal dialog in VS

            //Use VS DialogWindow instead
            //ViewTools.ShowModalWindow<MainWindowView>(mainWindowVm, ownerHwnd);
        }

        private bool CheckSettingsExists()
        {
            if (!container.Resolve<ISettingsService>().IsSolutionSettingsExist)
            {
                return ShowSettings();
            }
            return true;
        }

        private bool ShowSettings()
        {
            var settingsVM = container.Resolve<SettingsViewModel>();

            return ViewTools.ShowModalWindow<SettingsView>(settingsVM, ownerHwnd).GetValueOrDefault();
        }

        public TViewModel CreateViewModel<TViewModel>()
        {
            return container.Resolve<TViewModel>();
        }

        public void Dispose()
        {
            container?.Dispose();
        }
    }
}
