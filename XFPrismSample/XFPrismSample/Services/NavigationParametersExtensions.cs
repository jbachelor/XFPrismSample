using System.Diagnostics;
using Prism.Navigation;

namespace XFPrismSample.Services
{
    public static class NavigationParametersExtensions
    {
        public static NavigationMode GetNavigationMode(this INavigationParameters parameters)
        {
            Debug.WriteLine($"**** {nameof(NavigationParametersExtensions)}.{nameof(GetNavigationMode)}");
            var internalParams = (INavigationParametersInternal)parameters;
            if (internalParams.ContainsKey(KnownInternalParameters.NavigationMode))
                return internalParams.GetValue<NavigationMode>(KnownInternalParameters.NavigationMode);

            throw new System.ArgumentNullException("NavigationMode is not available");
        }

        public static NavigationMode GetNavigationModeDeux(this INavigationParameters parameters)
        {
            Debug.WriteLine($"**** {nameof(NavigationParametersExtensions)}.{nameof(GetNavigationModeDeux)}");
            var internalParams = (INavigationParametersInternal)parameters;
            if (internalParams.ContainsKey(KnownInternalParameters.NavigationMode))
                return internalParams.GetValue<NavigationMode>(KnownInternalParameters.NavigationMode);

            throw new System.ArgumentNullException("NavigationMode is not available");
        }

        internal static INavigationParametersInternal GetNavigationParametersInternal(this INavigationParameters parameters)
        {
            Debug.WriteLine($"**** {nameof(NavigationParametersExtensions)}.{nameof(GetNavigationParametersInternal)}");
            return (INavigationParametersInternal)parameters;
        }
    }
}
