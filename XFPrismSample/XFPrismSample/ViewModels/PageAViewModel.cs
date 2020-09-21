using Prism.Commands;
using Prism.Mvvm;
using Prism.Navigation;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using XFPrismSample.Views;

namespace XFPrismSample.ViewModels
{
    public class PageAViewModel : ViewModelBase
    {
        public DelegateCommand GoToViewBCommand { get; set; }
        
        public PageAViewModel(INavigationService navigationService)
            : base(navigationService)
        {
            GoToViewBCommand = new DelegateCommand(OnGoToViewBTapped);
            Title = "Main Page";
        }

        private async void OnGoToViewBTapped()
        {
            Debug.WriteLine($"**** {this.GetType().Name}.{nameof(OnGoToViewBTapped)}");
            await _navigationService.NavigateAsync(nameof(PageB));
        }
    }
}
