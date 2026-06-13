using Microsoft.AspNetCore.Mvc;
using System.Linq;

namespace CardWallet.Api.Controllers
{
    [ApiController]
    [Route("api/localization")]
    public class LocalizationController : ControllerBase
    {
        [HttpGet("detect")]
        public IActionResult DetectLanguage()
        {
            // Priority: 1. X-Forwarded-For (for real IP behind proxy), 2. Accept-Language header
            // This is a simplified mock. A real implementation would use a GeoIP database.
            
            var acceptLanguage = Request.Headers["Accept-Language"].ToString();
            var lang = "vi"; // Default to Vietnamese
            var country = "VN";

            if (!string.IsNullOrEmpty(acceptLanguage))
            {
                var firstLang = acceptLanguage.Split(',').FirstOrDefault()?.Split(';').FirstOrDefault()?.ToLower();
                if (firstLang != null)
                {
                    if (firstLang.StartsWith("en")) { lang = "en"; country = "US"; }
                    else if (firstLang.StartsWith("zh")) { lang = "zh"; country = "CN"; }
                }
            }

            return Ok(new { language = lang, country });
        }
    }
}