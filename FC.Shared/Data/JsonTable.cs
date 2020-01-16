﻿// Copyright (c) FCChan. All rights reserved.
//
// Licensed under the MIT license.

namespace FC.Data
{
	using System;
	using System.Collections.Generic;
	using System.IO;
	using System.Reflection;
	using System.Text.Json;
	using System.Threading.Tasks;

	public class JsonTable : ITable
	{
		public readonly string Name;
		public readonly int Version;

		internal JsonTable(string databaseName, int version)
		{
			this.Name = databaseName;
			this.Version = version;
		}

		public string DirectoryPath
		{
			get
			{
				return "Database/" + this.Name + "_" + this.Version + "/";
			}
		}

		public Task Connect()
		{
			if (!Directory.Exists(this.DirectoryPath))
				Directory.CreateDirectory(this.DirectoryPath);

			return Task.CompletedTask;
		}

		public Task<T> CreateEntry<T>(string? id = null)
			where T : EntryBase, new()
		{
			if (id == null)
				id = Guid.NewGuid().ToString();

			T entry = Activator.CreateInstance<T>();
			entry.Id = id;
			this.Save(entry);
			return Task.FromResult(entry);
		}

		public Task Delete<T>(T entry)
			where T : EntryBase, new()
		{
			return this.Delete(entry.Id);
		}

		public Task Delete(string key)
		{
			string path = this.GetEntryPath(key);

			if (!File.Exists(path))
				return Task.CompletedTask;

			File.Delete(path);
			return Task.CompletedTask;
		}

		public Task<string> GetNewID()
		{
			return Task.FromResult(Guid.NewGuid().ToString());
		}

		public Task<T?> Load<T>(string key)
			where T : EntryBase, new()
		{
			string path = this.GetEntryPath(key);

			if (!File.Exists(path))
				return Task.FromResult<T?>(null);

			string json = File.ReadAllText(path);
			T? entry = JsonSerializer.Deserialize<T>(json);

			return Task.FromResult((T?)entry);
		}

		public Task<List<T>> LoadAll<T>(Dictionary<string, object>? conditions = null)
			where T : EntryBase, new()
		{
			List<T> results = new List<T>();

			string[] files = Directory.GetFiles(this.DirectoryPath, "*.json");

			foreach (string path in files)
			{
				string json = File.ReadAllText(path);
				T entry = JsonSerializer.Deserialize<T>(json);

				bool meetsConditions = true;
				if (conditions != null)
				{
					foreach ((string propertyName, object value) in conditions)
					{
						PropertyInfo info = entry.GetType().GetProperty(propertyName);
						object val = info.GetValue(entry);

						if (!val.Equals(value))
						{
							meetsConditions = false;
							continue;
						}
					}
				}

				if (!meetsConditions)
					continue;

				results.Add(entry);
			}

			return Task.FromResult(results);
		}

		public async Task<T> LoadOrCreate<T>(string key)
			where T : EntryBase, new()
		{
			T? result = await this.Load<T>(key);
			if (result == null)
				result = await this.CreateEntry<T>(key);

			return result;
		}

		public Task Save(EntryBase document)
		{
			string path = this.GetEntryPath(document.Id);

			string json = JsonSerializer.Serialize(document);
			File.WriteAllText(path, json);

			return Task.CompletedTask;
		}

		private string GetEntryPath(string key)
		{
			// replace bad characters with "-".
			foreach (char c in Path.GetInvalidFileNameChars())
			{
				key = key.Replace(c, '-');
			}

			return this.DirectoryPath + key + ".json";
		}
	}
}
