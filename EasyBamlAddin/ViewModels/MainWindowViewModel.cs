using System;
using System.Reflection;
using System.Windows;
using EasyBamlAddin.Services;
using EasyBamlAddin.Tools;
using EasyBamlAddin.Views;

namespace EasyBamlAddin.ViewModels
{
    public class MainWindowViewModel : BaseWindowAwareViewModel
    {
        private readonly IViewFactory viewFactory;

        public MainWindowViewModel(IViewFactory viewFactory)
        {
            this.viewFactory = viewFactory;

            CloseCommand = new DelegateCommand(_ => Close());

            ShowSettingsCommand = new DelegateCommand(ShowSettings);

            CheckUIDsCommand = new DelegateCommand(CheckUIDs);

            PrepareTranslationCommand = new DelegateCommand(PrepareTranslationHandler);

            FillVersionInfo();
        }

        public DelegateCommand CloseCommand { get; private set; }

        public DelegateCommand ShowSettingsCommand { get; private set; }

        public DelegateCommand CheckUIDsCommand { get; private set; }

        public DelegateCommand PrepareTranslationCommand { get; private set; }

        public string VersionInfo { get; set; }

        private void ShowSettings(object obj)
        {
            ShowSettings();
        }

        private bool? ShowSettings()
        {
            try
            {
                var settingsVM = viewFactory.CreateViewModel<SettingsViewModel>();
                return ViewTools.ShowModalWindow<SettingsView>(settingsVM, Window);
            }
            catch (Exception e)
            {
                MessageBox.Show("Error occured: " + e.Message, "Easy BAML");
                return null;
            }
        }

        private void CheckUIDs(object obj)
        {
            try
            {
                var vm = viewFactory.CreateViewModel<ManageUidViewModel>();
                ViewTools.ShowModalWindow<ManageUidView>(vm, Window);
            }
            catch (Exception e)
            {
                MessageBox.Show("Error occured: " + e.Message, "Easy BAML");
            }
        }

        private void PrepareTranslationHandler(object obj)
        {
            try
            {
                var vm = viewFactory.CreateViewModel<ManageUidViewModel>();
                vm.Operation = vm.DoPrepareTranslation;
                ViewTools.ShowModalWindow<OperationProgressView>(vm, Window);
            }
            catch (Exception e)
            {
                MessageBox.Show("Error occured: " + e.Message, "Easy BAML");
            }
        }

        private void FillVersionInfo()
        {
            VersionInfo = Assembly.GetCallingAssembly().GetName().Version.ToString();
        }
    }
}
