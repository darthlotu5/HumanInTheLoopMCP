using HumanInTheLoop.Mcp;
using HumanInTheLoop.Mcp.Abstractions;
using HumanInTheLoop.TelegramMCPSample;

var builder = WebApplication.CreateBuilder(args);

// Core MCP server + ask_user tool + single-tenant (config) recipient resolver.
builder.Services.AddHumanInTheLoopMcp(builder.Configuration);

// Bring your own channel: register a Telegram channel (also long-polls for answers).
var botToken = builder.Configuration["HumanInTheLoop:TelegramBotToken"]
    ?? throw new InvalidOperationException(
        "Set HumanInTheLoop:TelegramBotToken (copy appsettings-example.json to appsettings.json).");

builder.Services.AddSingleton(sp =>
    new TelegramChannel(botToken, sp.GetRequiredService<ILogger<TelegramChannel>>()));
builder.Services.AddSingleton<IHumanChannel>(sp => sp.GetRequiredService<TelegramChannel>());
builder.Services.AddHostedService(sp => sp.GetRequiredService<TelegramChannel>());

var app = builder.Build();

app.MapGet("/", () => "Human-in-the-Loop MCP (Telegram sample) is running.");
app.MapHumanInTheLoopMcp("/mcp");

app.Run();
