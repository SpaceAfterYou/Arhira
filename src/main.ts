import { config } from 'dotenv';
config();

import Discord, { Message } from 'discord.js';

const client = new Discord.Client();

client.on('ready', () => {
  console.log('Bot has started');
});

client.on('message', (message: Message) => {
});

client.on('error', (e) => {
  console.error('Discord client error!', e);
});

client.login(process.env.DISCORD_TOKEN);
