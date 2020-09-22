using System.Diagnostics;
using Prism.Commands;
using Prism.Navigation;
using XFPrismSample.Views;

namespace XFPrismSample.ViewModels
{
    public class PageBViewModel : ViewModelBase
    {
        public DelegateCommand GoToViewCCommand { get; set; }
        public DelegateCommand GoToViewCAndRemoveSelfFromNavStackCommand { get; set; }

        public PageBViewModel(INavigationService navigationService) : base(navigationService)
        {
            GoToViewCAndRemoveSelfFromNavStackCommand = new DelegateCommand(GoToViewCAndRemoveSelfFromNavStackAsync);
            GoToViewCCommand = new DelegateCommand(GoToViewC);
            Title = "View B";
        }

        private async void GoToViewC()
        {
            string navString = nameof(PageC);
            var initialUri = _navigationService.GetNavigationUriPath();
            Debug.WriteLine($"**** {this.GetType().Name}.{nameof(GoToViewC)}: Navigating string=[{navString}]");
            await _navigationService.NavigateAsync(navString);
            var finalUri = _navigationService.GetNavigationUriPath();
            Debug.WriteLine($"**** {this.GetType().Name}.{nameof(GoToViewC)}\n\tBeforeNav: {initialUri}\n\tAfter nav: {finalUri}");
        }

        private async void GoToViewCAndRemoveSelfFromNavStackAsync()
        {
            string initialUri = _navigationService.GetNavigationUriPath();

            string navString = $"../{nameof(PageC)}";
            Debug.WriteLine($"**** {this.GetType().Name}.{nameof(GoToViewCAndRemoveSelfFromNavStackAsync)}: Navigating string=[{navString}]\n\tInitial uri stack: {initialUri}");
            await _navigationService.NavigateAsync(navString);
            string finalUri = _navigationService.GetNavigationUriPath();
            Debug.WriteLine($"**** {this.GetType().Name}.{nameof(GoToViewCAndRemoveSelfFromNavStackAsync)}\n\tFinal uri stack: {finalUri}");
        }
    }
}
