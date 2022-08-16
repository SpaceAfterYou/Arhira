using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using static Arhira.Defines;

namespace Arhira.Workers;

public sealed class RoleWorker : IHostedService
{
    #region Constructors

    public RoleWorker(ILogger<RoleWorker> logger, DiscordSocketClient client)
    {
        _logger = logger;
        _client = client;

        _client.ReactionAdded += Client_ReactionAdded;
        _client.ReactionRemoved += Client_ReactionRemoved;

        _client.Ready += Client_Ready;
    }

    #endregion Constructors

    #region IHostedService

    public Task StartAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    #endregion IHostedService

    #region Private Methods

    private Task Client_ReactionRemoved(Cacheable<IUserMessage, ulong> arg1, Cacheable<IMessageChannel, ulong> channel, SocketReaction reaction)
    {
        _logger.LogInformation("Client_ReactionRemoved");

        if (reaction.MessageId != MessagesIds.HelloMessage) return Task.CompletedTask;

        var guild = _client.Guilds.FirstOrDefault(s => s.Id == GuildId);

        var emote = guild?.Emotes.FirstOrDefault(s => s.Name == reaction.Emote.Name);
        if (emote is null || EmojiRolePairs.All(s => s.EmojiId != emote.Id)) return Task.CompletedTask;

        var user = guild?.Users.FirstOrDefault(s => s.Id == reaction.UserId);
        if (user is null) return Task.CompletedTask;

        var pair = EmojiRolePairs.FirstOrDefault(s => s.EmojiId == emote.Id);
        if (pair is null) return Task.CompletedTask;

        var role = guild?.Roles.FirstOrDefault(s => s.Id == pair.RoleId);
        return role is null ? Task.CompletedTask : user.RemoveRoleAsync(role);
    }

    private Task Client_ReactionAdded(Cacheable<IUserMessage, ulong> arg1, Cacheable<IMessageChannel, ulong> channel, SocketReaction reaction)
    {
        _logger.LogInformation("Client_ReactionAdded");

        if (reaction.MessageId != MessagesIds.HelloMessage) return Task.CompletedTask;

        var guild = _client.Guilds.FirstOrDefault(s => s.Id == GuildId);

        var emote = guild?.Emotes.FirstOrDefault(s => s.Name == reaction.Emote.Name);
        if (emote is null || EmojiRolePairs.All(s => s.EmojiId != emote.Id)) return Task.CompletedTask;

        var user = guild?.Users.FirstOrDefault(s => s.Id == reaction.UserId);
        if (user is null) return Task.CompletedTask;

        var pair = EmojiRolePairs.FirstOrDefault(s => s.EmojiId == emote.Id);
        if (pair is null) return Task.CompletedTask;

        var role = guild?.Roles.FirstOrDefault(s => s.Id == pair.RoleId);
        return role is null ? Task.CompletedTask : user.AddRoleAsync(role);
    }

    private Task Client_Ready()
    {
        _logger.LogInformation("Client_Ready");
        _logger.LogInformation("{} is connected!", _client.CurrentUser);

        var guild = _client.Guilds.FirstOrDefault(s => s.Id == GuildId);
        if (guild is not null) _ = Task.Run(async () => await RoleRestore(guild));

        return Task.CompletedTask;
    }

    private async Task RoleRestore(SocketGuild guild)
    {
        _logger.LogInformation("RoleRestore");

        var members = (await guild.GetUsersAsync().ToListAsync()).SelectMany(e => e).ToDictionary(k => k.Id);

        var roles = EmojiRolePairs
            .Select(s => new { Key = guild.Emotes.First(e => e.Id == s.EmojiId)!, Value = guild.GetRole(s.RoleId) })
            .ToDictionary(k => k.Key, v => v.Value);

        var channel = guild.TextChannels.First(s => s.Id == ChannelIds.EntryPoint);
        var message = await channel.GetMessageAsync(MessagesIds.HelloMessage);

        await Parallel.ForEachAsync(message.Reactions, async (pair, ct) =>
        {
            var (emote, _) = pair;

            await Parallel.ForEachAsync(message.GetReactionUsersAsync(emote, int.MaxValue), async (users, ct) =>
            {
                await Parallel.ForEachAsync(users, async (user, ct) =>
                {
                    if (members.TryGetValue(user.Id, out var member))
                    {
                        var guildEmote = guild.Emotes.FirstOrDefault(s => s.Name == emote.Name);
                        if (guildEmote is not null && roles.TryGetValue(guildEmote, out var role))
                            await member.AddRoleAsync(role);
                    }
                });
            });
        });

        _logger.LogInformation("RoleRestore Done");
    }

    #endregion Private Methods

    #region Private Fields

    private readonly DiscordSocketClient _client;
    private readonly ILogger _logger;

    #endregion Private Fields
}