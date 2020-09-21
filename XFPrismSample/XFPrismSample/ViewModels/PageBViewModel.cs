using System.Diagnostics;
using Prism.Commands;
using Prism.Navigation;

namespace XFPrismSample.ViewModels
{
    public class PageBViewModel : ViewModelBase
    {
        public DelegateCommand GoToViewCCommand { get; set; }

        public PageBViewModel(INavigationService navigationService) : base(navigationService)
        {
            GoToViewCCommand = new DelegateCommand(OnGoToViewCTapped);
            Title = "View B";
        }

        private void OnGoToViewCTapped()
        {
            Debug.WriteLine($"**** {this.GetType().Name}.{nameof(OnGoToViewCTapped)}");
            _navigationService.NavigateAsync("PageC");
        }
    }
}
