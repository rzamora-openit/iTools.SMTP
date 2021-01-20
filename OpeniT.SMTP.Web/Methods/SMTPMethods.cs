using Microsoft.Extensions.Configuration;
using OpeniT.SMTP.Web.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;

namespace OpeniT.SMTP.Web.Methods
{
	public class SMTPMethods
	{
		private readonly IConfiguration configuration;
		private readonly SmtpClient smtpClient;

		public SMTPMethods(IConfigurationRoot configuration)
		{
			this.configuration = configuration;
			this.smtpClient = new SmtpClient()
			{
				Host = this.configuration.GetValue<string>("SMTP:Host"),
				Port = this.configuration.GetValue<int>("SMTP:Port"),
				EnableSsl = true,
				DeliveryMethod = SmtpDeliveryMethod.Network,
				UseDefaultCredentials = true
			};
		}

		public async Task<bool> SendMail(MailMessage mailMessage)
		{
			try
			{
				await this.smtpClient.SendMailAsync(mailMessage);

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
				var mailMessage = new MailMessage()
				{
					From = new MailAddress(mail?.From?.Address, mail?.From?.DisplayName),
					Subject = mail?.Subject,
					Body = mail?.Body,
					IsBodyHtml = true
				};

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
	}
}
