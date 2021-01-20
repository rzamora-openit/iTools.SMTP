using Blazored.LocalStorage;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.JSInterop;
using OpeniT.SMTP.Web.Pages.Shared;
using OpeniT.SMTP.Web.ViewModels;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace OpeniT.SMTP.Web.Methods
{
    public static class JSIntropMethods
    {
        public static readonly string SiteFunctionsPrefix = "SiteFunctions";
        public static readonly string AdminSiteFunctionsPrefix = "AdminSiteFunctions";
        public static readonly string QuillFunctionsPrefix = "QuillFunctions";
        public static readonly string GrapesJsFunctionsPrefix = "GrapesjsFunctions";

        public static readonly string SiteCascadingValueKey = "siteCascadingValue";

        #region Site
        internal static ValueTask SiteInit(
            IJSRuntime jsRuntime)
        {
            return jsRuntime.InvokeVoidAsync($"{SiteFunctionsPrefix}.init");
        }
        internal static ValueTask InitDropdown(
            IJSRuntime jsRuntime,
            DotNetObjectReference<Dropdown> jsHelper,
            ElementReference menuElement,
            ElementReference anchorElement,
            DropdownAnchorCorner anchorCorner,
            bool flipCornerHorizontally)
        {
            return jsRuntime.InvokeVoidAsync($"{SiteFunctionsPrefix}.initDropdown", jsHelper, menuElement, anchorCorner, anchorCorner, flipCornerHorizontally);
        }
        internal static ValueTask<string> InitResizableTable(
            IJSRuntime jsRuntime,
            ElementReference tableElement)
        {
            return jsRuntime.InvokeAsync<string>($"{SiteFunctionsPrefix}.initResizableTable", tableElement);
        }
        internal static ValueTask UpdateMatBlazorTheme(
            IJSRuntime jsRuntime,
            string matBlazorStyle)
        {
            return jsRuntime.InvokeVoidAsync($"{SiteFunctionsPrefix}.updateMatBlazorTheme", matBlazorStyle);
        }
        internal static ValueTask CreateOverlayScrollbars(
            IJSRuntime jsRuntime,
            ElementReference element,
            string className)
        {
            return jsRuntime.InvokeVoidAsync($"{SiteFunctionsPrefix}.createOverlayScrollbars", element, className);
        }
        internal static ValueTask DestroyOverlayScrollbars(
            IJSRuntime jsRuntime,
            ElementReference element)
        {
            return jsRuntime.InvokeVoidAsync($"{SiteFunctionsPrefix}.destroyOverlayScrollbars", element);
        }
        internal static ValueTask<double> GetElementWidth(
            IJSRuntime jsRuntime,
            ElementReference element)
        {
            return jsRuntime.InvokeAsync<double>($"{SiteFunctionsPrefix}.getElementWidth", element);
        }
        internal static ValueTask<double> GetElementHeight(
            IJSRuntime jsRuntime,
            ElementReference element)
        {
            return jsRuntime.InvokeAsync<double>($"{SiteFunctionsPrefix}.getElementHeight", element);
        }
        internal static ValueTask<double> GetInnerHTML(
            IJSRuntime jsRuntime,
            ElementReference element)
        {
            return jsRuntime.InvokeAsync<double>($"{SiteFunctionsPrefix}.getInnerHTML", element);
        }
        internal static ValueTask LoadPDFViewer(
            IJSRuntime jsRuntime,
            string base64String,
            string fileName,
            bool HideControls)
        {
            return jsRuntime.InvokeVoidAsync($"{SiteFunctionsPrefix}.loadPDFViewer", base64String, fileName, HideControls);
        }
        internal static ValueTask LoadPDFViewerCanvas(
            IJSRuntime jsRuntime,
            ElementReference canvas,
            string base64String)
        {
            return jsRuntime.InvokeVoidAsync($"{SiteFunctionsPrefix}.loadPDFViewerCanvas", canvas, base64String);
        }
        internal static ValueTask ScrollIntoView(
            IJSRuntime jsRuntime,
            ElementReference element)
        {
            return jsRuntime.InvokeVoidAsync($"{SiteFunctionsPrefix}.scrollIntoView", element);
        }
        internal static ValueTask ScrollToHighest(
            IJSRuntime jsRuntime,
            IEnumerable<ElementReference> elements)
        {
            return jsRuntime.InvokeVoidAsync($"{SiteFunctionsPrefix}.scrollToHighest", elements);
        }
        internal static ValueTask OpenFileSelector(
            IJSRuntime jsRuntime,
            ElementReference inputContainerElement)
        {
            return jsRuntime.InvokeVoidAsync($"{SiteFunctionsPrefix}.OpenFileSelector", inputContainerElement);
        }
        internal static ValueTask<string> GetUrlParam(
            IJSRuntime jsRuntime,
            string paramName)
        {
            return jsRuntime.InvokeAsync<string>($"{SiteFunctionsPrefix}.getUrlParam", paramName);
        }
        internal static ValueTask<string> OpenUrl(
            IJSRuntime jsRuntime,
            string url,
            string windowName,
            string winodwFeatures = null)
        {
            return jsRuntime.InvokeAsync<string>($"{SiteFunctionsPrefix}.openUrl", url, windowName, winodwFeatures);
        }
        internal static ValueTask<string> ReplaceState(
            IJSRuntime jsRuntime,
            object stateObject,
            string title,
            string url)
        {
            return jsRuntime.InvokeAsync<string>($"{SiteFunctionsPrefix}.replaceState", stateObject, title, url);
        }
        #endregion Site

        #region AdminSite
        internal static ValueTask AdminSiteInit(
            IJSRuntime jsRuntime)
        {
            return jsRuntime.InvokeVoidAsync($"{AdminSiteFunctionsPrefix}.init");
        }
        internal static ValueTask InitFileUploadContainer(
            IJSRuntime jsRuntime,
            ElementReference fileUploadContainerElement)
        {
            return jsRuntime.InvokeVoidAsync($"{AdminSiteFunctionsPrefix}.initFileUploadContainer", fileUploadContainerElement);
        }
        #endregion AdminSite

        #region GrapesjsEditor
        internal static ValueTask InitializeGrapesJs(
            IJSRuntime jsRuntime,
            ElementReference editorElement)
        {
            return jsRuntime.InvokeVoidAsync($"{GrapesJsFunctionsPrefix}.init", editorElement);
        }
        internal static ValueTask GrapesJsEditorSetValue(
            IJSRuntime jsRuntime,
            ElementReference editorElement,
            string value)
        {
            return jsRuntime.InvokeVoidAsync($"{GrapesJsFunctionsPrefix}.setValue", editorElement, value);
        }
        internal static ValueTask EnableGrapesJsEditor(
            IJSRuntime jsRuntime,
            ElementReference editorElement,
            bool mode)
        {
            return jsRuntime.InvokeVoidAsync($"{GrapesJsFunctionsPrefix}.enable", editorElement, mode);
        }
        internal static ValueTask DestroyGrapesJsEditor(
            IJSRuntime jsRuntime,
            ElementReference editorElement)
        {
            return jsRuntime.InvokeVoidAsync($"{GrapesJsFunctionsPrefix}.destroy", editorElement);
        }
        #endregion GrapesjsEditor

        #region LocalStorage
        internal static ValueTask<SiteCascadingValueViewModel> GetSiteCascadingValue(
           ILocalStorageService localStorage)
        {
            return localStorage.GetItemAsync<SiteCascadingValueViewModel>(SiteCascadingValueKey);
        }
        internal static ValueTask SetSiteCascadingValue(
           ILocalStorageService localStorage,
           SiteCascadingValueViewModel siteCascadingValue)
        {
            return localStorage.SetItemAsync(SiteCascadingValueKey, siteCascadingValue);
        }
        #endregion LocalStorage
    }
}