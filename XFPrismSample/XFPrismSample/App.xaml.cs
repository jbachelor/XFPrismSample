using System.Diagnostics;
using Prism;
using Prism.Ioc;
using Prism.Navigation;
using XFPrismSample.ViewModels;
using XFPrismSample.Views;
using Xamarin.Essentials.Interfaces;
using Xamarin.Essentials.Implementation;
using Xamarin.Forms;
using XFPrismSample.Services;
using Prism.DryIoc;
using DryIoc;
using Prism.Common;

namespace XFPrismSample
{
    public partial class App
    {
        public App(IPlatformInitializer initializer)
            : base(initializer)
        {
            Debug.WriteLine($"**** {this.GetType().Name}: ctor");
        }

        protected override async void OnInitialized()
        {
            Debug.WriteLine($"**** {this.GetType().Name}.{nameof(OnInitialized)}");
            InitializeComponent();

            await NavigationService.NavigateAsync($"{nameof(NavigationPage)}/{nameof(PageA)}");
        }

        internal static INavigationService SetPage(INavigationService navigationService, Page page)
        {
            Debug.WriteLine($"**** {nameof(App)}.{nameof(SetPage)}");
            if (navigationService is IPageAware pageAware)
            {
                pageAware.Page = page;
            }

            return navigationService;
        }
        
        protected override void RegisterRequiredTypes(IContainerRegistry containerRegistry)
        {
            Debug.WriteLine($"**** {this.GetType().Name}.{nameof(RegisterRequiredTypes)}");
            base.RegisterRequiredTypes(containerRegistry);

            containerRegistry.GetContainer().Register<INavigationService, MyNavService>();
            containerRegistry.GetContainer().Register<INavigationService>(
                made: Made.Of(() => SetPage(Arg.Of<INavigationService>(), Arg.Of<Page>())),
                setup: Setup.Decorator);
        }

        protected override void RegisterTypes(IContainerRegistry containerRegistry)
        {
            Debug.WriteLine($"**** {this.GetType().Name}.{nameof(RegisterTypes)}");
            containerRegistry.RegisterSingleton<IAppInfo, AppInfoImplementation>();
            containerRegistry.Register<INavigationService, MyNavService>("MyNavSvc");
            containerRegistry.Register<INavigationService, PageNavigationService>(NavigationServiceName);

            containerRegistry.RegisterForNavigation<NavigationPage>();
            containerRegistry.RegisterForNavigation<PageA, PageAViewModel>();
            containerRegistry.RegisterForNavigation<PageB, PageBViewModel>();
            containerRegistry.RegisterForNavigation<PageC, PageCViewModel>();
        }

        protected override void OnStart()
        {
            base.OnStart();
            Debug.WriteLine($"**** {this.GetType().Name}.{nameof(OnStart)}");
        }

        protected override void OnSleep()
        {
            base.OnSleep();
            Debug.WriteLine($"**** {this.GetType().Name}.{nameof(OnSleep)}");
        }

        protected override void OnResume()
        {
            base.OnResume();
            Debug.WriteLine($"**** {this.GetType().Name}.{nameof(OnResume)}");
        }
    }
}
