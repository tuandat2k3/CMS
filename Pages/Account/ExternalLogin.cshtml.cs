using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace CMS.Pages.Account
{
    public class ExternalLoginModel : PageModel
    {
        public async Task OnGetAsync(string provider)
        {
            var redirectUrl = Url.Page("./ExternalLoginCallback");
            var properties = new AuthenticationProperties { RedirectUri = redirectUrl };
            await HttpContext.ChallengeAsync(provider, properties);
        }
    }
}
