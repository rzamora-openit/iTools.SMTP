using System;
using System.Net.Http;
using System.Security.Authentication;
using System.Text;
using System.Threading.Tasks;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Clients.ActiveDirectory;

using OpeniT.SMTP.Web.Models;

using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;

namespace OpeniT.SMTP.Web.Helpers
{
	public class AzureHelper
	{
		private readonly IConfigurationRoot config;

		private readonly ILogger<AzureHelper> logger;

		public AzureHelper(IConfigurationRoot config, ILogger<AzureHelper> logger)
		{
			this.config = config;
			this.logger = logger;
		}

		public async Task<List<AzureProfile>> GetUsers(string query)
		{
			List<AzureProfile> profiles = null;
			var apiVersion = this.config["Microsoft:GraphApiVersion"];
			var tenantName = this.config["Microsoft:TenantName"];
			var authString = this.config["Microsoft:Authority"];
			var clientId = this.config["Microsoft:ClientId"];
			var clientSecret = this.config["Microsoft:ClientSecret"];
			var graphUri = this.config["Microsoft:GraphUri"];

			try
			{
				var clientCredential = new ClientCredential(clientId, clientSecret);
				var authenticationContext = new AuthenticationContext(authString, false);
				var authenticationResult = await authenticationContext.AcquireTokenAsync(graphUri, clientCredential);
				var token = authenticationResult.AccessToken;

				using (var client = new HttpClient())
				{
					client.BaseAddress = new Uri(graphUri);
					client.DefaultRequestHeaders.Add("Authorization", "Bearer " + token);

					var uri = $"{apiVersion}/{tenantName}/users{query}";

					var result = await client.GetAsync(uri);
					if (!result.IsSuccessStatusCode) throw new Exception($"{result.Content.ReadAsStringAsync().Result}");

					var content = await result.Content.ReadAsStringAsync();
					var jArray = JObject.Parse(content).Value<JArray>("value");
					profiles = jArray.ToObject<List<AzureProfile>>();

					//if (string.IsNullOrEmpty(query))
					//{
					//	var thumbnailUri = $"{apiVersion}/{tenantName}/users/{owner}/photo/$value";
					//	result = await client.GetAsync(thumbnailUri);
					//	if (result != null && profile != null && result.IsSuccessStatusCode)
					//	{
					//		profile.ThumbnailContentType = result.Content.Headers.ContentType.MediaType;
					//		profile.ThumbnailContent = await result.Content.ReadAsByteArrayAsync();
					//	}
					//}
				}
			}
			catch (AuthenticationException ex)
			{
				this.logger.LogCritical($"Acquiring a token failed with the following error: {ex.Message}");
				if (ex.InnerException != null) this.logger.LogError($"Error detail: {ex.InnerException.Message}");
			}
			catch (Exception ex)
			{
				this.logger.LogError($"Error getting users information: {ex}");
			}

			return profiles;
		}

		public async Task<AzureProfile> GetUser(string owner, string query)
		{
			AzureProfile profile = null;
			var apiVersion = this.config["Microsoft:GraphApiVersion"];
			var tenantName = this.config["Microsoft:TenantName"];
			var authString = this.config["Microsoft:Authority"];
			var clientId = this.config["Microsoft:ClientId"];
			var clientSecret = this.config["Microsoft:ClientSecret"];
			var graphUri = this.config["Microsoft:GraphUri"];

			try
			{
				var clientCredential = new ClientCredential(clientId, clientSecret);
				var authenticationContext = new AuthenticationContext(authString, false);
				var authenticationResult = await authenticationContext.AcquireTokenAsync(graphUri, clientCredential);
				var token = authenticationResult.AccessToken;

				using (var client = new HttpClient())
				{
					client.BaseAddress = new Uri(graphUri);
					client.DefaultRequestHeaders.Add("Authorization", "Bearer " + token);

					var uri = $"{apiVersion}/{tenantName}/users/{owner}{query}";

					var result = await client.GetAsync(uri);
					if (!result.IsSuccessStatusCode) throw new Exception($"{result.Content.ReadAsStringAsync().Result}");

					var content = await result.Content.ReadAsStringAsync();
					profile = JsonConvert.DeserializeObject<AzureProfile>(content);

					if (string.IsNullOrEmpty(query))
					{
						var thumbnailUri = $"{apiVersion}/{tenantName}/users/{owner}/photo/$value";
						result = await client.GetAsync(thumbnailUri);
						if (result != null && profile != null && result.IsSuccessStatusCode)
						{
							profile.ThumbnailContentType = result.Content.Headers.ContentType.MediaType;
							profile.ThumbnailContent = await result.Content.ReadAsByteArrayAsync();
						}
					}
				}
			}
			catch (AuthenticationException ex)
			{
				this.logger.LogCritical($"Acquiring a token failed with the following error: {ex.Message}");
				if (ex.InnerException != null) this.logger.LogError($"Error detail: {ex.InnerException.Message}");
			}
			catch (Exception ex)
			{
				this.logger.LogError($"Error getting user information: {ex}");
			}

			return profile;
		}
	}
}