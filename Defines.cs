using System.Collections.Generic;

namespace Arhira
{
    internal static class Defines
    {
        internal static readonly IEnumerable<EmojiRolePair> EmojiRolePairs = new[]
        {
            new EmojiRolePair(785672410242351124, 787207100044279808),
            new EmojiRolePair(791113170470830140, 787207068841934848),
            new EmojiRolePair(785672410803863574, 791186577057382404)
        };

        internal const ulong GuildId = 606442027873206292;
        internal static readonly Channels ChannelIds = new();
        internal static readonly Messages MessagesIds = new();

        internal sealed record EmojiRolePair
        {
            internal readonly ulong EmojiId;
            internal readonly ulong RoleId;

            internal EmojiRolePair(ulong emojiId, ulong roleId)
            {
                EmojiId = emojiId;
                RoleId = roleId;
            }
        }

        internal sealed record Channels
        {
            internal readonly ulong EntryPoint = 606442027873206294;
        }

        internal sealed record Messages
        {
            internal readonly ulong HelloMessage = 606904872008417291;
        }
    }
}