using Prism.Commands;
using Prism.Mvvm;
using Prism.Navigation;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace XFPrismSample.ViewModels
{
    public class MainPageViewModel : ViewModelBase
    {
        public DelegateCommand GoToViewBCommand { get; set; }
        
        public MainPageViewModel(INavigationService navigationService)
            : base(navigationService)
        {
            GoToViewBCommand = new DelegateCommand(OnGoToViewBTapped);
            Title = "Main Page";
        }

        private async void OnGoToViewBTapped()
        {
            Debug.WriteLine($"**** {this.GetType().Name}.{nameof(OnGoToViewBTapped)}");
            await _navigationService.NavigateAsync("PageB");
        }
    }
}
