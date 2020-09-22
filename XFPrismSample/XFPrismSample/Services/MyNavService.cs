using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Prism.AppModel;
using Prism.Behaviors;
using Prism.Common;
using Prism.Ioc;
using Prism.Logging;
using Prism.Mvvm;
using Prism.Navigation;
using Xamarin.Forms;

namespace XFPrismSample.Services
{
    public class MyNavService : INavigationService, IPlatformNavigationService, IPageAware
    {
        internal const string RemovePageRelativePath = "../";
        internal const string RemovePageInstruction = "__RemovePage/";
        internal const string RemovePageSegment = "__RemovePage";

        //not sure I like this static property, think about this a little more
        protected internal static PageNavigationSource NavigationSource { get; protected set; } = PageNavigationSource.Device;

        private readonly IContainerProvider _container;
        protected readonly IApplicationProvider _applicationProvider;
        protected readonly IPageBehaviorFactory _pageBehaviorFactory;
        protected readonly ILoggerFacade _logger;

        protected Page _page;
        Page IPageAware.Page
        {
            get { return _page; }
            set { _page = value; }
        }

        public MyNavService(IContainerExtension container, IApplicationProvider applicationProvider, IPageBehaviorFactory pageBehaviorFactory, ILoggerFacade logger)
        {
            _container = container;
            _applicationProvider = applicationProvider;
            _pageBehaviorFactory = pageBehaviorFactory;
            _logger = logger;
        }

        /// <summary>
        /// Navigates to the most recent entry in the back navigation history by popping the calling Page off the navigation stack.
        /// </summary>
        /// <returns>If <c>true</c> a go back operation was successful. If <c>false</c> the go back operation failed.</returns>
        public virtual Task<INavigationResult> GoBackAsync()
        {
            return GoBackAsync(null);
        }

        /// <summary>
        /// Navigates to the most recent entry in the back navigation history by popping the calling Page off the navigation stack.
        /// </summary>
        /// <param name="parameters">The navigation parameters</param>
        /// <returns>If <c>true</c> a go back operation was successful. If <c>false</c> the go back operation failed.</returns>
        public virtual Task<INavigationResult> GoBackAsync(INavigationParameters parameters)
        {
            return GoBackInternal(parameters, null, true);
        }

        Task<INavigationResult> IPlatformNavigationService.GoBackAsync(INavigationParameters parameters, bool? useModalNavigation, bool animated)
        {
            return GoBackInternal(parameters, useModalNavigation, animated);
        }

        /// <summary>
        /// Navigates to the most recent entry in the back navigation history by popping the calling Page off the navigation stack.
        /// </summary>
        /// <param name="parameters">The navigation parameters</param>
        /// <param name="useModalNavigation">If <c>true</c> uses PopModalAsync, if <c>false</c> uses PopAsync</param>
        /// <param name="animated">If <c>true</c> the transition is animated, if <c>false</c> there is no animation on transition.</param>
        /// <returns>If <c>true</c> a go back operation was successful. If <c>false</c> the go back operation failed.</returns>
        protected async virtual Task<INavigationResult> GoBackInternal(INavigationParameters parameters, bool? useModalNavigation, bool animated)
        {
            var result = new NavigationResult();
            Page page = null;
            try
            {
                NavigationSource = PageNavigationSource.NavigationService;

                page = GetCurrentPage();
                var segmentParameters = UriParsingHelper.GetSegmentParameters(null, parameters);
                segmentParameters.GetNavigationParametersInternal().Add(KnownInternalParameters.NavigationMode, NavigationMode.Back);

                var canNavigate = await PageUtilities.CanNavigateAsync(page, segmentParameters);
                if (!canNavigate)
                {
                    result.Exception = new NavigationException(NavigationException.IConfirmNavigationReturnedFalse, page);
                    return result;
                }

                bool useModalForDoPop = UseModalGoBack(page, useModalNavigation);
                Page previousPage = PageUtilities.GetOnNavigatedToTarget(page, _applicationProvider.MainPage, useModalForDoPop);

                var poppedPage = await DoPop(page.Navigation, useModalForDoPop, animated);
                if (poppedPage != null)
                {
                    PageUtilities.OnNavigatedFrom(page, segmentParameters);
                    PageUtilities.OnNavigatedTo(previousPage, segmentParameters);
                    PageUtilities.DestroyPage(poppedPage);

                    result.Success = true;
                    return result;
                }
            }
            catch (Exception ex)
            {
                _logger.Log(ex.ToString(), Category.Exception, Priority.High);
                result.Exception = ex;
                return result;
            }
            finally
            {
                NavigationSource = PageNavigationSource.Device;
            }

            result.Exception = GetGoBackException(page, _applicationProvider.MainPage);
            return result;
        }

        private static Exception GetGoBackException(Page currentPage, Page mainPage)
        {
            if(IsMainPage(currentPage, mainPage))
            {
                return new NavigationException(NavigationException.CannotPopApplicationMainPage, currentPage);
            }
            else if((currentPage is NavigationPage navPage && IsOnNavigationPageRoot(navPage)) ||
                (currentPage.Parent is NavigationPage navParent && IsOnNavigationPageRoot(navParent)))
            {
                return new NavigationException(NavigationException.CannotGoBackFromRoot, currentPage);
            }

            return new NavigationException(NavigationException.UnknownException, currentPage);
        }

        private static bool IsOnNavigationPageRoot(NavigationPage navigationPage) =>
            navigationPage.CurrentPage == navigationPage.RootPage;

        private static bool IsMainPage(Page currentPage, Page mainPage)
        {
            if (currentPage == mainPage)
            {
                return true;
            }
            else if(mainPage is MasterDetailPage mdp && mdp.Detail == currentPage)
            {
                return true;
            }
            else if (currentPage.Parent is TabbedPage tabbed && mainPage == tabbed)
            {
                return true;
            }
            else if (currentPage.Parent is CarouselPage carousel && mainPage == carousel)
            {
                return true;
            }
            else if(currentPage.Parent is NavigationPage navPage && navPage.CurrentPage == navPage.RootPage)
            {
                return IsMainPage(navPage, mainPage);
            }

            return false;
        }

        Task<INavigationResult> IPlatformNavigationService.GoBackToRootAsync(INavigationParameters parameters)
        {
            return GoBackToRootInternal(parameters);
        }

        /// <summary>
        /// When navigating inside a NavigationPage: Pops all but the root Page off the navigation stack
        /// </summary>
        /// <param name="navigationService">The INavigatinService instance</param>
        /// <param name="parameters">The navigation parameters</param>
        /// <remarks>Only works when called from a View within a NavigationPage</remarks>
        protected async virtual Task<INavigationResult> GoBackToRootInternal(INavigationParameters parameters)
        {
            var result = new NavigationResult();
            Page page = null;
            try
            {
                if (parameters == null)
                    parameters = new NavigationParameters();

                parameters.GetNavigationParametersInternal().Add(KnownInternalParameters.NavigationMode, NavigationMode.Back);

                page = GetCurrentPage();
                var canNavigate = await PageUtilities.CanNavigateAsync(page, parameters);
                if (!canNavigate)
                {
                    result.Exception = new NavigationException(NavigationException.IConfirmNavigationReturnedFalse, page);
                    return result;
                }

                List<Page> pagesToDestroy = page.Navigation.NavigationStack.ToList(); // get all pages to destroy
                pagesToDestroy.Reverse(); // destroy them in reverse order
                var root = pagesToDestroy.Last();
                pagesToDestroy.Remove(root); //don't destroy the root page

                await page.Navigation.PopToRootAsync();

                foreach (var destroyPage in pagesToDestroy)
                {
                    PageUtilities.OnNavigatedFrom(destroyPage, parameters);
                    PageUtilities.DestroyPage(destroyPage);
                }

                PageUtilities.OnNavigatedTo(root, parameters);

                result.Success = true;
                return result;
            }
            catch (Exception ex)
            {
                result.Exception = ex;
                return result;
            }
        }

        /// <summary>
        /// Initiates navigation to the target specified by the <paramref name="name"/>.
        /// </summary>
        /// <param name="name">The name of the target to navigate to.</param>
        public virtual Task<INavigationResult> NavigateAsync(string name)
        {
            return NavigateAsync(name, null);
        }

        /// <summary>
        /// Initiates navigation to the target specified by the <paramref name="name"/>.
        /// </summary>
        /// <param name="name">The name of the target to navigate to.</param>
        /// <param name="parameters">The navigation parameters</param>
        public virtual Task<INavigationResult> NavigateAsync(string name, INavigationParameters parameters)
        {
            return NavigateInternal(name, parameters, null, true);
        }

        Task<INavigationResult> IPlatformNavigationService.NavigateAsync(string name, INavigationParameters parameters, bool? useModalNavigation, bool animated)
        {
            return NavigateInternal(name, parameters, useModalNavigation, animated);
        }

        /// <summary>
        /// Initiates navigation to the target specified by the <paramref name="name"/>.
        /// </summary>
        /// <param name="name">The name of the target to navigate to.</param>
        /// <param name="parameters">The navigation parameters</param>
        /// <param name="useModalNavigation">If <c>true</c> uses PopModalAsync, if <c>false</c> uses PopAsync</param>
        /// <param name="animated">If <c>true</c> the transition is animated, if <c>false</c> there is no animation on transition.</param>
        protected virtual Task<INavigationResult> NavigateInternal(string name, INavigationParameters parameters, bool? useModalNavigation, bool animated)
        {
            if (name.StartsWith(RemovePageRelativePath))
                name = name.Replace(RemovePageRelativePath, RemovePageInstruction);

            return NavigateInternal(UriParsingHelper.Parse(name), parameters, useModalNavigation, animated);
        }

        /// <summary>
        /// Initiates navigation to the target specified by the <paramref name="uri"/>.
        /// </summary>
        /// <param name="uri">The Uri to navigate to</param>
        /// <remarks>Navigation parameters can be provided in the Uri and by using the <paramref name="parameters"/>.</remarks>
        /// <example>
        /// Navigate(new Uri("MainPage?id=3&name=brian", UriKind.RelativeSource), parameters);
        /// </example>
        public virtual Task<INavigationResult> NavigateAsync(Uri uri)
        {
            return NavigateAsync(uri, null);
        }

        /// <summary>
        /// Initiates navigation to the target specified by the <paramref name="uri"/>.
        /// </summary>
        /// <param name="uri">The Uri to navigate to</param>
        /// <param name="parameters">The navigation parameters</param>
        /// <remarks>Navigation parameters can be provided in the Uri and by using the <paramref name="parameters"/>.</remarks>
        /// <example>
        /// Navigate(new Uri("MainPage?id=3&name=brian", UriKind.RelativeSource), parameters);
        /// </example>
        public virtual Task<INavigationResult> NavigateAsync(Uri uri, INavigationParameters parameters)
        {
            return NavigateInternal(uri, parameters, null, true);
        }

        Task<INavigationResult> IPlatformNavigationService.NavigateAsync(Uri uri, INavigationParameters parameters, bool? useModalNavigation, bool animated)
        {
            return NavigateInternal(uri, parameters, useModalNavigation, animated);
        }

        /// <summary>
        /// Initiates navigation to the target specified by the <paramref name="uri"/>.
        /// </summary>
        /// <param name="uri">The Uri to navigate to</param>
        /// <param name="parameters">The navigation parameters</param>
        /// <param name="useModalNavigation">If <c>true</c> uses PopModalAsync, if <c>false</c> uses PopAsync</param>
        /// <param name="animated">If <c>true</c> the transition is animated, if <c>false</c> there is no animation on transition.</param>
        /// <remarks>Navigation parameters can be provided in the Uri and by using the <paramref name="parameters"/>.</remarks>
        /// <example>
        /// Navigate(new Uri("MainPage?id=3&name=brian", UriKind.RelativeSource), parameters);
        /// </example>
        protected async virtual Task<INavigationResult> NavigateInternal(Uri uri, INavigationParameters parameters, bool? useModalNavigation, bool animated)
        {
            var result = new NavigationResult();
            try
            {
                NavigationSource = PageNavigationSource.NavigationService;

                var navigationSegments = UriParsingHelper.GetUriSegments(uri);

                if (uri.IsAbsoluteUri)
                {
                    await ProcessNavigationForAbsoulteUri(navigationSegments, parameters, useModalNavigation, animated);
                    result.Success = true;
                    return result;
                }
                else
                {
                    await ProcessNavigation(GetCurrentPage(), navigationSegments, parameters, useModalNavigation, animated);
                    result.Success = true;
                    return result;
                }

            }
            catch (Exception ex)
            {
                _logger.Log(ex.ToString(), Category.Exception, Priority.High);
                result.Exception = ex;
                return result;
            }
            finally
            {
                NavigationSource = PageNavigationSource.Device;
            }
        }

        protected virtual async Task ProcessNavigation(Page currentPage, Queue<string> segments, INavigationParameters parameters, bool? useModalNavigation, bool animated)
        {
            if (segments.Count == 0)
                return;

            var nextSegment = segments.Dequeue();

            var pageParameters = UriParsingHelper.GetSegmentParameters(nextSegment);
            if (pageParameters.ContainsKey(KnownNavigationParameters.UseModalNavigation))
                useModalNavigation = pageParameters.GetValue<bool>(KnownNavigationParameters.UseModalNavigation);

            if (nextSegment == RemovePageSegment)
            {
                await ProcessNavigationForRemovePageSegments(currentPage, nextSegment, segments, parameters, useModalNavigation, animated);
                return;
            }

            if (currentPage == null)
            {
                await ProcessNavigationForRootPage(nextSegment, segments, parameters, useModalNavigation, animated);
                return;
            }

            if (currentPage is ContentPage)
            {
                await ProcessNavigationForContentPage(currentPage, nextSegment, segments, parameters, useModalNavigation, animated);
            }
            else if (currentPage is NavigationPage)
            {
                await ProcessNavigationForNavigationPage((NavigationPage)currentPage, nextSegment, segments, parameters, useModalNavigation, animated);
            }
            else if (currentPage is TabbedPage)
            {
                await ProcessNavigationForTabbedPage((TabbedPage)currentPage, nextSegment, segments, parameters, useModalNavigation, animated);
            }
            else if (currentPage is CarouselPage)
            {
                await ProcessNavigationForCarouselPage((CarouselPage)currentPage, nextSegment, segments, parameters, useModalNavigation, animated);
            }
            else if (currentPage is MasterDetailPage)
            {
                await ProcessNavigationForMasterDetailPage((MasterDetailPage)currentPage, nextSegment, segments, parameters, useModalNavigation, animated);
            }
        }

        protected virtual Task ProcessNavigationForRemovePageSegments(Page currentPage, string nextSegment, Queue<string> segments, INavigationParameters parameters, bool? useModalNavigation, bool animated)
        {
            if (!PageUtilities.HasDirectNavigationPageParent(currentPage))
                throw new NavigationException(NavigationException.RelativeNavigationRequiresNavigationPage, currentPage);

            if (CanRemoveAndPush(segments))
                return RemoveAndPush(currentPage, nextSegment, segments, parameters, useModalNavigation, animated);
            else
                return RemoveAndGoBack(currentPage, nextSegment, segments, parameters, useModalNavigation, animated);
        }

        bool CanRemoveAndPush(Queue<string> segments)
        {
            if (segments.All(x => x == RemovePageSegment))
                return false;
            else
                return true;
        }

        Task RemoveAndGoBack(Page currentPage, string nextSegment, Queue<string> segments, INavigationParameters parameters, bool? useModalNavigation, bool animated)
        {
            List<Page> pagesToRemove = new List<Page>();

            var currentPageIndex = currentPage.Navigation.NavigationStack.Count;
            if (currentPage.Navigation.NavigationStack.Count > 0)
                currentPageIndex = currentPage.Navigation.NavigationStack.Count - 1;

            while (segments.Count != 0)
            {
                currentPageIndex -= 1;
                pagesToRemove.Add(currentPage.Navigation.NavigationStack[currentPageIndex]);
                nextSegment = segments.Dequeue();
            }

            RemovePagesFromNavigationPage(currentPage, pagesToRemove);

            return GoBackAsync(parameters);
        }

        async Task RemoveAndPush(Page currentPage, string nextSegment, Queue<string> segments, INavigationParameters parameters, bool? useModalNavigation, bool animated)
        {
            var pagesToRemove = new List<Page>
            {
                currentPage
            };

            var currentPageIndex = currentPage.Navigation.NavigationStack.Count;
            if (currentPage.Navigation.NavigationStack.Count > 0)
                currentPageIndex = currentPage.Navigation.NavigationStack.Count - 1;

            while (segments.Peek() == RemovePageSegment)
            {
                currentPageIndex -= 1;
                pagesToRemove.Add(currentPage.Navigation.NavigationStack[currentPageIndex]);
                nextSegment = segments.Dequeue();
            }

            await ProcessNavigation(currentPage, segments, parameters, useModalNavigation, animated);

            RemovePagesFromNavigationPage(currentPage, pagesToRemove);
        }

        private static void RemovePagesFromNavigationPage(Page currentPage, List<Page> pagesToRemove)
        {
            var navigationPage = (NavigationPage)currentPage.Parent;
            foreach (var page in pagesToRemove)
            {
                navigationPage.Navigation.RemovePage(page);
                PageUtilities.DestroyPage(page);
            }
        }

        protected virtual Task ProcessNavigationForAbsoulteUri(Queue<string> segments, INavigationParameters parameters, bool? useModalNavigation, bool animated)
        {
            return ProcessNavigation(null, segments, parameters, useModalNavigation, animated);
        }

        protected virtual async Task ProcessNavigationForRootPage(string nextSegment, Queue<string> segments, INavigationParameters parameters, bool? useModalNavigation, bool animated)
        {
            var nextPage = CreatePageFromSegment(nextSegment);

            await ProcessNavigation(nextPage, segments, parameters, useModalNavigation, animated);

            var currentPage = _applicationProvider.MainPage;
            var modalStack = currentPage?.Navigation.ModalStack.ToList();
            await DoNavigateAction(GetCurrentPage(), nextSegment, nextPage, parameters, async () =>
            {
                await DoPush(null, nextPage, useModalNavigation, animated);
            });
            if (currentPage != null)
            {
                PageUtilities.DestroyWithModalStack(currentPage, modalStack);
            }
        }

        protected virtual async Task ProcessNavigationForContentPage(Page currentPage, string nextSegment, Queue<string> segments, INavigationParameters parameters, bool? useModalNavigation, bool animated)
        {
            var nextPageType = PageNavigationRegistry.GetPageType(UriParsingHelper.GetSegmentName(nextSegment));
            bool useReverse = UseReverseNavigation(currentPage, nextPageType) && !(useModalNavigation.HasValue && useModalNavigation.Value);
            if (!useReverse)
            {
                var nextPage = CreatePageFromSegment(nextSegment);

                await ProcessNavigation(nextPage, segments, parameters, useModalNavigation, animated);

                await DoNavigateAction(currentPage, nextSegment, nextPage, parameters, async () =>
                {
                    await DoPush(currentPage, nextPage, useModalNavigation, animated);
                });
            }
            else
            {
                await UseReverseNavigation(currentPage, nextSegment, segments, parameters, useModalNavigation, animated);
            }
        }

        protected virtual async Task ProcessNavigationForNavigationPage(NavigationPage currentPage, string nextSegment, Queue<string> segments, INavigationParameters parameters, bool? useModalNavigation, bool animated)
        {
            if (currentPage.Navigation.NavigationStack.Count == 0)
            {
                await UseReverseNavigation(currentPage, nextSegment, segments, parameters, false, animated);
                return;
            }

            var clearNavigationStack = GetClearNavigationPageNavigationStack(currentPage);
            var isEmptyOfNavigationStack = currentPage.Navigation.NavigationStack.Count == 0;

            List<Page> destroyPages;
            if (clearNavigationStack && !isEmptyOfNavigationStack)
            {
                destroyPages = currentPage.Navigation.NavigationStack.ToList();
                destroyPages.Reverse();

                await currentPage.Navigation.PopToRootAsync(false);
            }
            else
            {
                destroyPages = new List<Page>();
            }

            var topPage = currentPage.Navigation.NavigationStack.LastOrDefault();
            var nextPageType = PageNavigationRegistry.GetPageType(UriParsingHelper.GetSegmentName(nextSegment));
            if (topPage?.GetType() == nextPageType)
            {
                if (clearNavigationStack)
                    destroyPages.Remove(destroyPages.Last());

                if (segments.Count > 0)
                    await UseReverseNavigation(topPage, segments.Dequeue(), segments, parameters, false, animated);

                await DoNavigateAction(topPage, nextSegment, topPage, parameters, onNavigationActionCompleted: (p) =>
                {
                    if (nextSegment.Contains(KnownNavigationParameters.SelectedTab))
                    {
                        var segmentParams = UriParsingHelper.GetSegmentParameters(nextSegment);
                        SelectPageTab(topPage, segmentParams);
                    }
                });
            }
            else
            {
                await UseReverseNavigation(currentPage, nextSegment, segments, parameters, false, animated);

                if (clearNavigationStack && !isEmptyOfNavigationStack)
                    currentPage.Navigation.RemovePage(topPage);
            }

            foreach (var destroyPage in destroyPages)
            {
                PageUtilities.DestroyPage(destroyPage);
            }
        }

        protected virtual async Task ProcessNavigationForTabbedPage(TabbedPage currentPage, string nextSegment, Queue<string> segments, INavigationParameters parameters, bool? useModalNavigation, bool animated)
        {
            var nextPage = CreatePageFromSegment(nextSegment);
            await ProcessNavigation(nextPage, segments, parameters, useModalNavigation, animated);
            await DoNavigateAction(currentPage, nextSegment, nextPage, parameters, async () =>
            {
                await DoPush(currentPage, nextPage, useModalNavigation, animated);
            });
        }

        protected virtual async Task ProcessNavigationForCarouselPage(CarouselPage currentPage, string nextSegment, Queue<string> segments, INavigationParameters parameters, bool? useModalNavigation, bool animated)
        {
            var nextPage = CreatePageFromSegment(nextSegment);
            await ProcessNavigation(nextPage, segments, parameters, useModalNavigation, animated);
            await DoNavigateAction(currentPage, nextSegment, nextPage, parameters, async () =>
            {
                await DoPush(currentPage, nextPage, useModalNavigation, animated);
            });
        }

        protected virtual async Task ProcessNavigationForMasterDetailPage(MasterDetailPage currentPage, string nextSegment, Queue<string> segments, INavigationParameters parameters, bool? useModalNavigation, bool animated)
        {
            bool isPresented = GetMasterDetailPageIsPresented(currentPage);

            var detail = currentPage.Detail;
            if (detail == null)
            {
                var newDetail = CreatePageFromSegment(nextSegment);
                await ProcessNavigation(newDetail, segments, parameters, useModalNavigation, animated);
                await DoNavigateAction(null, nextSegment, newDetail, parameters, onNavigationActionCompleted: (p) =>
                {
                    currentPage.IsPresented = isPresented;
                    currentPage.Detail = newDetail;
                });
                return;
            }

            if (useModalNavigation.HasValue && useModalNavigation.Value)
            {
                var nextPage = CreatePageFromSegment(nextSegment);
                await ProcessNavigation(nextPage, segments, parameters, useModalNavigation, animated);
                await DoNavigateAction(currentPage, nextSegment, nextPage, parameters, async () =>
                {
                    currentPage.IsPresented = isPresented;
                    await DoPush(currentPage, nextPage, true, animated);
                });
                return;
            }

            var nextSegmentType = PageNavigationRegistry.GetPageType(UriParsingHelper.GetSegmentName(nextSegment));

            //we must recreate the NavigationPage everytime or the transitions on iOS will not work properly, unless we meet the two scenarios below
            bool detailIsNavPage = false;
            bool reuseNavPage = false;
            if (detail is NavigationPage navPage)
            {
                detailIsNavPage = true;

                //we only care if we the next segment is also a NavigationPage.
                if (PageUtilities.IsSameOrSubclassOf<NavigationPage>(nextSegmentType))
                {
                    //first we check to see if we are being forced to reuse the NavPage by checking the interface
                    reuseNavPage = !GetClearNavigationPageNavigationStack(navPage);

                    if (!reuseNavPage)
                    {
                        //if we weren't forced to reuse the NavPage, then let's check the NavPage.CurrentPage against the next segment type as we don't want to recreate the entire nav stack
                        //just in case the user is trying to navigate to the same page which may be nested in a NavPage
                        var nextPageType = PageNavigationRegistry.GetPageType(UriParsingHelper.GetSegmentName(segments.Peek()));
                        var currentPageType = navPage.CurrentPage.GetType();
                        if (nextPageType == currentPageType)
                        {
                            reuseNavPage = true;
                        }
                    }
                }
            }

            if ((detailIsNavPage && reuseNavPage) || (!detailIsNavPage && detail.GetType() == nextSegmentType))
            {
                await ProcessNavigation(detail, segments, parameters, useModalNavigation, animated);
                await DoNavigateAction(null, nextSegment, detail, parameters, onNavigationActionCompleted: (p) =>
                 {
                     if (detail is TabbedPage && nextSegment.Contains(KnownNavigationParameters.SelectedTab))
                     {
                         var segmentParams = UriParsingHelper.GetSegmentParameters(nextSegment);
                         SelectPageTab(detail, segmentParams);
                     }

                     currentPage.IsPresented = isPresented;
                 });
                return;
            }
            else
            {
                var newDetail = CreatePageFromSegment(nextSegment);
                await ProcessNavigation(newDetail, segments, parameters, newDetail is NavigationPage ? false : true, animated);
                await DoNavigateAction(detail, nextSegment, newDetail, parameters, onNavigationActionCompleted: (p) =>
                {
                    if (detailIsNavPage)
                        OnNavigatedFrom(((NavigationPage)detail).CurrentPage, p);

                    currentPage.IsPresented = isPresented;
                    currentPage.Detail = newDetail;
                    PageUtilities.DestroyPage(detail);
                });
                return;
            }
        }

        protected static bool GetMasterDetailPageIsPresented(MasterDetailPage page)
        {
            if (page is IMasterDetailPageOptions iMasterDetailPage)
                return iMasterDetailPage.IsPresentedAfterNavigation;

            if (page.BindingContext is IMasterDetailPageOptions iMasterDetailPageBindingContext)
                return iMasterDetailPageBindingContext.IsPresentedAfterNavigation;

            return false;
        }

        protected static bool GetClearNavigationPageNavigationStack(NavigationPage page)
        {
            if (page is INavigationPageOptions iNavigationPage)
                return iNavigationPage.ClearNavigationStackOnNavigation;

            if (page.BindingContext is INavigationPageOptions iNavigationPageBindingContext)
                return iNavigationPageBindingContext.ClearNavigationStackOnNavigation;

            return true;
        }

        protected static async Task DoNavigateAction(Page fromPage, string toSegment, Page toPage, INavigationParameters parameters, Func<Task> navigationAction = null, Action<INavigationParameters> onNavigationActionCompleted = null)
        {
            var segmentParameters = UriParsingHelper.GetSegmentParameters(toSegment, parameters);
            segmentParameters.GetNavigationParametersInternal().Add(KnownInternalParameters.NavigationMode, NavigationMode.New);

            var canNavigate = await PageUtilities.CanNavigateAsync(fromPage, segmentParameters);
            if (!canNavigate)
            {
                throw new NavigationException(NavigationException.IConfirmNavigationReturnedFalse, toPage);
            }

            await OnInitializedAsync(toPage, segmentParameters);

            if (navigationAction != null)
                await navigationAction();

            OnNavigatedFrom(fromPage, segmentParameters);

            onNavigationActionCompleted?.Invoke(segmentParameters);

            OnNavigatedTo(toPage, segmentParameters);
        }

        static async Task OnInitializedAsync(Page toPage, INavigationParameters parameters)
        {
            await PageUtilities.OnInitializedAsync(toPage, parameters);

            if (toPage is TabbedPage tabbedPage)
            {
                foreach (var child in tabbedPage.Children)
                {
                    if (child is NavigationPage navigationPage)
                    {
                        await PageUtilities.OnInitializedAsync(navigationPage.CurrentPage, parameters);
                    }
                    else
                    {
                        await PageUtilities.OnInitializedAsync(child, parameters);
                    }
                }
            }
            else if (toPage is CarouselPage carouselPage)
            {
                foreach (var child in carouselPage.Children)
                {
                    await PageUtilities.OnInitializedAsync(child, parameters);
                }
            }
        }

        private static void OnNavigatedTo(Page toPage, INavigationParameters parameters)
        {
            PageUtilities.OnNavigatedTo(toPage, parameters);

            if (toPage is TabbedPage tabbedPage)
            {
                if (tabbedPage.CurrentPage is NavigationPage navigationPage)
                {
                    PageUtilities.OnNavigatedTo(navigationPage.CurrentPage, parameters);
                }
                else if (tabbedPage.BindingContext != tabbedPage.CurrentPage.BindingContext)
                {
                    PageUtilities.OnNavigatedTo(tabbedPage.CurrentPage, parameters);
                }
            }
            else if (toPage is CarouselPage carouselPage)
            {
                PageUtilities.OnNavigatedTo(carouselPage.CurrentPage, parameters);
            }
        }

        private static void OnNavigatedFrom(Page fromPage, INavigationParameters parameters)
        {
            PageUtilities.OnNavigatedFrom(fromPage, parameters);

            if (fromPage is TabbedPage tabbedPage)
            {
                if (tabbedPage.CurrentPage is NavigationPage navigationPage)
                {
                    PageUtilities.OnNavigatedFrom(navigationPage.CurrentPage, parameters);
                }
                else if (tabbedPage.BindingContext != tabbedPage.CurrentPage.BindingContext)
                {
                    PageUtilities.OnNavigatedFrom(tabbedPage.CurrentPage, parameters);
                }
            }
            else if (fromPage is CarouselPage carouselPage)
            {
                PageUtilities.OnNavigatedFrom(carouselPage.CurrentPage, parameters);
            }
        }

        protected virtual Page CreatePage(string segmentName)
        {
            try
            {
                return _container.Resolve<object>(segmentName) as Page;
            }
            catch (Exception ex)
            {
                if (((IContainerRegistry)_container).IsRegistered<object>(segmentName))
                    throw new NavigationException(NavigationException.ErrorCreatingPage, _page, ex);

                throw new NavigationException(NavigationException.NoPageIsRegistered, _page, ex);
            }
        }

        protected virtual Page CreatePageFromSegment(string segment)
        {
            string segmentName = null;
            try
            {
                segmentName = UriParsingHelper.GetSegmentName(segment);
                var page = CreatePage(segmentName);
                if (page == null)
                {
                    var innerException = new NullReferenceException(string.Format("{0} could not be created. Please make sure you have registered {0} for navigation.", segmentName));
                    throw new NavigationException(NavigationException.NoPageIsRegistered, _page, innerException);
                }

                PageUtilities.SetAutowireViewModelOnPage(page);
                _pageBehaviorFactory.ApplyPageBehaviors(page);
                ConfigurePages(page, segment);

                return page;
            }
            catch(NavigationException)
            {
                throw;
            }
            catch (Exception e)
            {
                _logger.Log(e.ToString(), Category.Exception, Priority.High);
                throw;
            }
        }

        void ConfigurePages(Page page, string segment)
        {
            if (page is TabbedPage)
            {
                ConfigureTabbedPage((TabbedPage)page, segment);
            }
            else if (page is CarouselPage)
            {
                ConfigureCarouselPage((CarouselPage)page, segment);
            }
        }

        void ConfigureTabbedPage(TabbedPage tabbedPage, string segment)
        {
            foreach (var child in tabbedPage.Children)
            {
                PageUtilities.SetAutowireViewModelOnPage(child);
                _pageBehaviorFactory.ApplyPageBehaviors(child);
                if (child is NavigationPage navPage)
                {
                    PageUtilities.SetAutowireViewModelOnPage(navPage.CurrentPage);
                    _pageBehaviorFactory.ApplyPageBehaviors(navPage.CurrentPage);
                }
            }

            var parameters = UriParsingHelper.GetSegmentParameters(segment);

            var tabsToCreate = parameters.GetValues<string>(KnownNavigationParameters.CreateTab);
            if (tabsToCreate.Count() > 0)
            {
                foreach (var tabToCreate in tabsToCreate)
                {
                    //created tab can be a single view or a view nested in a NavigationPage with the syntax "NavigationPage|ViewToCreate"
                    var tabSegements = tabToCreate.Split('|');
                    if (tabSegements.Length > 1)
                    {
                        var navigationPage = CreatePageFromSegment(tabSegements[0]) as NavigationPage;
                        if (navigationPage != null)
                        {
                            var navigationPageChild = CreatePageFromSegment(tabSegements[1]);

                            navigationPage.PushAsync(navigationPageChild);

                            //when creating a NavigationPage w/ DI, a blank Page object is injected into the ctor. Let's remove it
                            if (navigationPage.Navigation.NavigationStack.Count > 1)
                                navigationPage.Navigation.RemovePage(navigationPage.Navigation.NavigationStack[0]);

                            //set the title because Xamarin doesn't do this for us.
                            navigationPage.Title = navigationPageChild.Title;
                            navigationPage.Icon = navigationPageChild.Icon;

                            tabbedPage.Children.Add(navigationPage);
                        }
                    }
                    else
                    {
                        var tab = CreatePageFromSegment(tabToCreate);
                        tabbedPage.Children.Add(tab);
                    }
                }
            }

            TabbedPageSelectTab(tabbedPage, parameters);
        }

        void ConfigureCarouselPage(CarouselPage carouselPage, string segment)
        {
            foreach (var child in carouselPage.Children)
            {
                PageUtilities.SetAutowireViewModelOnPage(child);
            }

            var parameters = UriParsingHelper.GetSegmentParameters(segment);

            CarouselPageSelectTab(carouselPage, parameters);
        }

        private static void SelectPageTab(Page page, INavigationParameters parameters)
        {
            if (page is TabbedPage tabbedPage)
            {
                TabbedPageSelectTab(tabbedPage, parameters);
            }
            else if (page is CarouselPage carouselPage)
            {
                CarouselPageSelectTab(carouselPage, parameters);
            }
        }

        private static void TabbedPageSelectTab(TabbedPage tabbedPage, INavigationParameters parameters)
        {
            var selectedTab = parameters?.GetValue<string>(KnownNavigationParameters.SelectedTab);
            if (!string.IsNullOrWhiteSpace(selectedTab))
            {
                var selectedTabType = PageNavigationRegistry.GetPageType(UriParsingHelper.GetSegmentName(selectedTab));

                var childFound = false;
                foreach (var child in tabbedPage.Children)
                {
                    if (!childFound && child.GetType() == selectedTabType)
                    {
                        tabbedPage.CurrentPage = child;
                        childFound = true;
                    }

                    if (child is NavigationPage)
                    {
                        if (!childFound && ((NavigationPage)child).CurrentPage.GetType() == selectedTabType)
                        {
                            tabbedPage.CurrentPage = child;
                            childFound = true;
                        }
                    }
                }
            }
        }

        private static void CarouselPageSelectTab(CarouselPage carouselPage, INavigationParameters parameters)
        {
            var selectedTab = parameters?.GetValue<string>(KnownNavigationParameters.SelectedTab);
            if (!string.IsNullOrWhiteSpace(selectedTab))
            {
                var selectedTabType = PageNavigationRegistry.GetPageType(UriParsingHelper.GetSegmentName(selectedTab));

                foreach (var child in carouselPage.Children)
                {
                    if (child.GetType() == selectedTabType)
                        carouselPage.CurrentPage = child;
                }
            }
        }

        protected virtual async Task UseReverseNavigation(Page currentPage, string nextSegment, Queue<string> segments, INavigationParameters parameters, bool? useModalNavigation, bool animated)
        {
            var navigationStack = new Stack<string>();

            if (!String.IsNullOrWhiteSpace(nextSegment))
                navigationStack.Push(nextSegment);

            var illegalSegments = new Queue<string>();

            bool illegalPageFound = false;
            foreach (var item in segments)
            {
                //if we run into an illegal page, we need to create new navigation segments to properly handle the deep link
                if (illegalPageFound)
                {
                    illegalSegments.Enqueue(item);
                    continue;
                }

                //if any page decide to go modal, we need to consider it and all pages after it an illegal page
                var pageParameters = UriParsingHelper.GetSegmentParameters(item);
                if (pageParameters.ContainsKey(KnownNavigationParameters.UseModalNavigation))
                {
                    if (pageParameters.GetValue<bool>(KnownNavigationParameters.UseModalNavigation))
                    {
                        illegalSegments.Enqueue(item);
                        illegalPageFound = true;
                    }
                    else
                    {
                        navigationStack.Push(item);
                    }
                }
                else
                {
                    var pageType = PageNavigationRegistry.GetPageType(UriParsingHelper.GetSegmentName(item));
                    if (PageUtilities.IsSameOrSubclassOf<MasterDetailPage>(pageType))
                    {
                        illegalSegments.Enqueue(item);
                        illegalPageFound = true;
                    }
                    else
                    {
                        navigationStack.Push(item);
                    }
                }
            }

            var pageOffset = currentPage.Navigation.NavigationStack.Count;
            if (currentPage.Navigation.NavigationStack.Count > 2)
                pageOffset = currentPage.Navigation.NavigationStack.Count - 1;

            var onNavigatedFromTarget = currentPage;
            if (currentPage is NavigationPage navPage && navPage.CurrentPage != null)
                onNavigatedFromTarget = navPage.CurrentPage;

            bool insertBefore = false;
            while (navigationStack.Count > 0)
            {
                var segment = navigationStack.Pop();
                var nextPage = CreatePageFromSegment(segment);
                await DoNavigateAction(onNavigatedFromTarget, segment, nextPage, parameters, async () =>
                {
                    await DoPush(currentPage, nextPage, useModalNavigation, animated, insertBefore, pageOffset);
                });
                insertBefore = true;
            }

            //if an illegal page is found, we force a Modal navigation
            if (illegalSegments.Count > 0)
                await ProcessNavigation(currentPage.Navigation.NavigationStack.Last(), illegalSegments, parameters, true, animated);
        }

        protected virtual Task DoPush(Page currentPage, Page page, bool? useModalNavigation, bool animated, bool insertBeforeLast = false, int navigationOffset = 0)
        {
            if (page == null)
                throw new ArgumentNullException(nameof(page));

            if (currentPage == null)
            {
                _applicationProvider.MainPage = page;
                return Task.FromResult<object>(null);
            }
            else
            {
                bool useModalForPush = UseModalNavigation(currentPage, useModalNavigation);

                if (useModalForPush)
                {
                    return currentPage.Navigation.PushModalAsync(page, animated);
                }
                else
                {
                    if (insertBeforeLast)
                    {
                        return InsertPageBefore(currentPage, page, navigationOffset);
                    }
                    else
                    {
                        return currentPage.Navigation.PushAsync(page, animated);
                    }
                }

            }
        }

        protected virtual Task InsertPageBefore(Page currentPage, Page page, int pageOffset)
        {
            var navigationPage = currentPage.Parent as NavigationPage;
            var firstPage = currentPage.Navigation.NavigationStack.Skip(pageOffset).FirstOrDefault();
            currentPage.Navigation.InsertPageBefore(page, firstPage);
            return Task.FromResult(true);
        }

        protected virtual Task<Page> DoPop(INavigation navigation, bool useModalNavigation, bool animated)
        {
            if (useModalNavigation)
                return navigation.PopModalAsync(animated);
            else
                return navigation.PopAsync(animated);
        }

        protected virtual Page GetCurrentPage()
        {
            return _page != null ? _page : _applicationProvider.MainPage;
        }

        internal static bool UseModalNavigation(Page currentPage, bool? useModalNavigationDefault)
        {
            bool useModalNavigation = true;

            if (useModalNavigationDefault.HasValue)
                useModalNavigation = useModalNavigationDefault.Value;
            else if (currentPage is NavigationPage)
                useModalNavigation = false;
            else
                useModalNavigation = !PageUtilities.HasNavigationPageParent(currentPage);

            return useModalNavigation;
        }

        internal bool UseModalGoBack(Page currentPage, bool? useModalNavigationDefault)
        {
            if (useModalNavigationDefault.HasValue)
                return useModalNavigationDefault.Value;
            else if (currentPage is NavigationPage navPage)
                return GoBackModal(navPage);
            else if (PageUtilities.HasNavigationPageParent(currentPage, out var navParent))
                return GoBackModal(navParent);
            else
                return true;
        }

        private bool GoBackModal(NavigationPage navPage)
        {
            if (navPage.CurrentPage != navPage.RootPage)
                return false;
            else if (navPage.CurrentPage == navPage.RootPage && navPage.Parent is Application && _applicationProvider.MainPage != navPage)
                return true;
            else if (navPage.Parent is TabbedPage tabbed && tabbed != _applicationProvider.MainPage)
                return true;
            else if (navPage.Parent is CarouselPage carousel && carousel != _applicationProvider.MainPage)
                return true;

            return false;
        }

        internal static bool UseReverseNavigation(Page currentPage, Type nextPageType)
        {
            return PageUtilities.HasNavigationPageParent(currentPage) && PageUtilities.IsSameOrSubclassOf<ContentPage>(nextPageType);
        }
    }
    
    public static class NavigationParametersExtensions
    {
        public static NavigationMode GetNavigationMode(this INavigationParameters parameters)
        {
            var internalParams = (INavigationParametersInternal)parameters;
            if (internalParams.ContainsKey(KnownInternalParameters.NavigationMode))
                return internalParams.GetValue<NavigationMode>(KnownInternalParameters.NavigationMode);

            throw new System.ArgumentNullException("NavigationMode is not available");
        }
        
        public static NavigationMode GetNavigationModeDeux(this INavigationParameters parameters)
        {
            var internalParams = (INavigationParametersInternal)parameters;
            if (internalParams.ContainsKey(KnownInternalParameters.NavigationMode))
                return internalParams.GetValue<NavigationMode>(KnownInternalParameters.NavigationMode);

            throw new System.ArgumentNullException("NavigationMode is not available");
        }

        internal static INavigationParametersInternal GetNavigationParametersInternal(this INavigationParameters parameters)
        {
            return (INavigationParametersInternal)parameters;
        }
    }
    
    internal static class KnownInternalParameters
    {
        public const string NavigationMode = "__NavigationMode";
    }
    
    public static class PageUtilities
    {
        public static void InvokeViewAndViewModelAction<T>(object view, Action<T> action) where T : class
        {
            if (view is T viewAsT)
                action(viewAsT);

            if (view is BindableObject element)
            {
                if (element.BindingContext is T viewModelAsT)
                {
                    action(viewModelAsT);
                }
            }

            if (view is Page page && page.GetPartialViews() is List<BindableObject> partials)
            {
                foreach (var partial in partials)
                {
                    InvokeViewAndViewModelAction(partial, action);
                }
            }
        }

        public static async Task InvokeViewAndViewModelActionAsync<T>(object view, Func<T, Task> action) where T : class
        {
            if (view is T viewAsT)
                await action(viewAsT);

            if (view is BindableObject element)
            {
                if (element.BindingContext is T viewModelAsT)
                {
                    await action(viewModelAsT);
                }
            }

            if (view is Page page && page.GetPartialViews() is List<BindableObject> partials)
            {
                foreach (var partial in partials)
                {
                    await InvokeViewAndViewModelActionAsync(partial, action);
                }
            }
        }

        public static void DestroyPage(Page page)
        {
            try
            {
                DestroyChildren(page);

                InvokeViewAndViewModelAction<IDestructible>(page, v => v.Destroy());

                page.Behaviors?.Clear();
                page.BindingContext = null;
            }
            catch (Exception ex)
            {
                throw new Exception($"Cannot destroy {page}.", ex);
            }
        }

        private static void DestroyChildren(Page page)
        {
            switch(page)
            {
                case MasterDetailPage mdp:
                    DestroyPage(mdp.Master);
                    DestroyPage(mdp.Detail);
                    break;
                case TabbedPage tabbedPage:
                    foreach (var item in tabbedPage.Children.Reverse())
                    {
                        DestroyPage(item);
                    }
                    break;
                case CarouselPage carouselPage:
                    foreach (var item in carouselPage.Children.Reverse())
                    {
                        DestroyPage(item);
                    }
                    break;
                case NavigationPage navigationPage:
                    foreach (var item in navigationPage.Navigation.NavigationStack.Reverse())
                    {
                        DestroyPage(item);
                    }
                    break;
            }
        }

        public static void DestroyWithModalStack(Page page, IList<Page> modalStack)
        {
            foreach (var childPage in modalStack.Reverse())
            {
                DestroyPage(childPage);
            }
            DestroyPage(page);
        }


        public static Task<bool> CanNavigateAsync(object page, INavigationParameters parameters)
        {
            if (page is IConfirmNavigationAsync confirmNavigationItem)
                return confirmNavigationItem.CanNavigateAsync(parameters);

            if (page is BindableObject bindableObject)
            {
                if (bindableObject.BindingContext is IConfirmNavigationAsync confirmNavigationBindingContext)
                    return confirmNavigationBindingContext.CanNavigateAsync(parameters);
            }

            return Task.FromResult(CanNavigate(page, parameters));
        }

        public static bool CanNavigate(object page, INavigationParameters parameters)
        {
            if (page is IConfirmNavigation confirmNavigationItem)
                return confirmNavigationItem.CanNavigate(parameters);

            if (page is BindableObject bindableObject)
            {
                if (bindableObject.BindingContext is IConfirmNavigation confirmNavigationBindingContext)
                    return confirmNavigationBindingContext.CanNavigate(parameters);
            }

            return true;
        }

        public static void OnNavigatedFrom(object page, INavigationParameters parameters)
        {
            if (page != null)
                InvokeViewAndViewModelAction<INavigatedAware>(page, v => v.OnNavigatedFrom(parameters));
        }

        public static async Task OnInitializedAsync(object page, INavigationParameters parameters)
        {
            if (page is null) return;

            InvokeViewAndViewModelAction<IAbracadabra>(page, v => Abracadabra(v, parameters));
            InvokeViewAndViewModelAction<IInitialize>(page, v => v.Initialize(parameters));
            await InvokeViewAndViewModelActionAsync<IInitializeAsync>(page, async v => await v.InitializeAsync(parameters));
        }

        internal static void Abracadabra(object page, IEnumerable<KeyValuePair<string, object>> parameters)
        {
            var props = page.GetType()
                            .GetProperties(BindingFlags.Instance | BindingFlags.Public)
                            .Where(x => x.CanWrite);

            foreach(var prop in props)
            {
                (var name, var isRequired) = prop.GetAutoInitializeProperty();

                if(!parameters.HasKey(name, out var key))
                {
                    if (isRequired)
                        throw new ArgumentNullException(name);
                    continue;
                }

                prop.SetValue(page, parameters.GetValue(key, prop.PropertyType));
            }
        }

        private static bool HasKey(this IEnumerable<KeyValuePair<string, object>> parameters, string name, out string key)
        {
            key = parameters.Select(x => x.Key).FirstOrDefault(k => k.Equals(name, StringComparison.InvariantCultureIgnoreCase));
            return !string.IsNullOrEmpty(key);
        }

        private static (string Name, bool IsRequired) GetAutoInitializeProperty(this PropertyInfo pi)
        {
            var attr = pi.GetCustomAttribute<AutoInitializeAttribute>();
            if(attr is null)
            {
                return (pi.Name, false);
            }

            return (string.IsNullOrEmpty(attr.Name) ? pi.Name : attr.Name, attr.IsRequired);
        }

        public static void OnNavigatedTo(object page, INavigationParameters parameters)
        {
            if (page != null)
                InvokeViewAndViewModelAction<INavigatedAware>(page, v => v.OnNavigatedTo(parameters));
        }

        public static Page GetOnNavigatedToTarget(Page page, Page mainPage, bool useModalNavigation)
        {
            Page target;
            if (useModalNavigation)
            {
                var previousPage = GetPreviousPage(page, page.Navigation.ModalStack);

                //MainPage is not included in the navigation stack, so if we can't find the previous page above
                //let's assume they are going back to the MainPage
                target = GetOnNavigatedToTargetFromChild(previousPage ?? mainPage);
            }
            else
            {
                target = GetPreviousPage(page, page.Navigation.NavigationStack);
                if (target != null)
                    target = GetOnNavigatedToTargetFromChild(target);
                else
                    target = GetOnNavigatedToTarget(page, mainPage, true);
            }

            return target;
        }

        public static Page GetOnNavigatedToTargetFromChild(Page target)
        {
            Page child = null;

            if (target is MasterDetailPage)
                child = ((MasterDetailPage)target).Detail;
            else if (target is TabbedPage)
                child = ((TabbedPage)target).CurrentPage;
            else if (target is CarouselPage)
                child = ((CarouselPage)target).CurrentPage;
            else if (target is NavigationPage)
                child = target.Navigation.NavigationStack.Last();

            if (child != null)
                target = GetOnNavigatedToTargetFromChild(child);

            return target;
        }

        public static Page GetPreviousPage(Page currentPage, System.Collections.Generic.IReadOnlyList<Page> navStack)
        {
            Page previousPage = null;

            int currentPageIndex = GetCurrentPageIndex(currentPage, navStack);
            int previousPageIndex = currentPageIndex - 1;
            if (navStack.Count >= 0 && previousPageIndex >= 0)
                previousPage = navStack[previousPageIndex];

            return previousPage;
        }

        public static int GetCurrentPageIndex(Page currentPage, System.Collections.Generic.IReadOnlyList<Page> navStack)
        {
            int stackCount = navStack.Count;
            for (int x = 0; x < stackCount; x++)
            {
                var view = navStack[x];
                if (view == currentPage)
                    return x;
            }

            return stackCount - 1;
        }

        public static Page GetCurrentPage(Page mainPage)
        {
            var page = mainPage;

            var lastModal = page.Navigation.ModalStack.LastOrDefault();
            if (lastModal != null)
                page = lastModal;

            return GetOnNavigatedToTargetFromChild(page);
        }

        public static void HandleSystemGoBack(Page previousPage, Page currentPage)
        {
            var parameters = new NavigationParameters();
            parameters.GetNavigationParametersInternal().Add(KnownInternalParameters.NavigationMode, NavigationMode.Back);
            OnNavigatedFrom(previousPage, parameters);
            OnNavigatedTo(GetOnNavigatedToTargetFromChild(currentPage), parameters);
            DestroyPage(previousPage);
        }

        internal static bool HasDirectNavigationPageParent(Page page)
        {
            return page?.Parent != null && page?.Parent is NavigationPage;
        }

        internal static bool HasNavigationPageParent(Page page) =>
            HasNavigationPageParent(page, out var _);

        internal static bool HasNavigationPageParent(Page page, out NavigationPage navigationPage)
        {
            if (page?.Parent != null)
            {
                if (page.Parent is NavigationPage navParent)
                {
                    navigationPage = navParent;
                    return true;
                }
                else if ((page.Parent is TabbedPage || page.Parent is CarouselPage) && page.Parent?.Parent is NavigationPage navigationParent)
                {
                    navigationPage = navigationParent;
                    return true;
                }
            }

            navigationPage = null;
            return false;
        }

        internal static bool IsSameOrSubclassOf<T>(Type potentialDescendant)
        {
            if (potentialDescendant == null)
                return false;

            Type potentialBase = typeof(T);

            return potentialDescendant.GetTypeInfo().IsSubclassOf(potentialBase)
                   || potentialDescendant == potentialBase;
        }

        internal static void SetAutowireViewModelOnPage(Page page)
        {
            var vmlResult = ViewModelLocator.GetAutowireViewModel(page);
            if (vmlResult == null)
                ViewModelLocator.SetAutowireViewModel(page, true);
        }
    }
    
    /// <summary>
    /// This class defines the attached property and related change handler that calls the <see cref="Prism.Mvvm.ViewModelLocationProvider"/>.
    /// </summary>
    public static class ViewModelLocator
    {
        /// <summary>
        /// Instructs Prism whether or not to automatically create an instance of a ViewModel using a convention, and assign the associated View's <see cref="Xamarin.Forms.BindableObject.BindingContext"/> to that instance.
        /// </summary>
        public static readonly BindableProperty AutowireViewModelProperty =
            BindableProperty.CreateAttached("AutowireViewModel", typeof(bool?), typeof(ViewModelLocator), null, propertyChanged: OnAutowireViewModelChanged);

        /// <summary>
        /// Instructs Prism to use a given page as the parent for a Partial View
        /// </summary>
        public static readonly BindableProperty AutowirePartialViewProperty =
            BindableProperty.CreateAttached("AutowirePartialView", typeof(Page), typeof(ViewModelLocator), null, propertyChanged: OnAutowirePartialViewChanged);

        internal static readonly BindableProperty PartialViewsProperty =
            BindableProperty.CreateAttached("PrismPartialViews", typeof(List<BindableObject>), typeof(ViewModelLocator), null);

        /// <summary>
        /// Gets the AutowireViewModel property value.
        /// </summary>
        /// <param name="bindable"></param>
        /// <returns></returns>
        public static bool? GetAutowireViewModel(BindableObject bindable)
        {
            return (bool?)bindable.GetValue(ViewModelLocator.AutowireViewModelProperty);
        }

        /// <summary>
        /// Sets the AutowireViewModel property value.  If <c>true</c>, creates an instance of a ViewModel using a convention, and sets the associated View's <see cref="Xamarin.Forms.BindableObject.BindingContext"/> to that instance.
        /// </summary>
        /// <param name="bindable"></param>
        /// <param name="value"></param>
        public static void SetAutowireViewModel(BindableObject bindable, bool? value)
        {
            bindable.SetValue(ViewModelLocator.AutowireViewModelProperty, value);
        }

        public static Page GetAutowirePartialView(BindableObject bindable)
        {
            return (Page)bindable.GetValue(AutowirePartialViewProperty);
        }

        public static void SetAutowirePartialView(BindableObject bindable, Page page)
        {
            bindable.SetValue(AutowirePartialViewProperty, page);
        }

        internal static List<BindableObject> GetPartialViews(this Page page)
        {
            return (List<BindableObject>)page.GetValue(PartialViewsProperty);
        }

        private static void OnAutowireViewModelChanged(BindableObject bindable, object oldValue, object newValue)
        {
            bool? bNewValue = (bool?)newValue;
            if (bNewValue.HasValue && bNewValue.Value)
                ViewModelLocationProvider.AutoWireViewModelChanged(bindable, Bind);
        }

        private static void OnAutowirePartialViewChanged(BindableObject bindable, object oldValue, object newValue)
        {
            if (oldValue == newValue)
                return;

            if (oldValue is Page oldPage)
            {
                List<BindableObject> oldPartials = oldPage.GetPartialViews();
                oldPartials.Remove(bindable);
            }

            if (newValue is Page page)
            {
                // Add View to Views Collection for Page.
                List<BindableObject> partialViews = page.GetPartialViews();
                if (partialViews == null)
                {
                    partialViews = new List<BindableObject>();
                    page.SetValue(PartialViewsProperty, partialViews);
                }

                partialViews.Add(bindable);
                // Set Autowire Property
                if (bindable.GetValue(AutowireViewModelProperty) == null)
                {
                    bindable.SetValue(AutowireViewModelProperty, true);
                }
            }
        }

        /// <summary>
        /// Sets the <see cref="Xamarin.Forms.BindableObject.BindingContext"/> of a View
        /// </summary>
        /// <param name="view">The View to set the <see cref="Xamarin.Forms.BindableObject.BindingContext"/> on</param>
        /// <param name="viewModel">The object to use as the <see cref="Xamarin.Forms.BindableObject.BindingContext"/> for the View</param>
        private static void Bind(object view, object viewModel)
        {
            if (view is BindableObject element)
                element.BindingContext = viewModel;
        }
    }
}
