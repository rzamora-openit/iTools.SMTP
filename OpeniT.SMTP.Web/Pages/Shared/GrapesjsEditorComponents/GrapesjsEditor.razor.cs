using iTools.Utilities.JsRuntimeStream;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.Extensions.Options;
using Microsoft.JSInterop;
using OpeniT.SMTP.Web.Methods;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;

namespace OpeniT.SMTP.Web.Pages.Shared
{
	public partial class GrapesjsEditor : ComponentBase, IDisposable
	{
		[Inject] private IJSRuntime jsRuntime { get; set; }
		[Inject] private IJSRuntimeStream jsRuntimeStream { get; set; }

		[CascadingParameter] EditContext CascadedEditContext { get; set; } = default!;

		[Parameter] public string Value { get; set; }
		[Parameter] public bool Disabled { get; set; }

		private string CurrentValue;
		private bool CurrentDisabled;

		private bool grapesjsIsInitialized = false;
		private bool isDisposing = false;
		private bool isBusy = false;

		private ElementReference editorElement;

		protected override async Task OnAfterRenderAsync(bool firstRender)
		{
			if (firstRender)
			{
				await this.InitializeGrapesjs();
			}
		}

		protected override async Task OnParametersSetAsync()
		{
			await this.BindState();
		}
		public async Task InitializeGrapesjs()
		{
			if (!grapesjsIsInitialized)
			{
				await JSIntropMethods.InitializeGrapesJs(
						jsRuntime,
						editorElement);

				grapesjsIsInitialized = true;

				await this.BindState();
			}
		}

		public async Task BindState()
		{
			try
			{
				isBusy = true;
				StateHasChanged();

				if (grapesjsIsInitialized)
				{
					if (!string.Equals(CurrentValue, Value))
					{
						CurrentValue = Value;

						await this.SetValue(Value);
					}

					if (CurrentDisabled != Disabled)
					{
						CurrentDisabled = Disabled;

						await JSIntropMethods.EnableGrapesJsEditor(jsRuntime, editorElement, !Disabled);
					}
				}
			}
			catch (Exception ex)
			{
				Console.WriteLine(ex.Message);
			}
			finally
			{
				isBusy = false;
				StateHasChanged();
			}
		}


		public async Task<string> GetValue(CancellationTokenSource cts = default)
		{
			try
			{
				isBusy = true;
				StateHasChanged();

				using var stream = await jsRuntimeStream.InvokeReadStream("window.GrapesjsFunctions.getValue", cts.Token, editorElement);
				using var memoryStream = new MemoryStream();
				await stream.CopyToAsync(memoryStream);

				return System.Text.Encoding.UTF8.GetString(memoryStream.ToArray());
			}
			catch (Exception ex)
			{
				Console.WriteLine(ex.Message);
			}
			finally
			{
				isBusy = false;
				StateHasChanged();
			}

			return null;
		}

		public async Task SetValue(string value)
		{
			try
			{
				isBusy = true;
				StateHasChanged();

				await JSIntropMethods.GrapesJsEditorSetValue(jsRuntime, editorElement, value);
			}
			catch (Exception ex)
			{
				Console.WriteLine(ex.Message);
			}
			finally
			{
				isBusy = false;
				StateHasChanged();
			}
		}

		protected virtual void Dispose(bool disposing)
		{
			isDisposing = disposing;
			if (isDisposing)
			{
				JSIntropMethods.DestroyGrapesJsEditor(jsRuntime, editorElement);
			}
		}

		void IDisposable.Dispose()
		{
			Dispose(disposing: true);
		}
	}
}
