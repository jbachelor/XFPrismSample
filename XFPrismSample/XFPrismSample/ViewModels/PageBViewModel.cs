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
            Debug.WriteLine($"**** {this.GetType().Name}.{nameof(GoToViewC)}: Navigating string=[{navString}]");
            await _navigationService.NavigateAsync(navString);
        }

        private async void GoToViewCAndRemoveSelfFromNavStackAsync()
        {
            string navString = $"../{nameof(PageC)}";
            Debug.WriteLine($"**** {this.GetType().Name}.{nameof(GoToViewCAndRemoveSelfFromNavStackAsync)}: Navigating string=[{navString}]");
            await _navigationService.NavigateAsync(navString);
        }
    }
}
