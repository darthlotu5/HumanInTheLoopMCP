using System.ComponentModel;
using HumanInTheLoop.Mcp.Abstractions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using ModelContextProtocol;
using ModelContextProtocol.Server;

namespace HumanInTheLoop.Mcp;

[McpServerToolType]
public sealed class HumanTools
{
    internal const string RecipientItemKey = "hitl.recipient";

    private readonly IReadOnlyDictionary<string, IHumanChannel> _channels;
    private readonly IHttpContextAccessor _http;
    private readonly HumanInTheLoopOptions _options;

    public HumanTools(
        IEnumerable<IHumanChannel> channels,
        IHttpContextAccessor http,
        IOptions<HumanInTheLoopOptions> options)
    {
        _channels = channels.ToDictionary(c => c.ChannelId, StringComparer.OrdinalIgnoreCase);
        _http = http;
        _options = options.Value;
    }

    [McpServerTool(Name = "ask_user"), Description(
        "Ask the human operator a question and block until they reply on their channel (e.g. Telegram). " +
        "Use for approvals, confirmations, or any decision. Pass repository, branch, and conversation for context. " +
        "Returns the choice the human selected or the text they typed.")]
    public async Task<string> AskUser(
        [Description("The question to ask the human.")] string question,
        [Description("Optional selectable choices; the human taps one. Omit for a free-form answer.")]
        string[]? choices = null,
        [Description("Whether the human may type a free-form answer instead of picking a choice. Default true.")]
        bool allowFreeform = true,
        [Description("Repository name for context, e.g. Voxra.API.")] string? repository = null,
        [Description("Current git branch for context, e.g. feature/billing.")] string? branch = null,
        [Description("Short title of the current session/conversation.")] string? conversation = null,
        [Description("One extra line of context about why you're asking or what happens next.")] string? context = null,
        IProgress<ProgressNotificationValue>? progress = null,
        CancellationToken cancellationToken = default)
    {
        if (_http.HttpContext?.Items.TryGetValue(RecipientItemKey, out var value) != true || value is not Recipient recipient)
            return "Could not determine who to ask (missing or invalid access token).";

        if (!_channels.TryGetValue(recipient.ChannelId, out var channel))
            return $"No channel '{recipient.ChannelId}' is configured on this server.";

        var human = new HumanQuestion(
            Id: Guid.NewGuid().ToString("N"),
            Question: question,
            Choices: choices ?? [],
            AllowFreeform: allowFreeform,
            Context: context,
            Repository: repository,
            Branch: branch,
            Conversation: conversation);

        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(TimeSpan.FromSeconds(_options.AnswerTimeoutSeconds));

        // Keep the long-lived HTTP/SSE response flowing through proxies with a hard idle limit
        // (e.g. Azure App Service's ~230s) while we wait for the human.
        using var keepAlive = CancellationTokenSource.CreateLinkedTokenSource(timeout.Token);
        _ = KeepAliveAsync(progress, keepAlive.Token);

        try
        {
            return await channel.AskAsync(recipient, human, timeout.Token);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return $"No answer was received from the human within {_options.AnswerTimeoutSeconds} seconds.";
        }
        finally
        {
            keepAlive.Cancel();
        }
    }

    private static async Task KeepAliveAsync(IProgress<ProgressNotificationValue>? progress, CancellationToken ct)
    {
        if (progress is null)
            return;

        var count = 0;
        try
        {
            while (!ct.IsCancellationRequested)
            {
                await Task.Delay(TimeSpan.FromSeconds(15), ct);
                progress.Report(new ProgressNotificationValue { Progress = ++count, Message = "Waiting for your reply\u2026" });
            }
        }
        catch (OperationCanceledException)
        {
        }
    }
}
