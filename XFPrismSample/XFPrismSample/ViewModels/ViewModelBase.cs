using Prism.Commands;
using Prism.Mvvm;
using Prism.Navigation;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace XFPrismSample.ViewModels
{
    public class ViewModelBase : BindableBase, IInitialize, INavigationAware, IDestructible
    {
        protected INavigationService MyNavSvc { get; private set; }

        private string _title;
        public string Title
        {
            get { return _title; }
            set { SetProperty(ref _title, value); }
        }

        public ViewModelBase(INavigationService MyNavSvc)
        {
            Debug.WriteLine($"**** {this.GetType().Name}: ctor. NavSvc: {MyNavSvc.GetType()}");
            this.MyNavSvc = MyNavSvc;
        }

        public virtual void Initialize(INavigationParameters parameters)
        {
            Debug.WriteLine($"**** {this.GetType().Name}.{nameof(Initialize)}");
        }

        public virtual void OnNavigatedFrom(INavigationParameters parameters)
        {
            Debug.WriteLine($"**** {this.GetType().Name}.{nameof(OnNavigatedFrom)}");
        }

        public virtual void OnNavigatedTo(INavigationParameters parameters)
        {
            Debug.WriteLine($"**** {this.GetType().Name}.{nameof(OnNavigatedTo)}");
        }

        public virtual void Destroy()
        {
            Debug.WriteLine($"**** {this.GetType().Name}.{nameof(Destroy)}");
        }
    }
}
