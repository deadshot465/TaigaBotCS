# TaigaBot

Taiga bot is a bot that aims to provide interactive experiences to the users in a private-owned Discord server for fans of Taiga, who is a character from a yaoi visual novel Camp Buddy by BLits.

Taiga bot is based on and is a modified version of [yuuto-bot](https://github.com/Yuuto-Project/yuuto-bot), which is a community-driven project of Offical Camp Buddy Fan Server members, under GNU GPLv3 license. Yuuto bot's idea came from an increasing number of tech-oriented campers in the official fan server. While Yuuto is made by the community and for the community, the dialog choices and some design decisions are not meant for a specific character's fan server such as Taiga's fan server. Therefore, Taiga bot, while based on Yuuto bot and retains most features from Yuuto bot, aims to solve this problem and tailor to Taiga fan server's needs.

Taiga bot is also based on [hirobot](https://github.com/dunste123/hirobot) by dunste123 under the same license.

**Taiga bot is not the original version of Yuuto bot, but a modified version. Hence, if you are interested in the original version, please visit [yuuto-bot](https://github.com/Yuuto-Project/yuuto-bot) instead.**

*If you are interested in joining the project as a developer, please take time to check out Yuuto project's [website](https://iamdeja.github.io/yuuto-docs/).*

*See [hirobot](https://github.com/dunste123/hirobot) for the codebase of Hiro bot.*

## Contents

- [Project Setup](#project-setup)
  - [Bot application](#bot-application)
  - [Why C#](#why-c#)
  - [Setup steps](#setup-steps)
- [Differences between Taiga Bot and Yuuto Bot](#differences-between-taiga-bot-and-yuuto-bot)
- [Disclaimer](#disclaimer)

## Project Setup

The Taiga bot is based on Yuuto bot, which is written in JavaScript and has a dedicated repository [here](https://github.com/Yuuto-Project/yuuto-bot). However, Taiga bot is ported and rewritten in .NET Core 3.1 with C# 8.0.

### Bot application

The bot is a port and a rewritten version of Yuuto bot in C# 8.0. As such, it is run on [.NET Runtime](https://github.com/dotnet/runtime) and uses [Discord.Net](https://github.com/discord-net/Discord.Net) v2.2.0. **Please be advised that it's not written with .NET Framework; instead, it's written with .NET Core 3.1. Therefore, it's entirely possible to compile and develop the program in macOS or other UNIX-like environment.** Setup steps are described later.

### Why C#

JavaScript, while being a de facto language choice when it comes to web development, is a weak-typed language. This makes it more challenging to track each variable and return value's types. As a result, it's not uncommon for the developer to manually track variable's types or assume the available methods and properties of a variable. Also, it's also more challenging for IDEs to provide static type checking and IntelliSense. Therefore, in order to ease the burden when rewriting parts of Yuuto bot's codes, TypeScript was chosen and actively used in as many circumstances as possible. You can read more about TypeScript [here](https://www.typescriptlang.org/).

However, as the developers of Yuuto started seeking more robust languages than JavaScript, with Kotlin being the primary choice as for now. Given the fact that future developments of Yuuto might be migrated to using Kotlin, in order to adopt incoming changes more easily, Taiga bot is again rewritten with .NET Core 3.1 and C# 8.0.

### Setup steps

This repo doesn't include compiled files, which usually are stored under the `bin` folder of the root directory. Therefore, whether you are interested in hosting Taiga bot on your own or are just interested in the code, there are some required steps before you can compile the code.

1. [Install .NET Core](https://dotnet.microsoft.com/download) with methods that apply to your operating system. If you're on Windows or macOS, using [Microsoft Visual Studio](https://visualstudio.microsoft.com/en/downloads/) or [Microsoft Visual Studio Code](https://visualstudio.microsoft.com/en/downloads/) is strongly recommended, as Taiga is developed with these IDEs.

3. Clone this repository with:

   ```bash
   git clone https://github.com/deadshot465/TaigaBotCS.git
   ```

4. Assuming you're using Visual Studio, open up `TaigaBotCS.sln`, the IDE should take care and download required NuGet packages for you.

6. Provided that you have created your own application on Discord, you can manually create a file named `.env` in the same location as the compiled executable named `TaigaBotCS.exe` (Windows) or the respective files on other platforms, as the program will read required tokens and environment variables from this file. An unmodified version of Taiga bot expects the following variables/tokens from `.env`:

   ```
   TOKEN = <Your Discord application token here>
   PREFIX = <The bot's command prefix>
   ADMIN_PREFIX = <The admin's commands' prefix>
   ADMIN_ID = <The primary admin's user ID.>
   GENCHN = <The primary general channel's id>
   BOTCHN = <Dedicated bot commands channel's id>
   BOTMODCHN = <Dedicated bot commands channel's id that is only accessible by mods>
   TESTGENCHN = <Another personal test server's general channel id>
   TESTCHN = <Another personal test server's test channel id>
   VENTCHN = <Venting center channel id, as some channels are not meant for bot's random response>
   UNSPLASH_TOKEN = <This bot uses Unsplash's API to acquire certain images. This is the token of your Unsplash application>
BOT_ID = <YOur Discord bot's ID. This is different from the token.>
   MENTION_REACTION_CHANCE = <When Taiga is mentioned/pinged, the chance of he responding to the message.>
   REACTION_CHANCE = <The probability of Taiga reacting to messages related to certain characters using emote/emojis.>
   RDM_REPLY_CHANCE = <The probability of Taiga replying to messages related to certain characters.>
   SPECIALIZED_CHANCE = <The probability of Taiga replying to messages related to certain characters using specialized messages.>
   ```
   
   **All placeholder texts should be replaced with your own content, without quotation marks (`"` and `'`) and greater than/less than (`<` and `>`) symbols.**
   
7. Once you set up, compile the program to run the bot.


## Differences between Taiga Bot and Yuuto Bot

The main difference is, without a doubt, that Taiga bot is written in C#, while Yuuto bot is written in JavaScript. More detailed descriptions include, but not limited to, the following:

1. All commands of Yuuto bot extend the `Command` class. Due to the design of Discord.Net, the `Command` class is discarded. Instead, all commands utilize the command framework of Discord.Net and inherit the `ModuleBase<SocketCommandContext>` class.
2. `route` command and `valentine` command implement the `ICharacterRoutable` interface.
3. `CalculcateScore` method in `ship` command returns a `Tuple<int, string>`.
4. All parameters of methods are typed, as is required in C#.
5. Taiga bot uses Discord.Net, while Yuuto bot uses a customized version of Discord.js.
6. `cvt` command directly queries a `Dictionary<K, V>` and doesn't convert to Kelvin first when calculating temperatures.
7. Commands, aliases and cooldowns are not properties of the client; instead, they are directly implemented as class attributes.
8. Certain dialogs and reactions are changed to add more flavors to Taiga.
9. Several commands are added and more commands will be implemented as well as the time passes.
10. `info` command shows a modified version of information to add disclaimers and other supporters during the porting and rewriting of Yuuto bot's code.
11. Most services are implemented using dependency injection now.
12. As there is no `Promise` in C#, `async`, `await` and `Task<T>` are heavily used.

## Disclaimer

Taiga bot will not be possible without the code base of Yuuto bot. All credit for Yuuto bot's existing functionalities goes to the developers of Yuuto bot and the community. Please refer to the `info` command for more details.

- [Yuuto Project](https://iamdeja.github.io/yuuto-docs/)
- [Yuuto-bot Repository](https://github.com/Yuuto-Project/yuuto-bot)
- [hirobot](https://github.com/dunste123/hirobot) (by dunste123)
- [Blits Games](https://www.blitsgames.com/)
- [Official Camp Buddy Fan Server](https://discord.gg/campbuddy) (on Discord)