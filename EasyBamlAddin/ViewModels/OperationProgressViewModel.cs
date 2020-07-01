using System;
using System.ComponentModel;
using System.Threading;
using System.Windows;
using EasyBamlAddin.Tools;

namespace EasyBamlAddin.ViewModels
{
    public interface IOperationProgressMonitor
    {
        bool IsIndeterminate { get; set; }
        int TotalSteps { get; set; }
        int CurrentStep { get; set; }
        double Percent { get; set; }
        string OperationDescription { get; set; }
        string StepDescription { get; set; }
    }

    public class OperationProgressViewModel : BaseWindowAwareViewModel, IOperationProgressMonitor
    {
        private int currentStep;
        private bool isIndeterminate;
        private bool isRunning;
        private string operationDescription;
        private double percent;
        private string stepDescription;

        private int totalSteps = 1;
        private string windowTitle;


        public OperationProgressViewModel()
        {
            OKCommand = new DelegateCommand(OKHandler);
            AbortCommand = new DelegateCommand(AbortHandler);
        }

        public string WindowTitle
        {
            get { return windowTitle; }
            set
            {
                if (windowTitle != value)
                {
                    windowTitle = value;
                    OnPropertyChanged("WindowTitle");
                }
            }
        }

        public bool IsIndeterminate
        {
            get { return isIndeterminate; }
            set
            {
                if (isIndeterminate != value)
                {
                    isIndeterminate = value;
                    OnPropertyChanged("IsIndeterminate");
                }
            }
        }

        public int TotalSteps
        {
            get { return totalSteps; }
            set
            {
                if (totalSteps != value)
                {
                    totalSteps = value;
                    OnPropertyChanged("TotalSteps");
                }
            }
        }

        public int CurrentStep
        {
            get { return currentStep; }
            set
            {
                if (currentStep != value)
                {
                    currentStep = value;
                    OnPropertyChanged("CurrentStep");
                }
            }
        }

        public double Percent
        {
            get { return percent; }
            set
            {
                if (percent != value)
                {
                    percent = value;
                    OnPropertyChanged("Percent");
                }
            }
        }

        public string OperationDescription
        {
            get { return operationDescription; }
            set
            {
                if (operationDescription != value)
                {
                    operationDescription = value;
                    OnPropertyChanged("OperationDescription");
                }
            }
        }

        public string StepDescription
        {
            get { return stepDescription; }
            set
            {
                if (stepDescription != value)
                {
                    stepDescription = value;
                    OnPropertyChanged("StepDescription");
                }
            }
        }

        public bool IsRunning
        {
            get { return isRunning; }
            set
            {
                if (isRunning != value)
                {
                    isRunning = value;
                    OnPropertyChanged("IsRunning");
                }
            }
        }


        public Action Operation { get; set; }

        /// <summary>
        /// Method should return 'true' if operation was aborted
        /// </summary>
        public Func<bool> OnOperationAbortRequest { get; set; }

        public DelegateCommand AbortCommand { get; private set; }

        public DelegateCommand OKCommand { get; private set; }

        public void SetWindowSubTitle(string windowSubTitle)
        {
            WindowTitle = string.Format("Easy BAML - {0}", windowSubTitle);
        }

        protected override void WindowLoaded()
        {
            if (Operation != null)
            {
                AsyncStartOperation(Operation);
            }
        }

        protected void AsyncStartOperation(Action operation)
        {
            ThreadPool.QueueUserWorkItem(
                delegate
                    {
                        IsRunning = true;
                        try
                        {
                            operation();
                        }
                        catch (Exception e)
                        {
                            MessageBox.Show(e.Message, "Easy BAML");
                            //TODO: correctly hanlde error
                        }
                        IsRunning = false;
                    });
        }

        protected override void WindowClosing(CancelEventArgs e)
        {
            if (IsRunning)
            {
                if (OnOperationAbortRequest != null)
                {
                    if (!OnOperationAbortRequest())
                    {
                        e.Cancel = true;
                    }
                }
                else
                {
                    MessageBox.Show("Please wait until operation complete", "Easy BAML");
                    e.Cancel = true;
                }
            }
        }

        private void OKHandler(object obj)
        {
            if (!IsRunning)
            {
                Close(true);
            }
            else
            {
                throw new InvalidOperationException("Trying close operation progress window while job in progress");
            }
        }

        private void AbortHandler(object obj)
        {
            if (IsRunning)
            {
                if (OnOperationAbortRequest != null)
                {
                    if (OnOperationAbortRequest())
                    {
                        Close(false);
                        return;
                    }
                }
            }
        }
    }
}