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
	public enum DropdownAnchorCorner
	{
		BOTTOM_END = 13,
		BOTTOM_LEFT = 1,
		BOTTOM_RIGHT = 5,
		BOTTOM_START = 9,
		TOP_END = 12,
		TOP_LEFT = 0,
		TOP_RIGHT = 4,
		TOP_START = 8
	};

	public partial class Dropdown : ComponentBase
	{
		[Inject] private IJSRuntime jsRuntime { get; set; }

		[Parameter] public string Class { get; set; }
		[Parameter] public string Style { get; set; }
		[Parameter] public DropdownAnchorCorner Position { get; set; } = DropdownAnchorCorner.BOTTOM_LEFT;
		[Parameter] public bool FlipCornerHorizontally { get; set; }
		[Parameter] public RenderFragment ButtonContent { get; set; }
		[Parameter] public RenderFragment<MatMenu> MenuContent { get; set; }
		[Parameter] public bool IsOpen { get; set; }
		[Parameter] public EventCallback<bool> IsOpenChanged { get; set; }
		[Parameter] public bool Disabled { get; set; }
		[Parameter] public bool MenuOverlayScrollbarsEnabled { get; set; } = false;
		[Parameter] public string ButtonClass { get; set; }
		[Parameter] public string ButtonStyle { get; set; }
		[Parameter] public string MenuClass { get; set; }
		[Parameter] public string MenuStyle { get; set; }

		private DotNetObjectReference<Dropdown> jsHelper;
		private ElementReference anchorRef;
		private MatMenu menu;


		public Dropdown()
		{
			jsHelper = DotNetObjectReference.Create(this);
		}

		public async Task Toggle()
		{
			if (!Disabled)
			{
				IsOpen = !IsOpen;
				if (IsOpen)
				{
					await this.menu.OpenAsync(anchorRef);
				}
				else
				{
					await this.menu.CloseAsync();
				}
			}
			else
			{
				IsOpen = false;
				await this.menu.CloseAsync();
			}

			await this.NotifyIsOpenChanged(IsOpen);
		}

		protected override async Task OnAfterRenderAsync(bool firstRender)
		{
			if (firstRender)
			{
				await JSIntropMethods.InitDropdown(jsRuntime, jsHelper, menu.Ref, anchorRef, Position, FlipCornerHorizontally);
			}
		}

		[JSInvokable]
		public Task NotifyIsOpenChanged(bool isOpen)
		{
			IsOpen = isOpen;
			StateHasChanged();
			return IsOpenChanged.InvokeAsync(IsOpen);
		}
	}
}
