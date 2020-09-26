using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Prism.Commands;
using Prism.Navigation;
using Prism.Services.Dialogs;
using XFPrismSample.Views;

namespace XFPrismSample.ViewModels
{
    public class PageBViewModel : ViewModelBase
    {
        private readonly IDialogService _dialogService;
        
        public DelegateCommand GoToViewCCommand { get; set; }
        public DelegateCommand GoToViewCAndRemoveSelfFromNavStackCommand { get; set; }

        public PageBViewModel(INavigationService navigationService, IDialogService dialogService) : base(navigationService)
        {
            _dialogService = dialogService;
            GoToViewCAndRemoveSelfFromNavStackCommand = new DelegateCommand(GoToViewCAndRemoveSelfFromNavStackAsync);
            GoToViewCCommand = new DelegateCommand(GoToViewC);
            Title = "View B";
        }

        private async void GoToViewC()
        {
            string navString = nameof(PageC);
            var initialUri = NavigationService.GetNavigationUriPath();
            Debug.WriteLine($"**** {this.GetType().Name}.{nameof(GoToViewC)}: Navigating string=[{navString}]");
            INavigationResult navResult = await NavigationService.NavigateAsync(navString);
            if (navResult.Success == false)
            {
                Debug.WriteLine($"**** {this.GetType().Name}.{nameof(GoToViewC)}: FAILED NAVIGATION: {navResult.Exception}");
            }

            var finalUri = NavigationService.GetNavigationUriPath();
            Debug.WriteLine($"**** {this.GetType().Name}.{nameof(GoToViewC)}\n\tBeforeNav: {initialUri}\n\tAfter nav: {finalUri}");
        }

        private async void GoToViewCAndRemoveSelfFromNavStackAsync()
        {
            ShowNavConfirmationDialog();
        }

        private void ShowNavConfirmationDialog()
        {
            Debug.WriteLine($"**** {this.GetType().Name}.{nameof(ShowNavConfirmationDialog)}");
            _dialogService.ShowDialog(nameof(ConfirmNavigationDialog), HandleConfirmNavigationDialogResult);
        }

        private async void HandleConfirmNavigationDialogResult(IDialogResult dialogResult)
        {
            Debug.WriteLine($"**** {this.GetType().Name}.{nameof(HandleConfirmNavigationDialogResult)}");
            await DoNavToViewCAndRemoveSelf();
        }

        private async Task DoNavToViewCAndRemoveSelf()
        {
            Debug.WriteLine($"**** {this.GetType().Name}.{nameof(DoNavToViewCAndRemoveSelf)}");
            string initialUri = NavigationService.GetNavigationUriPath();

            string navString = $"../{nameof(PageC)}";
            Debug.WriteLine(
                $"**** {this.GetType().Name}.{nameof(GoToViewCAndRemoveSelfFromNavStackAsync)}: Navigating string=[{navString}]\n\tInitial uri stack: {initialUri}");
            var navResult = await NavigationService.NavigateAsync(navString);
            if (navResult.Success == false)
            {
                Debug.WriteLine(
                    $"**** {this.GetType().Name}.{nameof(GoToViewCAndRemoveSelfFromNavStackAsync)}: FAILED NAVIGATION: {navResult.Exception}");
            }

            string finalUri = NavigationService.GetNavigationUriPath();
            Debug.WriteLine($"**** {this.GetType().Name}.{nameof(GoToViewCAndRemoveSelfFromNavStackAsync)}\n\tFinal uri stack: {finalUri}");
        }
    }
}
