using Prism.Navigation;
using Xamarin.Forms;

namespace XFPrismSample.Services
{
    public class MyPopupNavigationException : NavigationException
    {
        public const string RootPageHasNotBeenSet = "Popup Pages cannot be set before the Application.MainPage has been set. You must have a valid NavigationStack prior to navigating.";

        public MyPopupNavigationException(string message, Page page)
            : base(message, page)
        {

        }
    }
}
