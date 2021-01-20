using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using OpeniT.SMTP.Web.Models;
using OpeniT.SMTP.Web.DataRepositories;
using OpeniT.SMTP.Web.Methods;
using OpeniT.SMTP.Web.ViewModels;

namespace OpeniT.SMTP.Web.Controllers
{
    public class AccountController : Controller
    {
        private readonly IWebHostEnvironment hostingEnvironment;
        private readonly IPortalRepository portalRepository;
        private readonly ILogger<PortalRepository> portalLogger;
        //private readonly IStringLocalizer<AccountController> localizer;
        private readonly SignInManager<ApplicationUser> signInManager;
        private readonly UserManager<ApplicationUser> userManager;
		private readonly RoleManager<IdentityRole> roleManager;

        public AccountController(
            IWebHostEnvironment hostingEnvironment,
            IPortalRepository portalRepository,
            ILogger<PortalRepository> portalLogger,
            //IStringLocalizer<AccountController> localizer,
            SignInManager<ApplicationUser> signInManager,
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole> roleManager)
        {
            this.hostingEnvironment = hostingEnvironment;
            this.portalRepository = portalRepository;
            this.portalLogger = portalLogger;
            //this.localizer = localizer;
            this.signInManager = signInManager;
            this.userManager = userManager;
			this.roleManager = roleManager;
        }

        [HttpGet]
        public IActionResult Logout()
        {
            signInManager.SignOutAsync();
            return RedirectToAction("index", "home");
        }

        #region Login
        [AllowAnonymous]
        public async Task<IActionResult> Login(string returnUrl)
        {
            LoginViewModel model = new LoginViewModel()
            {
                ReturnUrl = returnUrl,
                ExternalLogins = (await signInManager.GetExternalAuthenticationSchemesAsync()).ToList()
            };

            var ExternalLogins = (await signInManager.GetExternalAuthenticationSchemesAsync()).ToList();
            var redirectUrl = Url.Action("ExternalLoginCallback", "Account", new { area = "", ReturnUrl = returnUrl });
            var properties = signInManager.ConfigureExternalAuthenticationProperties(ExternalLogins[0].Name, redirectUrl);
            return new ChallengeResult(ExternalLogins[0].Name, properties);
        }
        [AllowAnonymous]
        public async Task<IActionResult> ExternalLoginCallback(string returnUrl = null, string remoteError = null)
        {
            returnUrl = returnUrl ?? Url.Content("~/");

            if (remoteError != null)
            {
                ModelState.AddModelError(string.Empty, $"Error from external provider: {remoteError}");
                return LocalRedirect(returnUrl);
            }
            
            // Get the login information about the user from the external login provider
            var info = await signInManager.GetExternalLoginInfoAsync();
            
            if (info == null)
            {
                ModelState.AddModelError(string.Empty, "Error loading external login information.");
                return LocalRedirect(returnUrl);
            }

            // Get the claim values
            var email = info.Principal.FindFirstValue(ClaimTypes.Name);
            var displayName = info.Principal.FindFirstValue("name");
            var role = info.Principal.FindFirstValue(ClaimTypes.Role);

            // If the user already has a login (i.e if there is a record in AspNetUserLogins table) then sign-in the user with this external login provider
            var signInResult = await signInManager.ExternalLoginSignInAsync(info.LoginProvider, info.ProviderKey, isPersistent: false, bypassTwoFactor: true);

            if (signInResult.Succeeded)
            {
                await AddToRole(email, role);
                return LocalRedirect(returnUrl);
            }
            // If there is no record in AspNetUserLogins table, the user may not have a local account
            else
            {
                if (email != null)
                {
                    // Create a new user without password if we do not have a user already
                    var user = await this.portalRepository.GetUserByEmail(email) ?? await AddUser(email, displayName);
                    await AddToRole(email, role);

                    // Add a login (i.e insert a row for the user in AspNetUserLogins table)
                    await userManager.AddLoginAsync(user, info);
                    await signInManager.SignInAsync(user, isPersistent: false);
                }
            }
            return LocalRedirect(returnUrl);
        }

        public async Task<ApplicationUser> AddUser(string email, string displayName)
        {
            var user = new ApplicationUser
            {
                UserName = email,
                Email = email,
                DisplayName = displayName
            };

            await this.portalRepository.CreateAdminUser(user);
            return user;
        }

        public async Task<IdentityResult> AddToRole(string email, string role)
        {
            var user = await this.portalRepository.GetUserByEmail(email);

            var result = await AddToRole(user, role);
            if (result.Succeeded)
            {
                var roles = await userManager.GetRolesAsync(user);
                user.Roles = roles;

                try
                {
                    await this.portalRepository.UpdateUser(user);
                    await this.portalRepository.SaveChangesAsync();
                } 
                catch
                { 

                }
                
            }
            return result;
        }

        public async Task<IdentityResult> AddToRole(ApplicationUser user, string role)
        {
            if (role == null)
            {
                role = "User-Internal";
            }

            var identityRole = await roleManager.FindByNameAsync(role);
            if (identityRole == null)
            {
                await roleManager.CreateAsync(new IdentityRole(role));
            }

            var roles = await userManager.GetRolesAsync(user);

            foreach (var item in roles)
            {
                if (item == "Administrator" || item == "Developer" || item == "User-Internal")
                {
                    await userManager.RemoveFromRoleAsync(user, item);
                }
            }

            return await userManager.AddToRoleAsync(user, role);
        }

        [HttpPost]
        public async Task<IdentityResult> AddToExternalUser(ApplicationUser user)
        {
            var role = "User-External";

            var identityRole = await roleManager.FindByNameAsync(role);
            if (identityRole == null)
            {
                await roleManager.CreateAsync(new IdentityRole(role));
            }

            var roles = await userManager.GetRolesAsync(user);

            foreach (var item in roles)
            {
                await userManager.RemoveFromRoleAsync(user, item);
            }

            return await userManager.AddToRoleAsync(user, role);
        }

        #endregion Login

        [HttpGet]
        public IActionResult AccessDenied()
        {                        
            return View();
        }
    }

    public class LoginViewModel
    {
        [Required]
        [Display(Name = "Username")]
        public string UserName { get; set; }

        [Required]
        [DataType(DataType.Password)]
        public string Password { get; set; }

        [Display(Name = "Remember Me")]
        public bool Remember { get; set; }

        public string ReturnUrl { get; set; }

        public IList<AuthenticationScheme> ExternalLogins { get; set; }
    }
}
