// Copyright (c) FCChan. All rights reserved.
//
// Licensed under the MIT license.

namespace FC.Bot.Characters
{
	using System;
	using System.Collections.Generic;
	using System.Threading.Tasks;
	using FC.Bot.Services;

	public static class UserExtensions
	{
		public static List<User.Character> GetCharacters(this User self, string characterName, string? serverName = null)
		{
			List<User.Character> results = new List<User.Character>();
			foreach (User.Character character in self.Characters)
			{
				if (character.CharacterName?.ToLower() != characterName.ToLower())
					continue;

				if (!string.IsNullOrEmpty(serverName) && character.ServerName?.ToLower() != serverName.ToLower())
					continue;

				results.Add(character);
			}

			return results;
		}

		public static User.Character? GetCharacter(this User self, string characterName, string? serverName = null)
		{
			List<User.Character> characters = self.GetCharacters(characterName, serverName);

			if (characters.Count > 1 && string.IsNullOrEmpty(serverName))
				throw new UserException("Multiple characters found, please specify a server.");

			// its not possible to have multiple characters with the same name on the same server, meaning
			// we've somehow recorded the same character multiple times.
			if (characters.Count > 1)
				throw new Exception("Multiple characters defined in same user!");

			if (characters.Count <= 0)
				return null;

			return characters[0];
		}

		public static User.Character? GetCharacter(this User self, uint ffxivCharacterId)
		{
			foreach (User.Character character in self.Characters)
			{
				if (character.FFXIVCharacterId == ffxivCharacterId)
				{
					return character;
				}
			}

			return null;
		}

		public static void SetDefaultCharacter(this User self, string characterName, string? serverName = null)
		{
			foreach (User.Character otherCharacter in self.Characters)
			{
				otherCharacter.IsDefault = false;
			}

			User.Character? character = self.GetCharacter(characterName, serverName);

			if (character is null)
				throw new Exception("I couldn't find that character, Have you linked them with `IAm`?");

			character.IsDefault = true;
		}

		public static User.Character? GetDefaultCharacter(this User self)
		{
			// no characters at all
			if (self.Characters.Count <= 0)
				return null;

			// return the character marked as default
			foreach (User.Character character in self.Characters)
			{
				if (character.IsDefault)
				{
					return character;
				}
			}

			// return the first verified character
			foreach (User.Character character in self.Characters)
			{
				if (character.IsVerified)
				{
					return character;
				}
			}

			// no default or verified character
			return self.Characters[0];
		}

		public static void RemoveCharacter(this User self, string characterName, string? serverName = null)
		{
			User.Character? character = self.GetCharacter(characterName, serverName);

			if (character == null)
				return;

			self.Characters.Remove(character);
		}

		public static void RemoveCharacter(this User self, uint characterId)
		{
			User.Character? character = self.GetCharacter(characterId);

			if (character == null)
				return;

			self.Characters.Remove(character);
		}

		public static async Task<bool> IsVerified(this User.Character self, User owner)
		{
			if (self.IsVerified)
				return true;

			if (self.FFXIVCharacterVerification == null)
				self.FFXIVCharacterVerification = Guid.NewGuid().ToString();

			XIVAPI.CharacterAPI.GetResponse xivapi = await XIVAPI.CharacterAPI.Get(self.FFXIVCharacterId);

			if (xivapi.Character?.Bio?.Contains(self.FFXIVCharacterVerification) == true)
			{
				self.IsVerified = true;
				await UserService.SaveUser(owner);
			}

			return self.IsVerified;
		}

		public static bool IsVerified(this User.Character self, CharacterInfo character)
		{
			if (self.IsVerified)
				return true;

			if (self.FFXIVCharacterVerification == null)
				self.FFXIVCharacterVerification = Guid.NewGuid().ToString();

			if (character.Bio?.Contains(self.FFXIVCharacterVerification) == true)
				self.IsVerified = true;

			return self.IsVerified;
		}
	}
}
