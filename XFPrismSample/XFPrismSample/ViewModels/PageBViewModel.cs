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
            var initialUri = NavigationService.GetNavigationUriPath();
            Debug.WriteLine($"**** {this.GetType().Name}.{nameof(GoToViewC)}: Navigating string=[{navString}]");
            var navResult = await NavigationService.NavigateAsync(navString);
            if (navResult.Success == false)
            {
                Debug.WriteLine($"**** {this.GetType().Name}.{nameof(GoToViewC)}: FAILED NAVIGATION: {navResult.Exception}");
            }
            var finalUri = NavigationService.GetNavigationUriPath();
            Debug.WriteLine($"**** {this.GetType().Name}.{nameof(GoToViewC)}\n\tBeforeNav: {initialUri}\n\tAfter nav: {finalUri}");
        }

        private async void GoToViewCAndRemoveSelfFromNavStackAsync()
        {
            string initialUri = NavigationService.GetNavigationUriPath();

            string navString = $"../{nameof(PageC)}";
            Debug.WriteLine($"**** {this.GetType().Name}.{nameof(GoToViewCAndRemoveSelfFromNavStackAsync)}: Navigating string=[{navString}]\n\tInitial uri stack: {initialUri}");
            var navResult = await NavigationService.NavigateAsync(navString);
            if (navResult.Success == false)
            {
                Debug.WriteLine($"**** {this.GetType().Name}.{nameof(GoToViewCAndRemoveSelfFromNavStackAsync)}: FAILED NAVIGATION: {navResult.Exception}");
            }
            string finalUri = NavigationService.GetNavigationUriPath();
            Debug.WriteLine($"**** {this.GetType().Name}.{nameof(GoToViewCAndRemoveSelfFromNavStackAsync)}\n\tFinal uri stack: {finalUri}");
        }
    }
}
