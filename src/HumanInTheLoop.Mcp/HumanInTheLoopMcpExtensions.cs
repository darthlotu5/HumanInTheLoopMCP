using HumanInTheLoop.Mcp.Abstractions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace HumanInTheLoop.Mcp;

public static class HumanInTheLoopMcpExtensions
{
    /// <summary>
    /// Registers the Human-in-the-Loop MCP server, the <c>ask_user</c> tool, and the config-based
    /// single-tenant recipient resolver. Register one or more <see cref="IHumanChannel"/> to deliver
    /// questions (e.g. <c>services.AddSingleton&lt;IHumanChannel, TelegramChannel&gt;()</c>).
    /// </summary>
    public static IServiceCollection AddHumanInTheLoopMcp(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<HumanInTheLoopOptions>(configuration.GetSection(HumanInTheLoopOptions.SectionName));
        services.AddHttpContextAccessor();
        services.TryAddSingleton<IRecipientResolver, ConfigRecipientResolver>();

        services.AddMcpServer()
            .WithHttpTransport()
            .WithTools<HumanTools>();

        return services;
    }

    /// <summary>
    /// Maps the MCP endpoint at <paramref name="pattern"/> (default <c>/mcp</c>). Requests are gated by the
    /// <see cref="IRecipientResolver"/>: a token that resolves to a recipient is authorized, and the recipient
    /// is stashed for the <c>ask_user</c> tool.
    /// </summary>
    public static WebApplication MapHumanInTheLoopMcp(this WebApplication app, string pattern = "/mcp")
    {
        app.Use(async (context, next) =>
        {
            if (context.Request.Path.StartsWithSegments(pattern))
            {
                var token = ExtractToken(context);
                var resolver = context.RequestServices.GetRequiredService<IRecipientResolver>();
                var recipient = token is null ? null : await resolver.ResolveAsync(token, context.RequestAborted);
                if (recipient is null)
                {
                    context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                    return;
                }

                context.Items[HumanTools.RecipientItemKey] = recipient;
            }

            await next();
        });

        app.MapMcp(pattern);
        return app;
    }

    private static string? ExtractToken(HttpContext http)
    {
        var authorization = http.Request.Headers.Authorization.ToString();
        if (authorization.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
            return authorization["Bearer ".Length..].Trim();

        var header = http.Request.Headers["X-Access-Token"].ToString();
        if (!string.IsNullOrEmpty(header))
            return header;

        var query = http.Request.Query["access_token"].ToString();
        return string.IsNullOrEmpty(query) ? null : query;
    }
}
