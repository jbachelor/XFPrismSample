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
using Prism.Plugin.Popups;

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

            var navResult = await NavigationService.NavigateAsync($"{nameof(NavigationPage)}/{nameof(PageA)}");
            if (navResult.Success == false)
            {
                Debug.WriteLine($"**** {this.GetType().Name}.{nameof(OnInitialized)}: FAILED NAVIGATION: {navResult.Exception}");                
            }
        }

        internal static INavigationService SetPage(INavigationService navigationService, Page page)
        {
            Debug.WriteLine($"**** {nameof(App)}.{nameof(SetPage)}: page={page.Title}, navigationService={navigationService}");
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

            containerRegistry.GetContainer().Register<INavigationService, MyPopupNavService>();
            containerRegistry.GetContainer().Register<INavigationService>(
                made: Made.Of(() => SetPage(Arg.Of<INavigationService>(), Arg.Of<Page>())),
                setup: Setup.Decorator);
            containerRegistry.Register<INavigationService, MyPopupNavService>(NavigationServiceName);
            
            containerRegistry.RegisterSingleton<IAppInfo, AppInfoImplementation>();
            containerRegistry.RegisterForNavigation<NavigationPage>();
            containerRegistry.RegisterForNavigation<PageA, PageAViewModel>();
            containerRegistry.RegisterForNavigation<PageB, PageBViewModel>();
            containerRegistry.RegisterForNavigation<PageC, PageCViewModel>();
            containerRegistry.RegisterDialog<ConfirmNavigationDialog, ConfirmNavigationDialogViewModel>();
            
            // containerRegistry.RegisterPopupNavigationService();
            containerRegistry.RegisterPopupDialogService();
        }

        protected override void RegisterTypes(IContainerRegistry containerRegistry)
        {
            Debug.WriteLine($"**** {this.GetType().Name}.{nameof(RegisterTypes)}");
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
