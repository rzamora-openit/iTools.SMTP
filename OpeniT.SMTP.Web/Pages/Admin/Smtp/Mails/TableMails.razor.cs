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

namespace OpeniT.SMTP.Web.Pages.Admin
{
    [Authorize(Roles = "Administrator, Developer, User-Internal")]
    public partial class TableMails : ComponentBase
    {
        [CascadingParameter] public SiteCascadingValueViewModel siteCascadingValue { get; set; } = new SiteCascadingValueViewModel();
        [CascadingParameter] public ManageMails ManageMails { get; set; }

        [Parameter] public bool Shown { get; set; } = false;
        [Parameter] public EventCallback OnFilterChanged { get; set; }

        private string _searchValue;
        private bool filterDropdownIsOpen;

        private CancellationTokenSource filterCTS;

        private Task SearchChanged(string searchValue)
        {
            _searchValue = searchValue;
            ManageMails.SearchValue = searchValue;

            return this.DebounceFilter();
        }

        private Task SortChanged(MatSortChangedEvent sort)
        {
            sort.Direction = sort.Direction == MatSortDirection.Asc || sort.Direction == MatSortDirection.None ? MatSortDirection.Asc : MatSortDirection.Desc;

            ManageMails.SelectedSort = sort;

            return this.DebounceFilter();
        }

        private Task PaginationChanged(MatPaginatorPageEvent pagination)
        {
            ManageMails.SelectedPage = pagination;

            return this.DebounceFilter();
        }

        private async Task DebounceFilter()
        {
            filterCTS?.Cancel();
            filterCTS = new CancellationTokenSource();
            var filterCT = filterCTS.Token;

            await Task.Delay(100);
            if (!filterCT.IsCancellationRequested)
            {
                await OnFilterChanged.InvokeAsync(null);
            }
        }
    }
}
