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
    public enum MenuAnchorPosition
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

    public class BaseMenu : BaseMatDomComponent
    {
        [Inject] private IJSRuntime jsRuntime { get; set; }

        [Parameter] public RenderFragment ChildContent { get; set; }
        [Parameter] public ForwardRef TargetForwardRef { get; set; }
        [Parameter] public MenuAnchorPosition Position { get; set; } = MenuAnchorPosition.BOTTOM_LEFT;
        [Parameter] public bool FlipCornerHorizontally { get; set; }
        [Parameter] public bool IsOpen { get; set; }
        [Parameter] public EventCallback<bool> IsOpenChanged { get; set; }

        private DotNetObjectReference<BaseMenu> jsHelper;

        public ElementReference AnchorElement;

        public BaseMenu()
        {
            jsHelper = DotNetObjectReference.Create(this);
            ClassMapper.Add("mdc-menu mdc-menu-surface");
        }

        public async Task SetAnchorElementAsync(ElementReference anchorElement)
        {
            AnchorElement = anchorElement;

            await JsInvokeAsync<object>("matBlazor.matMenu.setAnchorElement", Ref, AnchorElement);
        }

        public async Task OpenAsync(ElementReference anchorElement)
        {
            IsOpen = true;

            AnchorElement = anchorElement;

            await JsInvokeAsync<object>("matBlazor.matMenu.setAnchorElement", Ref, AnchorElement);
            await JsInvokeAsync<object>("matBlazor.matMenu.open", Ref);

            await IsOpenChanged.InvokeAsync(IsOpen);
        }

        public async Task CloseAsync()
        {
            IsOpen = false;
            await JsInvokeAsync<object>("matBlazor.matMenu.close", Ref);

            await IsOpenChanged.InvokeAsync(IsOpen);
        }

        public async Task OpenAsync()
        {
            IsOpen = true;
            await JsInvokeAsync<object>("matBlazor.matMenu.setAnchorElement", Ref, TargetForwardRef.Current);
            await JsInvokeAsync<object>("matBlazor.matMenu.open", Ref);

            await IsOpenChanged.InvokeAsync(IsOpen);
        }
        public async Task SetState(bool open)
        {
            IsOpen = open;
            await JsInvokeAsync<object>("matBlazor.matMenu.setAnchorElement", Ref, TargetForwardRef.Current);
            await JsInvokeAsync<object>("matBlazor.matMenu.setState", Ref, open);

            await IsOpenChanged.InvokeAsync(IsOpen);
        }

        protected async override Task OnFirstAfterRenderAsync()
        {
            await base.OnFirstAfterRenderAsync();
            await JsInvokeAsync<object>("matBlazor.matMenu.init", Ref);
            await JSIntropMethods.InitMenu(jsRuntime, jsHelper, Ref, Position, FlipCornerHorizontally);
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
