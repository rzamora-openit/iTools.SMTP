using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Mvc;
using OpeniT.SMTP.Web.Models;
using OpeniT.SMTP.Web.DataRepositories;
using OpeniT.SMTP.Web.Methods;
using System.Drawing;
using System.Drawing.Imaging;

namespace OpeniT.SMTP.Web.Controllers
{
	[Authorize]
	public class DownloadController : Controller
	{
		private readonly IDataRepository portalRepository;
		private readonly ILogger<DataRepository> portalLogger;
		private readonly Microsoft.Extensions.Configuration.IConfiguration configuration;
		//private readonly IStringLocalizer<AccountController> localizer;
		private readonly SignInManager<ApplicationUser> signInManager;
		private IWebHostEnvironment webHostEnvironment { get; set; }

		public DownloadController(
			IDataRepository portalRepository,
			ILogger<DataRepository> portalLogger,
			Microsoft.Extensions.Configuration.IConfiguration configuration,
			//IStringLocalizer<AccountController> localizer,
			SignInManager<ApplicationUser> signInManager,
			IWebHostEnvironment webHostEnvironment)
		{
			this.portalRepository = portalRepository;
			this.portalLogger = portalLogger;
			this.configuration = configuration;
			//this.localizer = localizer;
			this.signInManager = signInManager;
			this.webHostEnvironment = webHostEnvironment;
		}

		[Route("/file/image/me")]
		public async Task<IActionResult> GetMyImage()
		{
			try
			{
				return await this.GetUserImage(this.HttpContext?.User?.Identity?.Name);

			}
			catch (Exception ex)
			{
				return this.BadRequest($"\"error\" : {ex.Message}");
			}
		}

		[Route("/file/image/{email}")]
		public async Task<IActionResult> GetUserImage(string email)
		{
			try
			{
				var user = await this.portalRepository.GetUserByEmail(email);

				string userInitials = string.Empty;
				var names = user?.DisplayName?.Split(" ");
				if (names != null)
				{
					foreach (var name in names)
					{
						if (!string.IsNullOrWhiteSpace(name))
						{
							userInitials += name[0];
						}
					}
				}

				var image = this.CreateImageFromText(userInitials, new Font("Arial", 40), Color.Black, Color.FromArgb(245, 245, 245));
				using (MemoryStream ms = new MemoryStream())
				{
					image.Save(ms, ImageFormat.Jpeg);
					return new FileContentResult(ms.ToArray(), "image/jpeg");
				}

			}
			catch (Exception ex)
			{
				return this.BadRequest($"\"error\" : {ex.Message}");
			}
		}

		public System.Drawing.Image CreateImageFromText(string text, Font font, Color textColor, Color backColor)
		{
			//first, create a dummy bitmap just to get a graphics object
			System.Drawing.Image img = new Bitmap(1, 1);
			Graphics drawing = Graphics.FromImage(img);

			//measure the string to see how big the image needs to be
			SizeF textSize = drawing.MeasureString(text, font);

			//free up the dummy image and old graphics object
			img.Dispose();
			drawing.Dispose();

			//create a new image of the right size
			img = new Bitmap((int)(textSize.Width + 100), (int)(textSize.Width + 100));

			drawing = Graphics.FromImage(img);

			//paint the background
			drawing.Clear(backColor);

			//create a brush for the text
			Brush textBrush = new SolidBrush(textColor);

			drawing.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAlias;
			drawing.DrawString(text, font, textBrush, 50, 50 + ((textSize.Width - textSize.Height) / 2));

			drawing.Save();

			textBrush.Dispose();
			drawing.Dispose();

			return img;
		}
	}
}