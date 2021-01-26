using HtmlAgilityPack;
using Microsoft.Extensions.Configuration;
using OpeniT.SMTP.Web.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Net.Mime;
using System.Threading.Tasks;

namespace OpeniT.SMTP.Web.Methods
{
	public class SMTPMethods
	{
		private readonly IConfiguration configuration;

		public SMTPMethods(IConfigurationRoot configuration)
		{
			this.configuration = configuration;
		}

		public async Task<bool> SendMail(MailMessage mailMessage)
		{
			try
			{
				using var smtpClient = new SmtpClient()
				{
					Host = this.configuration.GetValue<string>("SMTP:Host"),
					Port = this.configuration.GetValue<int>("SMTP:Port"),
					EnableSsl = true,
					DeliveryMethod = SmtpDeliveryMethod.Network,
					UseDefaultCredentials = false,
					Credentials = new NetworkCredential()
					{
						UserName = this.configuration.GetValue<string>("SMTP:Username"),
						Password = this.configuration.GetValue<string>("SMTP:Password"),
					}
				};

				await smtpClient.SendMailAsync(mailMessage);

				return true;
			}
			catch (Exception ex)
			{
				Console.WriteLine(ex.Message);
			}

			return false;
		}

		public async Task<bool> SendMail(SmtpMail mail, bool isBodyHTML = true)
		{
			try
			{
				var mailMessage = new MailMessage();
				mailMessage.From = new MailAddress(mail?.From?.Address, mail?.From?.DisplayName);
				mailMessage.Subject = mail?.Subject;

				if (isBodyHTML)
				{
					var htmlDoc = new HtmlDocument();
					htmlDoc.LoadHtml(mail?.Body);

					if (!string.Equals(htmlDoc.DocumentNode.FirstChild.Name, "body"))
					{
						var bodyNode = HtmlNode.CreateNode("<body></body>");
						bodyNode.AppendChildren(htmlDoc.DocumentNode.ChildNodes);

						htmlDoc.DocumentNode.RemoveAll();
						htmlDoc.DocumentNode.AppendChild(bodyNode);
					}

					if (!string.Equals(htmlDoc.DocumentNode.FirstChild.Name, "html"))
					{
						var htmlNode = HtmlNode.CreateNode(@"<!DOCTYPE html PUBLIC "" -//W3C//DTD XHTML 1.0 Transitional //EN"" ""http://www.w3.org/TR/xhtml1/DTD/xhtml1-transitional.dtd""><html xmlns=""http://www.w3.org/1999/xhtml"" xmlns:o=""urn:schemas-microsoft-com:office:office"" xmlns:v=""urn:schemas-microsoft-com:vml""></html>");
						htmlNode.AppendChildren(htmlDoc.DocumentNode.ChildNodes);

						htmlDoc.DocumentNode.RemoveAll();
						htmlDoc.DocumentNode.AppendChild(htmlNode);
					}

					if (!htmlDoc.DocumentNode.Descendants("head").Any())
					{
						var headNode = HtmlNode.CreateNode("<head></head>");
						var documentNode = htmlDoc.DocumentNode;

						MoveDescendants(ref documentNode, ref headNode, "//title");
						MoveDescendants(ref documentNode, ref headNode, "//style");
						MoveDescendants(ref documentNode, ref headNode, "//base");
						MoveDescendants(ref documentNode, ref headNode, "//link");
						MoveDescendants(ref documentNode, ref headNode, "//meta");

						var htmlNode = htmlDoc.DocumentNode.SelectSingleNode("//html");
						htmlNode.PrependChild(headNode);
					}

					AlternateView avHtml = AlternateView.CreateAlternateViewFromString
						(htmlDoc.DocumentNode.InnerHtml, null, MediaTypeNames.Text.Html);
					var images = htmlDoc.DocumentNode.Descendants("img");
					foreach (HtmlNode image in images ?? Enumerable.Empty<HtmlNode>())
					{
						string src = image.GetAttributeValue("src", null);

						if (src?.StartsWith("data:") != true)
						{
							if (Uri.TryCreate(src, UriKind.Absolute, out Uri srcUri))
							{
								if (srcUri.Scheme != Uri.UriSchemeHttp && srcUri.Scheme != Uri.UriSchemeHttps)
								{
									src = src.StartsWith("//") ? $"http:{src}" : $"http://{src}";
								}

								using (WebClient client = new WebClient())
								using (Stream stream = await client.OpenReadTaskAsync(src))
								{
									LinkedResource linkedResource = new LinkedResource(contentStream: stream, mediaType: MediaTypeNames.Image.Jpeg);
									linkedResource.ContentId = Guid.NewGuid().ToString();
									avHtml.LinkedResources.Add(linkedResource);

									image.SetAttributeValue("src", src);
								}
							}
						}
					}

					mailMessage.AlternateViews.Add(avHtml);
					mailMessage.Body = htmlDoc.DocumentNode.InnerHtml;
					mailMessage.IsBodyHtml = true;
				}
				else
				{
					mailMessage.Body = mail?.Body;
					mailMessage.IsBodyHtml = false;
				}

				foreach (var mailTo in mail?.To ?? Enumerable.Empty<SmtpMailAddress>())
				{
					mailMessage.To.Add(new MailAddress(mailTo?.Address, mailTo?.DisplayName));
				}

				foreach (var mailCC in mail?.CC ?? Enumerable.Empty<SmtpMailAddress>())
				{
					mailMessage.CC.Add(new MailAddress(mailCC?.Address, mailCC?.DisplayName));
				}

				return await this.SendMail(mailMessage);
			}
			catch (Exception ex)
			{
				Console.WriteLine(ex.Message);
			}

			return false;
		}

		private void MoveDescendants(ref HtmlNode fromNode, ref HtmlNode toNode, string selector)
		{
			var descendants = fromNode.SelectNodes(selector);
			if (descendants == null)
			{
				return;
			}

			toNode.AppendChildren(descendants);
		}
	}
}
