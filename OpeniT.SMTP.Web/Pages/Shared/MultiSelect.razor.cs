using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace OpeniT.SMTP.Web.Pages.Shared
{
	public partial class MultiSelect<TValue> : ComponentBase
	{
		[Inject] private IJSRuntime jsRunTime { get; set; }

		[Parameter] public IEnumerable<TValue> Items { get; set; }
		[Parameter] public ICollection<TValue> SelectedItems { get; set; } = new List<TValue>();
		[Parameter] public EventCallback<ICollection<TValue>> SelectedItemsChanged { get; set; }
		[Parameter] public Func<TValue, string> ValueSelector { get; set; }
		[Parameter] public RenderFragment<TValue> ItemTemplate { get; set; }
		[Parameter] public ElementReference Ref { get; set; } = new ElementReference();
		[Parameter] public string InputId { get; set; }
		[Parameter] public string InputClass { get; set; }
		[Parameter] public string InputStyle { get; set; }
		[Parameter] public string InputLabel { get; set; }
		[Parameter] public string InputPlaceHolder { get; set; }
		[Parameter] public string InputIcon { get; set; }
		[Parameter] public bool DisplayTooltip { get; set; }
		[Parameter] public string MenuClass { get; set; }
		[Parameter] public string MenuStyle { get; set; }
		[Parameter] public bool Disabled { get; set; }
		[Parameter] public bool OverlayScrollbarsEnabled { get; set; } = true;

		private ICollection<TValue> previousSelectedItems;
		private IEnumerable<TValue> previousItems;

		private IEnumerable<TValue> filteredItems;

		private string selectedItemsString;

		private string searchValue;
		private string _searchValue;
		private CancellationTokenSource debounceCTS;

		protected override async Task OnParametersSetAsync()
		{
			if (!Enumerable.SequenceEqual(previousItems?.OrderBy(i => ValueSelector.Invoke(i)) ?? Enumerable.Empty<TValue>(), Items?.OrderBy(i => ValueSelector.Invoke(i)) ?? Enumerable.Empty<TValue>()))
			{
				previousItems = Items;

				await this.DebounceSearch(150);
			}

			if (previousSelectedItems != SelectedItems)
			{
				previousSelectedItems = SelectedItems;

				if (SelectedItems == null)
				{
					SelectedItems = new List<TValue>();
				}

				selectedItemsString = string.Join(", ", SelectedItems?.Select((ValueSelector) ?? (i => string.Empty)) ?? Enumerable.Empty<string>());
			}
		}

		private void CheckAll(bool? isAllChecked)
		{
			if (!Disabled)
			{
				SelectedItems = SelectedItems ?? new List<TValue>();

				if (isAllChecked != true)
				{
					SelectedItems = Items.ToList();
				}
				else
				{
					SelectedItems.Clear();
				}

				selectedItemsString = string.Join(", ", SelectedItems?.Select((ValueSelector) ?? (i => string.Empty)) ?? Enumerable.Empty<string>());
				SelectedItemsChanged.InvokeAsync(SelectedItems);
			}
		}

		private void CheckedChanged(TValue item, bool isChecked)
		{
			if (!Disabled)
			{
				SelectedItems = SelectedItems ?? new List<TValue>();

				if (isChecked)
				{
					if (SelectedItems.Contains(item))
					{
						SelectedItems.Remove(item);
					}
				}
				else
				{
					if (!SelectedItems.Contains(item))
					{
						SelectedItems.Add(item);
					}
				}

				selectedItemsString = string.Join(", ", SelectedItems?.Select((ValueSelector) ?? (i => string.Empty)) ?? Enumerable.Empty<string>());
				SelectedItemsChanged.InvokeAsync(SelectedItems);
			}
		}

		private Task SearchChanged(string value)
		{
			searchValue = value;
			_searchValue = value;

			return this.DebounceSearch(100);
		}

		private async Task DebounceSearch(int DebounceMilliseconds)
		{
			debounceCTS?.Cancel();
			debounceCTS = new CancellationTokenSource();
			var cancellationToken = debounceCTS.Token;

			await Task.Delay(DebounceMilliseconds);
			if (!cancellationToken.IsCancellationRequested)
			{
				filteredItems = Items.Where(i => ValueSelector.Invoke(i).ToLower().Contains(searchValue?.ToLower() ?? string.Empty));
				debounceCTS = null;
			}
		}
	}
}
