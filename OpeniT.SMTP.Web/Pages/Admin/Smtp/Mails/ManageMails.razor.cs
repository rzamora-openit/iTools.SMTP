using MatBlazor;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.JSInterop;
using OpeniT.SMTP.Web.Models;
using OpeniT.SMTP.Web.DataRepositories;
using OpeniT.SMTP.Web.Helpers;
using OpeniT.SMTP.Web.Methods;
using OpeniT.SMTP.Web.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;

namespace OpeniT.SMTP.Web.Pages.Admin
{
	[Authorize(Roles = "Administrator, Developer, User-Internal")]
	[Route("/smtp/mails"), 
	Route("/smtp/mails/add"),
	Route("/smtp/mails/copy/{mailGuidString}"),
	Route("/smtp/mails/view/{mailGuidString}"),
	Route("/smtp/mails/delete/{mailGuidString}")]
	public partial class ManageMails : ComponentBase, IDisposable
	{
		[Inject] private IPortalRepository portalRepository { get; set; }
		[Inject] private IJSRuntime jsRuntime { get; set; }
		[Inject] private NavigationManager navigationManager { get; set; }
		[Inject] private AzureHelper azureHelper { get; set; }

		[Parameter] public string mailGuidString { get; set; }
		public Guid MailGuid
		{
			get
			{
				if (Guid.TryParse(mailGuidString, out var result))
				{
					return result;
				}

				return default;
			}
		}

		private SemaphoreSlim singleTaskQueue = new SemaphoreSlim(1);

		public bool IsBusy = false;
		public bool IsDataLoaded = false;
		public bool IsSiteValuesLoaded = false;
		public bool IsDisposing = false;

		public string MailsBaseRelativePath = "smtp/mails";

		public SmtpMail Mail = new SmtpMail();
		public List<SmtpMail> Mails;

		#region SiteValues
		public List<string> MailAddresses = new List<string>();
		public List<AzureProfile> Profiles = new List<AzureProfile>();
		#endregion SiteValues

		#region Filters

		public string CurrentFiltersUri;
		public bool HasSelectedFilter = false;

		public string SearchValue;

		public MatPaginatorPageEvent SelectedPage = new MatPaginatorPageEvent()
		{
			PageIndex = 0,
			PageSize = 25
		};

		public MatSortChangedEvent SelectedSort = new MatSortChangedEvent()
		{
			Direction = MatSortDirection.Asc,
			SortId = "Subject"
		};
		#endregion Filters

		private Dictionary<string, ComponentStateViewModel> componentStates = new Dictionary<string, ComponentStateViewModel>()
		{
			{ "table", new ComponentStateViewModel() { Rendered = false, Shown = false } },
			{ "add", new ComponentStateViewModel() { Rendered = false, Shown = false } },
			{ "view", new ComponentStateViewModel() { Rendered = false, Shown = false } },
			{ "copy", new ComponentStateViewModel() { Rendered = false, Shown = false } },
			{ "delete", new ComponentStateViewModel() { Rendered = false, Shown = false } }
		};

		private readonly EventHandler<LocationChangedEventArgs>? locationChanged;

		public ManageMails()
		{
			locationChanged = async (sender, eventArgs) => await HandleLocationChange();
		}

		protected override async Task OnInitializedAsync()
		{
			this.navigationManager.LocationChanged += locationChanged;

			await this.HandleLocationChange();
			await this.NavigateToFiltersQueryStrings();
		}

		private async Task HandleLocationChange()
		{
			try
			{
				if (!IsDisposing)
				{
					IsBusy = true;
					StateHasChanged();

					var baseRelativePath = this.navigationManager.GetBaseRelativePath();
					if (string.Equals(baseRelativePath, MailsBaseRelativePath))
					{
						this.SetCurrentVisibleComponents(new[] { "table" });
					}

					if (string.Equals(baseRelativePath, $"{MailsBaseRelativePath}/add"))
					{
						this.SetCurrentVisibleComponents(new[] { "add" });
					}

					if (baseRelativePath.StartsWith($"{MailsBaseRelativePath}/view") && MailGuid != default)
					{
						this.SetCurrentVisibleComponents(new[] { "table", "view" });
					}

					if (baseRelativePath.StartsWith($"{MailsBaseRelativePath}/copy") && MailGuid != default)
					{
						this.SetCurrentVisibleComponents(new[] { "copy" });
					}

					if (baseRelativePath.StartsWith($"{MailsBaseRelativePath}/delete") && MailGuid != default)
					{
						this.SetCurrentVisibleComponents(new[] { "table", "delete" });
					}

					await singleTaskQueue.Enqueue(() => this.portalRepository.ReloadEntry(Mail));

					await singleTaskQueue.Enqueue(() => this.LoadSiteValues());

					if (mailGuidString != null)
					{
						Mail = await singleTaskQueue.Enqueue(() => this.portalRepository.GetFirst<SmtpMail>(filterExpression: m => m.Guid == MailGuid, includeDepth: 2));

						if (Mail == null)
						{
							Mail = new SmtpMail();
							this.SetCurrentVisibleComponents(new[] { "not-found" });
						}
					}

					if (componentStates.TryGetValue("table", out var tableComponentState) && (tableComponentState?.Shown).GetValueOrDefault())
					{
						await this.LoadData();
					}

					IsBusy = false;
					StateHasChanged();
				}
			}
			catch (Exception ex)
			{
				Console.WriteLine(ex.Message);
			}
		}

		private async Task LoadSiteValues()
		{
			if (!IsSiteValuesLoaded)
			{
				IsSiteValuesLoaded = true;

				Profiles = await azureHelper.GetUsers($"?$select=accountEnabled,mail,companyName,displayName,department,givenName,jobTitle,physicalDeliveryOfficeName,surname,userPrincipalName&$top=999");
				MailAddresses = Profiles?.Where(p => p != null && p.AccountEnabled)?.Select(p => p.Mail)?.Distinct()?.ToList() ?? new List<string>();
			}
		}

		private async Task LoadData()
		{
			try
			{
				if (!IsDataLoaded)
				{
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

					SelectedPage.Length = await singleTaskQueue.Enqueue(() => this.portalRepository.GetCount(filterExpression));

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

					var mailsQuery = await singleTaskQueue.Enqueue(() => this.portalRepository.GetQueryable(filterExpression: filterExpression, includeDepth: 2, dataPagination: new DataPagination() { PageSize = SelectedPage.PageSize, PageIndex = SelectedPage.PageIndex }, dataSorts: dataSorts.ToArray()));
					Mails = await singleTaskQueue.Enqueue(() => mailsQuery.Select(m => new SmtpMail() { Id = m.Id, Guid = m.Guid, Subject = m.Subject, DateCreated = m.DateCreated, From = m.From, To = m.To, CC = m.CC }).ToListAsync());

					IsDataLoaded = true;
					HasSelectedFilter = this.SelectedFilterAny();
				}
			}
			catch (Exception ex)
			{
				Console.WriteLine(ex.Message);
			}
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
				SelectedPage.PageIndex = Math.Max(0, pageIndex - 1);
			}
			else
			{
				SelectedPage.PageIndex = 0;
			}

			if (int.TryParse(pageSizeQueryString, out int pageSize))
			{
				SelectedPage.PageSize = pageSize;
			}
			else
			{
				SelectedPage.PageSize = 25;
			}

			if (!string.IsNullOrWhiteSpace(searchValueQueryString))
			{
				SearchValue = searchValueQueryString;
			}
			else
			{
				SearchValue = null;
			}

			if (!string.IsNullOrWhiteSpace(sortQueryString))
			{
				SelectedSort.SortId = sortQueryString;
			}
			else
			{
				SelectedSort.SortId = "Subject";
			}

			if (!string.IsNullOrWhiteSpace(sortDirectionQueryString))
			{
				SelectedSort.Direction = sortDirectionQueryString == "ASC" ? MatSortDirection.Asc : MatSortDirection.Desc;
			}
			else
			{
				SelectedSort.Direction = MatSortDirection.Asc;
			}
		}

		private async Task FiltersChanged()
		{
			IsDataLoaded = false;
			IsBusy = true;
			StateHasChanged();

			await this.NavigateToFiltersQueryStrings();
			await this.LoadData();

			IsBusy = false;
			StateHasChanged();
		}

		private async Task NavigateToFiltersQueryStrings()
		{
			CurrentFiltersUri = $"{MailsBaseRelativePath}?page-index={Math.Max(1, SelectedPage.PageIndex + 1)}&page-size={SelectedPage.PageSize}";

			if (!string.IsNullOrWhiteSpace(SelectedSort.SortId))
			{
				CurrentFiltersUri += $"&sort={SelectedSort.SortId.ToUrlParam()}&sort-direction={(SelectedSort.Direction == MatSortDirection.Asc ? "ASC" : "DESC")}";
			}

			if (!string.IsNullOrWhiteSpace(SearchValue))
			{
				CurrentFiltersUri += $"&search={SearchValue.ToUrlParam()}";
			}

			var baseRelativePath = this.navigationManager.GetBaseRelativePath();
			if (string.Equals(baseRelativePath, MailsBaseRelativePath))
			{
				await JSIntropMethods.ReplaceState(jsRuntime, null, null, CurrentFiltersUri);
			}
		}

		public async Task<SmtpMail> GetMailCopy(SmtpMail model)
		{
			try
			{
				IsBusy = true;
				StateHasChanged();

				if (model != null)
				{
					var copy = await singleTaskQueue.Enqueue(() => this.portalRepository.CloneEntry<SmtpMail>(model));

					IsBusy = false;
					StateHasChanged();

					return copy;
				}
			}
			catch (Exception ex)
			{
				Console.WriteLine(ex.Message);
			}

			return null;
		}

		public async Task<bool> AddMail(SmtpMail model)
		{
			try
			{
				IsBusy = true;
				StateHasChanged();

				if (model != null)
				{
					await singleTaskQueue.Enqueue(() => this.portalRepository.Add<SmtpMail>(model));

					if (await singleTaskQueue.Enqueue(() => this.portalRepository.SaveChangesAsync()))
					{
						Mails?.Insert(0, model);

						IsBusy = false;
						StateHasChanged();

						return true;
					}
				}
			}
			catch (Exception ex)
			{
				Console.WriteLine(ex.Message);
			}

			return false;
		}

		public async Task<bool> DeleteMail(SmtpMail model)
		{
			try
			{
				IsBusy = true;
				StateHasChanged();

				if (model != null)
				{
					await singleTaskQueue.Enqueue(() => this.portalRepository.Remove<SmtpMail>(model));

					if (await singleTaskQueue.Enqueue(() => this.portalRepository.SaveChangesAsync()))
					{
						Mails?.Remove(model);

						IsBusy = false;
						StateHasChanged();

						return true;
					}
				}
			}
			catch (Exception ex)
			{
				Console.WriteLine(ex.Message);
			}

			return false;
		}

		private void ComponentDialogIsOpenChanged(bool isOpen, string component)
		{
			if (componentStates.ContainsKey(component))
			{
				var componentState = componentStates[component];
				componentState.Shown = isOpen;

				var baseRelativePath = this.navigationManager.GetBaseRelativePath();
				if (!componentState.Shown && baseRelativePath.StartsWith($"{MailsBaseRelativePath}/{component}"))
				{
					this.navigationManager.NavigateTo(CurrentFiltersUri);
				}
			}
		}

		private void SetCurrentVisibleComponents(IEnumerable<string> components)
		{
			foreach (string component in componentStates.Keys.ToList())
			{
				if (components.Contains(component))
				{
					componentStates[component].Shown = true;
					componentStates[component].Rendered = true;
				}
				else
				{
					componentStates[component].Shown = false;
				}

				StateHasChanged();
			}
		}

		public IEnumerable<string> GetVisibleComponents()
		{
			return componentStates.Where(cs => cs.Value.Shown).Select(cs => cs.Key);
		}

		public bool SelectedFilterAny()
		{
			return !string.IsNullOrWhiteSpace(SearchValue);
		}

		protected virtual void Dispose(bool disposing)
		{
			IsDisposing = disposing;
		}

		void IDisposable.Dispose()
		{
			this.navigationManager.LocationChanged -= locationChanged;
			Dispose(disposing: true);
		}
	}
}
