// Copyright (c) FCChan. All rights reserved.
//
// Licensed under the MIT license.

namespace FC.Bot.Characters
{
	using System;
	using System.Collections.Generic;
	using System.IO;
	using System.Text;
	using System.Threading.Tasks;
	using Discord;
	using Discord.WebSocket;
	using FC.Bot.Commands;
	using FC.Bot.Services;
	using FC.Bot.Utils;
	using FC.Utils;
	using SixLabors.ImageSharp;
	using SixLabors.ImageSharp.PixelFormats;
	using SixLabors.ImageSharp.Processing;
	using XIVAPI;

	using FFXIVCollectCharacter = FFXIVCollect.CharacterAPI.Character;
	using Image = SixLabors.ImageSharp.Image;
	using XIVAPICharacter = XIVAPI.Character;

	public class CharacterService : ServiceBase
	{
		public override async Task Initialize()
		{
			await base.Initialize();
		}

		[Command("IAm", Permissions.Everyone, "Records your character for use with the 'WhoIs' and 'WhoAmI' commands")]
		public async Task<string> IAm(CommandMessage message, string characterName, string serverName)
		{
			User user = await UserService.GetUser(message.Author);
			CharacterInfo character = await this.GetCharacterInfo(characterName, serverName);

			return await this.RecordCharacter(user, character);
		}

		[Command("IAm", Permissions.Everyone, "Records your character for use with the 'WhoIs' and 'WhoAmI' commands")]
		public async Task<string> IAm(CommandMessage message, uint characterId)
		{
			User user = await UserService.GetUser(message.Author);
			CharacterInfo character = await this.GetCharacterInfo(characterId);

			return await this.RecordCharacter(user, character);
		}

		[Command("IAmNot", Permissions.Everyone, "Removes your linked lodestone character")]
		public async Task<string> IAmNot(CommandMessage message, string characterName)
		{
			User user = await UserService.GetUser(message.Author);
			user.RemoveCharacter(characterName);
			await UserService.SaveUser(user);
			return "Character unlinked!";
		}

		[Command("IAmNot", Permissions.Everyone, "Removes your linked lodestone character")]
		public async Task<string> IAmNot(CommandMessage message, string characterName, string serverName)
		{
			User user = await UserService.GetUser(message.Author);
			user.RemoveCharacter(characterName, serverName);
			await UserService.SaveUser(user);
			return "Character unlinked!";
		}

		[Command("IAmNot", Permissions.Everyone, "Removes your linked lodestone character")]
		public async Task<string> IAmNot(CommandMessage message, uint characterId)
		{
			User user = await UserService.GetUser(message.Author);
			user.RemoveCharacter(characterId);
			await UserService.SaveUser(user);
			return "Character unlinked!";
		}

		[Command("IAmUsually", Permissions.Everyone, "Sets the linked lodestone character as your default")]
		public async Task<string> IAmUsually(CommandMessage message, string characterName)
		{
			User user = await UserService.GetUser(message.Author);
			user.SetDefaultCharacter(characterName);
			await UserService.SaveUser(user);
			return "Default character updated!";
		}

		[Command("IAmUsually", Permissions.Everyone, "Sets the linked lodestone character as your default")]
		public async Task<string> IAmUsually(CommandMessage message, string characterName, string serverName)
		{
			User user = await UserService.GetUser(message.Author);
			user.SetDefaultCharacter(characterName, serverName);
			await UserService.SaveUser(user);
			return "Default character updated!";
		}

		[Command("WhoAmI", Permissions.Everyone, "Displays your linked character")]
		public async Task WhoAmI(CommandMessage message)
		{
			User user = await UserService.GetUser(message.Author);
			await this.PostWhoIsResponse(message, user);
		}

		[Command("WhoAmI", Permissions.Everyone, "Displays your linked character")]
		public async Task WhoAmI(CommandMessage message, int characterIndex)
		{
			User user = await UserService.GetUser(message.Author);
			await this.PostWhoIsResponse(message, user, characterIndex);
		}

		[Command("WhoIs", Permissions.Everyone, "Looks up a linked character")]
		public async Task WhoIs(CommandMessage message, IGuildUser user)
		{
			User userEntry = await UserService.GetUser(user);
			await this.PostWhoIsResponse(message, userEntry);
		}

		[Command("WhoIs", Permissions.Everyone, "Looks up a linked character")]
		public async Task WhoIs(CommandMessage message, IGuildUser user, int characterIndex)
		{
			User userEntry = await UserService.GetUser(user);
			await this.PostWhoIsResponse(message, userEntry, characterIndex);
		}

		[Command("WhoIs", Permissions.Everyone, "looks up a character profile by character and server name")]
		public async Task WhoIs(CommandMessage message, string characterName, string serverName)
		{
			CharacterInfo character = await this.GetCharacterInfo(characterName, serverName);
			string file = await CharacterCard.Draw(character);
			await message.Channel.SendFileAsync(file);
		}

		public async Task PostWhoIsResponse(CommandMessage message, User user, int? characterIndex = null)
		{
			User.Character? defaultCharacter = user.GetDefaultCharacter();
			if (defaultCharacter is null)
				throw new UserException("No characters linked! Use `IAm` to link a character");

			int index = 0;
			if (characterIndex != null)
			{
				defaultCharacter = null;
				foreach (User.Character character in user.Characters)
				{
					index++;

					if (index == characterIndex)
					{
						defaultCharacter = character;
					}
				}

				if (defaultCharacter is null)
				{
					throw new UserException("I couldn't find a character at index: " + characterIndex);
				}
			}

			// Default character
			CharacterInfo defaultCharacterInfo = await this.GetCharacterInfo(defaultCharacter.FFXIVCharacterId);
			string file = await CharacterCard.Draw(defaultCharacterInfo);
			await message.Channel.SendFileAsync(file);

			if (!defaultCharacter.IsVerified(defaultCharacterInfo))
			{
				EmbedBuilder builder = new EmbedBuilder();
				builder.Description = "This character has not been verified.";
				builder.Color = Discord.Color.Gold;

				// If this is the requesting users character, give instructions on how to verify
				if (message.Author.Id == user.DiscordUserId)
				{
					builder.Title = "This character has not been verified";
					builder.Description = "To verify this character, enter the following verification code in your [lodestone profile](https://na.finalfantasyxiv.com/lodestone/my/setting/profile/).\n" + defaultCharacter.FFXIVCharacterVerification;
				}

				await message.Channel.SendMessageAsync(null, false, builder.Build());
			}

			if (!defaultCharacterInfo.HasMinions && !defaultCharacterInfo.HasMounts && !defaultCharacterInfo.HasAchievements)
			{
				EmbedBuilder builder = new EmbedBuilder();
				builder.Description = "To show Minions, Mounts, and Achievements, please link your character at [FFXIV Collect](https://ffxivcollect.com/)";
				await message.Channel.SendMessageAsync(null, false, builder.Build());
			}

			// AKA
			StringBuilder akaDescBuilder = new StringBuilder();
			index = 0;
			foreach (User.Character character in user.Characters)
			{
				index++;

				akaDescBuilder.Append(index);
				akaDescBuilder.Append(") ");
				akaDescBuilder.Append(character.CharacterName);
				akaDescBuilder.Append(" (");
				akaDescBuilder.Append(Emotes.Home.GetString());
				akaDescBuilder.Append(character.ServerName);
				akaDescBuilder.Append(")");

				if (!await character.IsVerified(user))
					akaDescBuilder.Append(" *(Not Verified)*");

				akaDescBuilder.AppendLine();
			}

			if (index > 1)
			{
				EmbedBuilder builder = new EmbedBuilder();
				builder.Description = akaDescBuilder.ToString();
				builder.Title = "Also known as:";
				await message.Channel.SendMessageAsync(null, false, builder.Build());
			}
		}

		[Command("Portrait", Permissions.Everyone, "Shows your linked character portrait")]
		public async Task Portrait(CommandMessage message)
		{
			CharacterInfo character = await this.GetCharacterInfo(message.Author);
			string file = await CharacterPortrait.Draw(character);

			await message.Channel.SendFileAsync(file);
		}

		[Command("Portrait", Permissions.Everyone, "Shows another user's linked character portrait")]
		public async Task Portrait(CommandMessage message, IGuildUser user)
		{
			CharacterInfo character = await this.GetCharacterInfo(user);
			string file = await CharacterPortrait.Draw(character);

			await message.Channel.SendFileAsync(file);
		}

		[Command("Portrait", Permissions.Everyone, "Looks up a character profile by character name and server name")]
		public async Task Portrait(CommandMessage message, string characterName, string serverName)
		{
			CharacterInfo character = await this.GetCharacterInfo(characterName, serverName);
			string file = await CharacterPortrait.Draw(character);
		}

		[Command("Gear", Permissions.Everyone, "Shows the current gear and stats of a character")]
		public async Task<Embed> Gear(CommandMessage message, IGuildUser user)
		{
			CharacterInfo info = await this.GetCharacterInfo(user);
			return info.GetGearEmbed();
		}

		[Command("Gear", Permissions.Everyone, "Shows the current gear and stats of a character")]
		public async Task<Embed> Gear(CommandMessage message)
		{
			CharacterInfo info = await this.GetCharacterInfo(message.Author);
			return info.GetGearEmbed();
		}

		[Command("Gear", Permissions.Everyone, "Shows the current gear and stats of a character")]
		public async Task<Embed> Gear(CommandMessage message, string characterName, string serverName)
		{
			CharacterInfo info = await this.GetCharacterInfo(characterName, serverName);
			return info.GetGearEmbed();
		}

		[Command("Stats", Permissions.Everyone, "Shows the current gear and stats of a character")]
		public async Task<Embed> Stats(IGuildUser user)
		{
			CharacterInfo info = await this.GetCharacterInfo(user);
			return info.GetAttributesEmbed();
		}

		[Command("Stats", Permissions.Everyone, "Shows the current gear and stats of your linked character")]
		public async Task<Embed> Stats(CommandMessage message)
		{
			CharacterInfo info = await this.GetCharacterInfo(message.Author);
			return info.GetAttributesEmbed();
		}

		[Command("Stats", Permissions.Everyone, "Shows the current gear and stats of a character")]
		public async Task<Embed> Stats(string characterName, string serverName)
		{
			CharacterInfo info = await this.GetCharacterInfo(characterName, serverName);
			return info.GetAttributesEmbed();
		}

		[Command("ElementalLevel", Permissions.Everyone, "Shows current Elemental Level of a character")]
		[Command("EL", Permissions.Everyone, "Shows current Elemental Level of a character")]
		public async Task<Embed> ElementalLevel(CommandMessage message)
		{
			return await this.GetElementalLevel(message);
		}

		[Command("ElementalLevel", Permissions.Everyone, "Shows current Elemental Level of a character")]
		[Command("EL", Permissions.Everyone, "Shows current Elemental Level of a character")]
		public async Task<Embed> ElementalLevel(CommandMessage message, int characterIndex)
		{
			return await this.GetElementalLevel(message, characterIndex);
		}

		private async Task<Embed> GetElementalLevel(CommandMessage message, int? characterIndex = null)
		{
			User user = await UserService.GetUser(message.Author);
			User.Character? defaultCharacter = user.GetDefaultCharacter();
			if (defaultCharacter is null)
				throw new UserException("No characters linked! Use `IAm` to link a character");

			int index = 0;
			if (characterIndex != null)
			{
				defaultCharacter = null;
				foreach (User.Character character in user.Characters)
				{
					index++;

					if (index == characterIndex)
					{
						defaultCharacter = character;
					}
				}

				if (defaultCharacter is null)
				{
					throw new UserException("I couldn't find a character at index: " + characterIndex);
				}
			}

			CharacterInfo info = await this.GetCharacterInfo(defaultCharacter.FFXIVCharacterId);
			return await info.GetElementalLevelEmbed();
		}

		private async Task<string> RecordCharacter(User user, CharacterInfo character)
		{
			User.Character? userCharacter = user.GetCharacter(character.Id);

			if (userCharacter == null)
			{
				userCharacter = new User.Character();
				userCharacter.FFXIVCharacterId = character.Id;
				userCharacter.CharacterName = character.Name;
				userCharacter.ServerName = character.Server;
				userCharacter.IsVerified = false;
				user.Characters.Add(userCharacter);
				await UserService.SaveUser(user);
			}

			if (userCharacter.IsVerified)
			{
				userCharacter.CharacterName = character.Name;
				userCharacter.ServerName = character.Server;
				await UserService.SaveUser(user);

				return "Character linked!";
			}
			else
			{
				if (userCharacter.FFXIVCharacterVerification == null)
				{
					userCharacter.FFXIVCharacterVerification = Guid.NewGuid().ToString();
					await UserService.SaveUser(user);
				}

				if (character.Bio?.Contains(userCharacter.FFXIVCharacterVerification) == true)
				{
					userCharacter.FFXIVCharacterId = character.Id;
					userCharacter.CharacterName = character.Name;
					userCharacter.ServerName = character.Server;
					userCharacter.FFXIVCharacterVerification = null;
					userCharacter.IsVerified = true;
					await UserService.SaveUser(user);

					return "Character linked! (You can now remove the Verification Id from your Character Profile)";
				}
				else
				{
					return "To verify character ownership, please place the following verification Id in your lodestone character profile: `" + userCharacter.FFXIVCharacterVerification + "`";
				}
			}
		}

		private async Task<CharacterInfo> GetCharacterInfo(IGuildUser guildUser)
		{
			User user = await UserService.GetUser(guildUser);
			User.Character? character = user.GetDefaultCharacter();

			if (character is null)
				throw new UserException("No characters linked.");

			return await this.GetCharacterInfo(character.FFXIVCharacterId);
		}

		private async Task<CharacterInfo> GetCharacterInfo(string characterName, string serverName)
		{
			XIVAPI.CharacterAPI.SearchResponse response = await XIVAPI.CharacterAPI.Search(characterName, serverName);

			if (response.Pagination == null)
				throw new Exception("No Pagination");

			if (response.Results == null)
			{
				throw new Exception("No Results");
			}
			else if (response.Results.Count == 0)
			{
				throw new UserException("I couldn't find a character with that name.");
			}
			else if (response.Results.Count != 1)
			{
				throw new UserException("I found " + response.Results.Count + " characters with that name.");
			}
			else
			{
				return await this.GetCharacterInfo(response.Results[0].ID);
			}
		}

		private async Task<CharacterInfo> GetCharacterInfo(uint id)
		{
			CharacterInfo info = new CharacterInfo(id);
			await info.Update();
			return info;
		}
	}
}
