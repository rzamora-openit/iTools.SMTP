using MatBlazor;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using OpeniT.SMTP.Web.Models;
using OpeniT.SMTP.Web.Methods;
using OpeniT.SMTP.Web.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using OpeniT.SMTP.Web.DataRepositories;
using Microsoft.JSInterop;
using OpeniT.SMTP.Web.Pages.Shared;
using OpeniT.SMTP.Web.Helpers;
using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;

namespace OpeniT.SMTP.Web.Pages.Admin
{
    [Authorize(Roles = "Administrator, Developer")]
    public partial class TableMails : ComponentBase, IDisposable
    {
        [Inject] private IDataRepository dataRepository { get; set; }
        [Inject] private IJSRuntime jsRuntime { get; set; }
        [Inject] private AzureHelper azureHelper { get; set; }
        [Inject] private NavigationManager navigationManager { get; set; }

        [CascadingParameter] public SiteCascadingValueViewModel siteCascadingValue { get; set; }

        [Parameter] public bool IsOpen { get; set; }

        [Parameter] public string BaseRouteTemplate { get; set; }

        [Parameter]
        public string SearchValue
        {
            get
            {
                return searchValue;
            }
            set
            {
                searchValue = value;
                InvokeAsync(this.FiltersChanged);
            }
        }

        [Parameter]
        public MatPaginatorPageEvent SelectedPage
        {
            get
            {
                return selectedPage;
            }
            set
            {
                selectedPage = value;
                InvokeAsync(this.FiltersChanged);
            }
        }

        [Parameter]
        public MatSortChangedEvent SelectedSort
        {
            get
            {
                return selectedSort;
            }
            set
            {
                selectedSort = value;
                InvokeAsync(this.FiltersChanged);
            }
        }

        public List<SmtpMail> Mails;
        public string CurrentFiltersUri;

        #region Filters
        private string searchValue;
        private string _searchValue;
        private MatPaginatorPageEvent selectedPage = new MatPaginatorPageEvent()
        {
            PageIndex = 0,
            PageSize = 25
        };
        private MatSortChangedEvent selectedSort = new MatSortChangedEvent()
        {
            Direction = MatSortDirection.Asc,
            SortId = "Name"
        };
        #endregion Filters

        private CancellationTokenSource loadDataCts;

        private bool isBusy = true;
        private bool isDisposed = false;
        private bool isDataLoaded = false;

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (firstRender)
            {
                isBusy = true;
                StateHasChanged();

                await this.LoadData();
                await this.NavigateToFiltersQueryStrings();

                isBusy = false;
                StateHasChanged();
            }
        }

        private Task SearchChanged(string value)
        {
            searchValue = value;
            _searchValue = value;

            return this.FiltersChanged();
        }

        private Task SortChanged(MatSortChangedEvent sort)
        {
            sort.Direction = sort.Direction == MatSortDirection.Asc || sort.Direction == MatSortDirection.None ? MatSortDirection.Asc : MatSortDirection.Desc;

            selectedSort = sort;

            return this.FiltersChanged();
        }

        private Task PaginationChanged(MatPaginatorPageEvent page)
        {
            selectedPage = page;

            return this.FiltersChanged();
        }

        private async Task FiltersChanged()
        {
            isDataLoaded = false;
            isBusy = true;
            StateHasChanged();

            await this.NavigateToFiltersQueryStrings();
            await this.LoadData();

            isBusy = false;
            StateHasChanged();
        }

        public async Task Insert(SmtpMail mail)
        {
            try
            {
                await this.LoadData();

                if (Mails?.Contains(mail) == true)
                {
                    Mails?.Remove(mail);
                }
                else
                {
                    Mails?.Remove(Mails?.LastOrDefault());
                }

                Mails?.Insert(0, mail);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            finally
            {
                StateHasChanged();
            }
        }

        public async Task Remove(SmtpMail mail)
        {
            try
            {
                if (Mails?.Contains(mail) == true)
                {
                    Mails?.Remove(mail);
                    selectedPage.PageSize = selectedPage.PageSize == int.MaxValue ? selectedPage.PageSize : selectedPage.PageSize - 1;
                }

                selectedPage.Length = selectedPage.Length - 1;

                await this.NavigateToFiltersQueryStrings();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            finally
            {
                StateHasChanged();
            }
        }

        public async Task LoadData()
        {
            try
            {
                if (!isDataLoaded)
                {
                    loadDataCts?.Cancel();
                    loadDataCts = new CancellationTokenSource();

                    await this.GetQueryStringValues();
                    Expression<Func<SmtpMail, bool>> filterExpression = null;
                    if (!string.IsNullOrWhiteSpace(SearchValue))
                    {
                        filterExpression = m =>
                            EF.Functions.Like(m.Subject, $"%{SearchValue}%") ||
                            EF.Functions.Like(m.DateCreated.ToString(), $"%{SearchValue}%") ||
                            EF.Functions.Like(m.Body, $"%{SearchValue}%") ||
                            EF.Functions.Like(m.From.Address, $"%{SearchValue}%") ||
                            m.To.Any(to => EF.Functions.Like(to.Address, $"%{SearchValue}%")) ||
                            m.CC.Any(cc => EF.Functions.Like(cc.Address, $"%{SearchValue}%"));
                    }

                    List<DataSort<SmtpMail, object>> dataSorts = new List<DataSort<SmtpMail, object>>();

                    switch (SelectedSort.SortId)
                    {
                        case "Subject":
                        default:
                            dataSorts.Add(new DataSort<SmtpMail, object>()
                            {
                                OrderExpression = m => m.Subject
                            });
                            break;
                        case "Date":
                            dataSorts.Add(new DataSort<SmtpMail, object>()
                            {
                                OrderExpression = m => m.DateCreated
                            });
                            break;
                        case "Body":
                            dataSorts.Add(new DataSort<SmtpMail, object>()
                            {
                                OrderExpression = m => m.Body
                            });
                            break;
                        case "From":
                            dataSorts.Add(new DataSort<SmtpMail, object>()
                            {
                                OrderExpression = m => m.From.Address
                            });
                            break;
                        case "To":
                            dataSorts.Add(new DataSort<SmtpMail, object>()
                            {
                                OrderExpression = m => m.To.Any() ? m.To.OrderBy(to => to.Address).First().Address : null
                            });
                            dataSorts.Add(new DataSort<SmtpMail, object>()
                            {
                                OrderExpression = m => m.To.Any() ? m.To.OrderBy(to => to.Address).Last().Address : null
                            });
                            break;
                        case "CC":
                            dataSorts.Add(new DataSort<SmtpMail, object>()
                            {
                                OrderExpression = m => m.CC.Any() ? m.CC.OrderBy(cc => cc.Address).First().Address : null
                            });
                            dataSorts.Add(new DataSort<SmtpMail, object>()
                            {
                                OrderExpression = m => m.CC.Any() ? m.CC.OrderBy(cc => cc.Address).Last().Address : null
                            });
                            break;
                    }
                    dataSorts.ForEach(dataSort => dataSort.SortDirection = SelectedSort.Direction == MatSortDirection.Asc ? SortDirection.ASC : SortDirection.DESC);

                    SelectedPage.Length = await this.dataRepository.GetCount(filterExpression);

                    var totalPages = Math.Max(0, (int)Math.Ceiling((decimal)SelectedPage.Length / SelectedPage.PageSize));
                    if (SelectedPage.PageIndex >= totalPages || SelectedPage.PageSize < 0 || SelectedPage.Length <= SelectedPage.PageSize)
                    {
                        if (SelectedPage.PageIndex >= totalPages)
                        {
                            SelectedPage.PageIndex = Math.Max(0, totalPages - 1);
                        }

                        if (SelectedPage.PageSize < 0 || SelectedPage.Length <= SelectedPage.PageSize)
                        {
                            SelectedPage.PageIndex = 0;
                        }

                        await this.NavigateToFiltersQueryStrings();
                    }

                    var mailsQuery = this.dataRepository.GetQueryable(filterExpression: filterExpression, includeDepth: 2, dataPagination: new DataPagination() { PageSize = SelectedPage.PageSize, PageIndex = SelectedPage.PageIndex }, dataSorts: dataSorts.ToArray());
                    mailsQuery = mailsQuery.Select(m => new SmtpMail() { Id = m.Id, Guid = m.Guid, Subject = m.Subject, DateCreated = m.DateCreated, From = m.From, To = m.To, CC = m.CC });

                    Mails = await this.dataRepository.GetAll(mailsQuery, loadDataCts.Token);

                    isDataLoaded = true;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        private bool SelectedFilterAny()
        {
            return !string.IsNullOrWhiteSpace(searchValue);
        }

        private async Task GetQueryStringValues()
        {
            string pageIndexQueryString = await JSIntropMethods.GetUrlParam(jsRuntime, "page-index");
            string pageSizeQueryString = await JSIntropMethods.GetUrlParam(jsRuntime, "page-size");
            string searchValueQueryString = await JSIntropMethods.GetUrlParam(jsRuntime, "search");
            string sortQueryString = await JSIntropMethods.GetUrlParam(jsRuntime, "sort");
            string sortDirectionQueryString = await JSIntropMethods.GetUrlParam(jsRuntime, "sort-direction");

            if (int.TryParse(pageIndexQueryString, out int pageIndex))
            {
                selectedPage.PageIndex = Math.Max(0, pageIndex - 1);
            }
            else
            {
                selectedPage.PageIndex = 0;
            }

            if (int.TryParse(pageSizeQueryString, out int pageSize))
            {
                selectedPage.PageSize = pageSize;
            }
            else
            {
                selectedPage.PageSize = 25;
            }

            if (!string.IsNullOrWhiteSpace(searchValueQueryString))
            {
                searchValue = searchValueQueryString;
            }
            else
            {
                searchValue = null;
            }

            if (!string.IsNullOrWhiteSpace(sortQueryString))
            {
                selectedSort.SortId = sortQueryString;
            }
            else
            {
                selectedSort.SortId = "Name";
            }

            if (!string.IsNullOrWhiteSpace(sortDirectionQueryString))
            {
                selectedSort.Direction = sortDirectionQueryString == "ASC" ? MatSortDirection.Asc : MatSortDirection.Desc;
            }
            else
            {
                selectedSort.Direction = MatSortDirection.Asc;
            }
        }

        private async Task NavigateToFiltersQueryStrings()
        {
            CurrentFiltersUri = $"{BaseRouteTemplate}?page-index={Math.Max(1, selectedPage.PageIndex + 1)}&page-size={selectedPage.PageSize}";

            if (!string.IsNullOrWhiteSpace(selectedSort.SortId))
            {
                CurrentFiltersUri += $"&sort={selectedSort.SortId.ToUrlParam()}&sort-direction={(selectedSort.Direction == MatSortDirection.Asc ? "ASC" : "DESC")}";
            }

            if (!string.IsNullOrWhiteSpace(searchValue))
            {
                CurrentFiltersUri += $"&search={searchValue.ToUrlParam()}";
            }

            if (this.navigationManager.RouteTemplateMatch(BaseRouteTemplate))
            {
                await JSIntropMethods.ReplaceState(jsRuntime, null, null, CurrentFiltersUri);
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (isDisposed)
            {
                return;
            }

            if (disposing)
            {
                loadDataCts?.Cancel();
            }

            isDisposed = true;
        }
    }
}
