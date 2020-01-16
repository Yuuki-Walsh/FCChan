﻿// Copyright (c) FCChan. All rights reserved.
//
// Licensed under the MIT license.

namespace FC.Data
{
	using System.Collections.Generic;
	using System.Threading.Tasks;

	public class Table : ITable
	{
		private readonly ITable table;

		public Table(string tableName, int version)
		{
			this.table = TableService.Create(tableName, version);
		}

		public Task Connect()
		{
			return this.table.Connect();
		}

		public Task<T> CreateEntry<T>(string? id = null)
			where T : EntryBase, new()
		{
			return this.table.CreateEntry<T>(id);
		}

		public Task Delete<T>(T entry)
			where T : EntryBase, new()
		{
			return this.table.Delete<T>(entry);
		}

		public Task Delete(string key)
		{
			return this.table.Delete(key);
		}

		public Task<string> GetNewID()
		{
			return this.table.GetNewID();
		}

		public Task<T?> Load<T>(string key)
			where T : EntryBase, new()
		{
			return this.table.Load<T>(key);
		}

		public Task<List<T>> LoadAll<T>(Dictionary<string, object>? conditions = null)
			where T : EntryBase, new()
		{
			return this.table.LoadAll<T>(conditions);
		}

		public Task<T> LoadOrCreate<T>(string key)
			where T : EntryBase, new()
		{
			return this.table.LoadOrCreate<T>(key);
		}

		public Task Save(EntryBase document)
		{
			return this.table.Save(document);
		}
	}
}
