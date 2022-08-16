using System;
using System.Threading;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Arhira.Workers;

public sealed class LifeWorker : IHostedService
{
    #region Constructors

    public LifeWorker(DiscordSocketClient client, ILogger<LifeWorker> logger, IConfiguration configuration)
    {
        _client = client;
        _logger = logger;

        _client.Log += LogAsync;

        var token = configuration["Discord:Token"] ?? throw new ApplicationException();
        _client.LoginAsync(TokenType.Bot, token);
    }

    #endregion Constructors

    #region IHostedService

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await _client.StartAsync();
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        await _client.StopAsync();
    }

    #endregion IHostedService

    #region Private Methods

    private Task LogAsync(LogMessage log)
    {
        _logger.LogInformation("{}", log.ToString());
        return Task.CompletedTask;
    }

    #endregion Private Methods

    #region Private Fields

    private readonly DiscordSocketClient _client;
    private readonly ILogger _logger;

    #endregion Private Fields
}
