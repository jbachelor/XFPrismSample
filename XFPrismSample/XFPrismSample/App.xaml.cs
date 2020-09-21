using System.Diagnostics;
using Prism;
using Prism.Ioc;
using XFPrismSample.ViewModels;
using XFPrismSample.Views;
using Xamarin.Essentials.Interfaces;
using Xamarin.Essentials.Implementation;
using Xamarin.Forms;

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

        protected override void RegisterTypes(IContainerRegistry containerRegistry)
        {
            Debug.WriteLine($"**** {this.GetType().Name}.{nameof(RegisterTypes)}");
            containerRegistry.RegisterSingleton<IAppInfo, AppInfoImplementation>();

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
