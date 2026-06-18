using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace CardWallet.Api.Middleware;

public class SubdomainRoutingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<SubdomainRoutingMiddleware> _logger;

    public SubdomainRoutingMiddleware(RequestDelegate next, ILogger<SubdomainRoutingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var host = context.Request.Host.Host.ToLowerInvariant();
        var path = context.Request.Path.Value?.ToLowerInvariant() ?? "";

        // Check if request is to the Admin subdomain
        // We support app.mmohub.xyz and app.localhost (for local development)
        bool isAdminSubdomain = host.StartsWith("app.") || host.Equals("app.localhost");

        if (isAdminSubdomain)
        {
            // Skip rewriting for APIs, Swagger, static files and direct requests with extensions
            bool isResourceOrApi = path.StartsWith("/api") || 
                                   path.StartsWith("/swagger") || 
                                   path.StartsWith("/assets") || 
                                   path.StartsWith("/lib") || 
                                   path.StartsWith("/_framework") || 
                                   path.StartsWith("/_vs") || 
                                   path.Contains(".");

            if (!isResourceOrApi)
            {
                if (path == "/auth-sso")
                {
                    context.Request.Path = "/admin/sso";
                }
                else if (path == "/login" || path == "/admin/login")
                {
                    context.Request.Path = "/admin/login";
                }
                else
                {
                    // Rewrite / -> /admin, /users -> /admin/users, etc.
                    context.Request.Path = "/admin" + (path == "/" ? "" : path);
                }
                
                _logger.LogInformation("Internal rewrite: Host {Host} Request {Path} -> {NewPath}", host, path, context.Request.Path);
            }
        }
        else
        {
            // On main domain: block any direct access to routes starting with /admin or /myadmin
            // but allow /api/admin/... to pass since it is an AJAX call from browser
            bool isForbiddenAdminPath = (path.StartsWith("/admin") || path.StartsWith("/myadmin")) && !path.StartsWith("/api");

            if (isForbiddenAdminPath)
            {
                _logger.LogWarning("Blocked direct admin access on main domain. Host: {Host}, Path: {Path}", host, path);
                context.Response.StatusCode = StatusCodes.Status404NotFound;
                return;
            }
        }

        await _next(context);
    }
}
