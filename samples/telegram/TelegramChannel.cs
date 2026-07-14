using System.Collections.Concurrent;
using System.Text;
using HumanInTheLoop.Mcp.Abstractions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace HumanInTheLoop.Mcp.Sample.Telegram;

/// <summary>
/// Sample <see cref="IHumanChannel"/> that delivers questions to a Telegram chat and reads the
/// reply (an inline-button tap or a text message). It doubles as a <see cref="BackgroundService"/>
/// that long-polls Telegram for answers. Use this as a template to bring your own channel.
/// </summary>
public sealed class TelegramChannel : BackgroundService, IHumanChannel
{
    private sealed class Pending
    {
        public required string Id { get; init; }
        public required string ChatId { get; init; }
        public required string[] Choices { get; init; }
        public int MessageId { get; set; }
        public DateTime CreatedUtc { get; } = DateTime.UtcNow;
        public TaskCompletionSource<string> Completion { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
    }

    private readonly TelegramBotClient _bot;
    private readonly ILogger<TelegramChannel> _log;
    private readonly ConcurrentDictionary<string, Pending> _pending = new();

    public TelegramChannel(string botToken, ILogger<TelegramChannel> log)
    {
        _bot = new TelegramBotClient(botToken);
        _log = log;
    }

    public string ChannelId => "telegram";

    public async Task<string> AskAsync(Recipient recipient, HumanQuestion question, CancellationToken cancellationToken)
    {
        var pending = new Pending
        {
            Id = question.Id,
            ChatId = recipient.Address,
            Choices = question.Choices.ToArray(),
        };
        _pending[question.Id] = pending;

        InlineKeyboardMarkup? markup = null;
        if (pending.Choices.Length > 0)
        {
            var rows = pending.Choices
                .Select((choice, index) => (IEnumerable<InlineKeyboardButton>)
                    [InlineKeyboardButton.WithCallbackData(Truncate(choice, 60), $"{question.Id}|{index}")])
                .ToList();
            markup = new InlineKeyboardMarkup(rows);
        }

        var message = await _bot.SendMessage(
            recipient.Address,
            BuildHtml(question),
            parseMode: ParseMode.Html,
            replyMarkup: markup,
            cancellationToken: cancellationToken);
        pending.MessageId = message.MessageId;

        await using var registration = cancellationToken.Register(() =>
        {
            _pending.TryRemove(question.Id, out _);
            pending.Completion.TrySetCanceled(cancellationToken);
        });

        return await pending.Completion.Task;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _log.LogInformation("Telegram channel polling started.");
        var offset = 0;
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var updates = await _bot.GetUpdates(offset, timeout: 30, cancellationToken: stoppingToken);
                foreach (var update in updates)
                {
                    offset = update.Id + 1;
                    await HandleUpdateAsync(update, stoppingToken);
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _log.LogError(ex, "Telegram polling error");
                await Task.Delay(TimeSpan.FromSeconds(3), stoppingToken);
            }
        }
    }

    private async Task HandleUpdateAsync(Update update, CancellationToken ct)
    {
        if (update.CallbackQuery is { Data: { } data } callback)
        {
            var parts = data.Split('|', 2);
            if (parts.Length == 2 && _pending.TryGetValue(parts[0], out var pending)
                && int.TryParse(parts[1], out var index) && index >= 0 && index < pending.Choices.Length)
            {
                var answer = pending.Choices[index];
                Complete(parts[0], answer);
                await _bot.AnswerCallbackQuery(callback.Id, $"Sent: {Truncate(answer, 40)}", cancellationToken: ct);
                if (callback.Message is { } m)
                    await _bot.SendMessage(m.Chat.Id, $"\u2705 You chose: {answer}", cancellationToken: ct);
            }
            else
            {
                await _bot.AnswerCallbackQuery(callback.Id, "That question is no longer pending.", cancellationToken: ct);
            }

            return;
        }

        if (update.Message is { Text: { } text } message && !string.IsNullOrWhiteSpace(text) && !text.StartsWith('/'))
        {
            var chatId = message.Chat.Id.ToString();
            Pending? target = null;
            if (message.ReplyToMessage is { } reply)
                target = _pending.Values.FirstOrDefault(p => p.MessageId == reply.MessageId);
            target ??= _pending.Values.Where(p => p.ChatId == chatId).OrderBy(p => p.CreatedUtc).FirstOrDefault();
            if (target is null)
                return;

            Complete(target.Id, text);
            await _bot.SendMessage(message.Chat.Id, "\u2705 Recorded your answer.", cancellationToken: ct);
        }
    }

    private void Complete(string id, string answer)
    {
        if (_pending.TryRemove(id, out var pending))
            pending.Completion.TrySetResult(answer);
    }

    private static string BuildHtml(HumanQuestion q)
    {
        var sb = new StringBuilder();
        sb.AppendLine("\U0001F916 <b>An AI agent needs your help</b>");

        var hasHeader = false;
        void Header(string label, string? value)
        {
            if (string.IsNullOrWhiteSpace(value)) return;
            if (!hasHeader) { sb.AppendLine(); hasHeader = true; }
            sb.AppendLine($"<b>{label}:</b> {Escape(value)}");
        }

        Header("Repository", q.Repository);
        Header("Branch", q.Branch);
        Header("Conversation", q.Conversation);

        sb.AppendLine();
        sb.AppendLine("<b>Question:</b>");
        sb.AppendLine(Escape(q.Question));

        if (!string.IsNullOrWhiteSpace(q.Context))
        {
            sb.AppendLine();
            sb.AppendLine($"<i>{Escape(q.Context)}</i>");
        }

        sb.AppendLine();
        sb.AppendLine(q.Choices.Count > 0
            ? (q.AllowFreeform ? "<i>Tap an option below, or reply with your own answer.</i>" : "<i>Tap an option below.</i>")
            : "<i>Reply with your answer.</i>");

        return sb.ToString().TrimEnd();
    }

    private static string Escape(string value) => value
        .Replace("&", "&amp;")
        .Replace("<", "&lt;")
        .Replace(">", "&gt;");

    private static string Truncate(string value, int max) =>
        value.Length <= max ? value : value[..(max - 1)] + "\u2026";
}
