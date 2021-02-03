using MatBlazor;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using OpeniT.SMTP.Web.Methods;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace OpeniT.SMTP.Web.Pages.Shared
{
	public partial class Dropdown : ComponentBase
	{
		[Parameter] public string Class { get; set; }
		[Parameter] public string Style { get; set; }
		[Parameter] public MenuAnchorPosition Position { get; set; } = MenuAnchorPosition.BOTTOM_LEFT;
		[Parameter] public bool FlipCornerHorizontally { get; set; }
		[Parameter] public RenderFragment ButtonContent { get; set; }
		[Parameter] public RenderFragment<Menu> MenuContent { get; set; }
		[Parameter] public bool IsOpen { get; set; }
		[Parameter] public EventCallback<bool> IsOpenChanged { get; set; }
		[Parameter] public bool Disabled { get; set; }
		[Parameter] public bool MenuOverlayScrollbarsEnabled { get; set; } = false;
		[Parameter] public string ButtonClass { get; set; }
		[Parameter] public string ButtonStyle { get; set; }
		[Parameter] public string MenuClass { get; set; }
		[Parameter] public string MenuStyle { get; set; }

		private ElementReference anchorRef;
		private Menu menu;

		private string classMapper => $"mdc-dropdown {Class} {(IsOpen ? "mdc-dropdown-open" : string.Empty)}";

		public async Task Toggle()
		{
			if (!Disabled && !IsOpen)
			{
				await this.menu.OpenAsync(anchorRef);
			}
			else
			{
				await this.menu.CloseAsync();
			}
		}

		private Task IsOpenIsChanged(bool isOpen)
		{
			if (IsOpen != isOpen)
			{
				IsOpen = isOpen;
				return IsOpenChanged.InvokeAsync(IsOpen);
			}

			return Task.CompletedTask;
		}
	}
}
