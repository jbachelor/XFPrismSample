using System.Diagnostics;
using Prism.Navigation;

namespace XFPrismSample.Services
{
    public static class NavigationParametersExtensions
    {
        public static NavigationMode GetNavigationMode(this INavigationParameters parameters)
        {
            NavigationMode modeToReturn = NavigationMode.Refresh;
            var internalParams = (INavigationParametersInternal) parameters;
            
            if (internalParams.ContainsKey(KnownInternalParameters.NavigationMode))
            {
                modeToReturn = internalParams.GetValue<NavigationMode>(KnownInternalParameters.NavigationMode);
                Debug.WriteLine($"###### {nameof(NavigationParametersExtensions)}.{nameof(GetNavigationMode)}: returning=[{modeToReturn}]");
                return modeToReturn;
            }

            throw new System.ArgumentNullException("NavigationMode is not available");
        }

        internal static INavigationParametersInternal GetNavigationParametersInternal(this INavigationParameters parameters)
        {
            Debug.WriteLine($"###### {nameof(NavigationParametersExtensions)}.{nameof(GetNavigationParametersInternal)}");
            return (INavigationParametersInternal) parameters;
        }
    }
}
