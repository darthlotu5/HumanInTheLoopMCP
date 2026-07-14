using HumanInTheLoop.Mcp.Abstractions;
using Microsoft.Extensions.Options;

namespace HumanInTheLoop.Mcp;

/// <summary>
/// Single-tenant resolver: any request bearing the configured <see cref="HumanInTheLoopOptions.AccessToken"/>
/// maps to the configured default recipient. Swap this out for a multi-tenant implementation
/// (per-user tokens) in a hosted service.
/// </summary>
public sealed class ConfigRecipientResolver : IRecipientResolver
{
    private readonly HumanInTheLoopOptions _options;

    public ConfigRecipientResolver(IOptions<HumanInTheLoopOptions> options) => _options = options.Value;

    public Task<Recipient?> ResolveAsync(string accessToken, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(_options.AccessToken) ||
            !string.Equals(accessToken, _options.AccessToken, StringComparison.Ordinal) ||
            string.IsNullOrWhiteSpace(_options.DefaultRecipient))
        {
            return Task.FromResult<Recipient?>(null);
        }

        return Task.FromResult<Recipient?>(new Recipient(_options.DefaultChannel, _options.DefaultRecipient));
    }
}
