namespace HumanInTheLoop.Mcp;

public sealed class HumanInTheLoopOptions
{
    public const string SectionName = "HumanInTheLoop";

    /// <summary>Shared secret MCP clients must present as a Bearer token (single-tenant self-host).</summary>
    public string AccessToken { get; set; } = "";

    /// <summary>How long ask_user waits for a human answer before giving up.</summary>
    public int AnswerTimeoutSeconds { get; set; } = 3600;

    /// <summary>Single-tenant default recipient: the channel to deliver to (e.g. "telegram").</summary>
    public string DefaultChannel { get; set; } = "telegram";

    /// <summary>Single-tenant default recipient address (e.g. a Telegram chat id).</summary>
    public string DefaultRecipient { get; set; } = "";
}
