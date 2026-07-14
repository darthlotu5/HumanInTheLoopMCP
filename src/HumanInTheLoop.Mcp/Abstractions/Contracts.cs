namespace HumanInTheLoop.Mcp.Abstractions;

/// <summary>A question to put to a human.</summary>
public sealed record HumanQuestion(
    string Id,
    string Question,
    IReadOnlyList<string> Choices,
    bool AllowFreeform,
    string? Context = null,
    string? Repository = null,
    string? Branch = null,
    string? Conversation = null);

/// <summary>Who to ask, and on which channel.</summary>
/// <param name="ChannelId">The channel that can reach this person, e.g. "telegram".</param>
/// <param name="Address">The channel-specific address, e.g. a Telegram chat id.</param>
/// <param name="DisplayName">Optional friendly name for logging/UX.</param>
public sealed record Recipient(string ChannelId, string Address, string? DisplayName = null);

/// <summary>
/// Delivers questions to a human over some channel (Telegram, Slack, email, a webhook, ...)
/// and returns their answer. Implement this to "bring your own channel".
/// </summary>
public interface IHumanChannel
{
    /// <summary>The channel id this implementation handles (matches <see cref="Recipient.ChannelId"/>).</summary>
    string ChannelId { get; }

    /// <summary>Deliver <paramref name="question"/> to <paramref name="recipient"/> and await the answer.</summary>
    Task<string> AskAsync(Recipient recipient, HumanQuestion question, CancellationToken cancellationToken);
}

/// <summary>
/// Maps a validated MCP access token to the human who should be asked. The single-tenant
/// (config) resolver ships in the core; a multi-tenant resolver (per-user tokens) can be
/// supplied by a hosted service.
/// </summary>
public interface IRecipientResolver
{
    /// <summary>Return the recipient for <paramref name="accessToken"/>, or null if it is not valid.</summary>
    Task<Recipient?> ResolveAsync(string accessToken, CancellationToken cancellationToken);
}
