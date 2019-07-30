﻿// This document is intended for use by Kupo Nut Brigade developers.

namespace KupoNutsBot.Services
{
	using System.Threading.Tasks;

	public abstract class ServiceBase
	{
		public abstract Task Initialize();

		public abstract Task Shutdown();
	}
}