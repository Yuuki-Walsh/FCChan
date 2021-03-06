﻿// Copyright (c) FCChan. All rights reserved.
//
// Licensed under the MIT license.

namespace FC
{
	using System;
	using FC.Attributes;

	[Serializable]
	public abstract class SettingsEntry : EntryBase
	{
		[InspectorHidden]
		public ulong Guild { get; set; } = 0;
	}
}
