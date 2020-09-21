using Prism.Navigation;

namespace XFPrismSample.ViewModels
{
    public class PageCViewModel : ViewModelBase
    {
        public PageCViewModel(INavigationService navigationService) : base(navigationService)
        {
            Title = "View C";
        }
    }
}
