using System.Diagnostics;
using Prism.Navigation;

namespace XFPrismSample.ViewModels
{
    public class PageCViewModel : ViewModelBase
    {
        public PageCViewModel(INavigationService MyNavSvc) : base(MyNavSvc)
        {
            Title = "View C";
        }

        public override void OnNavigatedTo(INavigationParameters parameters)
        {
            base.OnNavigatedTo(parameters);
            Debug.WriteLine($"**** {this.GetType().Name}.{nameof(OnNavigatedTo)} NavUri: {MyNavSvc.GetNavigationUriPath()}");
        }
    }
}
