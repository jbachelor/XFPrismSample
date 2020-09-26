using System;
using System.Diagnostics;
using Prism.Commands;
using Prism.Navigation;
using Prism.Services.Dialogs;

namespace XFPrismSample.ViewModels
{
    public class ConfirmNavigationDialogViewModel : ViewModelBase, IDialogAware
    {
        public  DelegateCommand DoNavigationCommand { get; set; }
        
        public ConfirmNavigationDialogViewModel(INavigationService navigationService) : base(navigationService)
        {
            Debug.WriteLine($"**** {this.GetType().Name}: ctor\n\tnavigationService=[{navigationService}]");
            DoNavigationCommand = new DelegateCommand(OnButtonTapped);
        }

        private void OnButtonTapped()
        {
            Debug.WriteLine($"**** {this.GetType().Name}.{nameof(OnButtonTapped)}");
            RequestClose?.Invoke(null);
        }
        
        public bool CanCloseDialog()
        {
            Debug.WriteLine($"**** {this.GetType().Name}.{nameof(CanCloseDialog)}");
            return true;
        }

        public void OnDialogClosed()
        {
            Debug.WriteLine($"**** {this.GetType().Name}.{nameof(OnDialogClosed)}");
        }

        public void OnDialogOpened(IDialogParameters parameters)
        {
            Debug.WriteLine($"**** {this.GetType().Name}.{nameof(OnDialogOpened)}");
        }

        public event Action<IDialogParameters> RequestClose;
    }
}
