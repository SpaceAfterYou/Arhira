import { config } from 'dotenv';
config();

import { defines } from './defines';
import Discord from 'discord.js';

const client = new Discord.Client({ partials: ['MESSAGE', 'REACTION'] });

client.on('ready', () => {
  console.log('Bot has started');
});

client.on('error', (e) => {
  console.error('Discord client error!', e);
});

const main = async () => {
  await client.login(process.env.DISCORD_TOKEN);

  const channel: Discord.TextChannel = await (client.channels.fetch(defines.channels.entryPoint) as Promise<Discord.TextChannel>);
  const message = await channel.messages.fetch(defines.messages.helloMessage);

  const messageCollector = message.createReactionCollector((reaction: Discord.MessageReaction) => {
    console.log(reaction.emoji.id);

    return defines.roles.some(c => c.emoji == reaction.emoji?.id);
  }, { dispose: true });

  messageCollector.on('remove', async (reaction, user) => {
    console.log('remove');

    const role = defines.roles.find(c => c.emoji == reaction.emoji?.id)!;
    await message.guild?.member(user)?.roles.remove(role.role);
  });

  messageCollector.on('collect', async (reaction, user) => {
    console.log('collect');

    const role = defines.roles.find(c => c.emoji == reaction.emoji?.id)!;
    await message.guild?.member(user)?.roles.add(role.role);
  });

};

main();
