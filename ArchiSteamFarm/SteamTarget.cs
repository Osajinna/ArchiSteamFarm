﻿//     _                _      _  ____   _                           _____
//    / \    _ __  ___ | |__  (_)/ ___| | |_  ___   __ _  _ __ ___  |  ___|__ _  _ __  _ __ ___
//   / _ \  | '__|/ __|| '_ \ | |\___ \ | __|/ _ \ / _` || '_ ` _ \ | |_  / _` || '__|| '_ ` _ \
//  / ___ \ | |  | (__ | | | || | ___) || |_|  __/| (_| || | | | | ||  _|| (_| || |   | | | | | |
// /_/   \_\|_|   \___||_| |_||_||____/  \__|\___| \__,_||_| |_| |_||_|   \__,_||_|   |_| |_| |_|
// 
//  Copyright 2015-2018 Łukasz "JustArchi" Domeradzki
//  Contact: JustArchi@JustArchi.net
// 
//  Licensed under the Apache License, Version 2.0 (the "License");
//  you may not use this file except in compliance with the License.
//  You may obtain a copy of the License at
// 
//  http://www.apache.org/licenses/LICENSE-2.0
//      
//  Unless required by applicable law or agreed to in writing, software
//  distributed under the License is distributed on an "AS IS" BASIS,
//  WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//  See the License for the specific language governing permissions and
//  limitations under the License.

using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;
using NLog;
using NLog.Config;
using NLog.Targets;

namespace ArchiSteamFarm {
	[SuppressMessage("ReSharper", "ClassNeverInstantiated.Global")]
	[Target(TargetName)]
	internal sealed class SteamTarget : TargetWithLayout {
		internal const string TargetName = "Steam";

		// This is NLog config property, it must have public get() and set() capabilities
		[SuppressMessage("ReSharper", "MemberCanBePrivate.Global")]
		[SuppressMessage("ReSharper", "UnusedAutoPropertyAccessor.Global")]
		public string BotName { get; set; }

		// This is NLog config property, it must have public get() and set() capabilities
		[SuppressMessage("ReSharper", "MemberCanBePrivate.Global")]
		[SuppressMessage("ReSharper", "UnusedAutoPropertyAccessor.Global")]
		[RequiredParameter]
		public ulong ChatGroupID { get; set; }

		// This is NLog config property, it must have public get() and set() capabilities
		[SuppressMessage("ReSharper", "MemberCanBePrivate.Global")]
		[SuppressMessage("ReSharper", "UnusedAutoPropertyAccessor.Global")]
		[RequiredParameter]
		public ulong ChatID { get; set; }

		// This is NLog config property, it must have public get() and set() capabilities
		[SuppressMessage("ReSharper", "MemberCanBePrivate.Global")]
		[SuppressMessage("ReSharper", "UnusedAutoPropertyAccessor.Global")]
		[RequiredParameter]
		public ulong SteamID { get; set; }

		// This parameterless(!) constructor is intentionally public, as NLog uses it for creating targets
		// It must stay like this as we want to have our targets defined in our NLog.config
		// Keeping date in default layout also doesn't make much sense (Steam offers that), so we remove it by default
		public SteamTarget() => Layout = "${level:uppercase=true}|${logger}|${message}";

		protected override void Write(LogEventInfo logEvent) {
			if (logEvent == null) {
				ASF.ArchiLogger.LogNullError(nameof(logEvent));
				return;
			}

			base.Write(logEvent);

			bool groupMessage = (ChatGroupID != 0) && (ChatID != 0);
			bool privateMessage = SteamID != 0;

			if (!groupMessage && !privateMessage) {
				return;
			}

			string message = Layout.Render(logEvent);

			if (string.IsNullOrEmpty(message)) {
				return;
			}

			Bot bot = null;

			if (!string.IsNullOrEmpty(BotName)) {
				if (!Bot.Bots.TryGetValue(BotName, out bot)) {
					return;
				}

				if (!bot.IsConnectedAndLoggedOn) {
					return;
				}
			}

			if (groupMessage) {
				Utilities.InBackground(() => SendGroupMessage(message, bot));
			}

			if (privateMessage && ((bot == null) || (bot.CachedSteamID != SteamID))) {
				Utilities.InBackground(() => SendPrivateMessage(message, bot));
			}
		}

		private async Task SendGroupMessage(string message, Bot bot = null) {
			if (string.IsNullOrEmpty(message)) {
				ASF.ArchiLogger.LogNullError(nameof(message));
				return;
			}

			if (bot == null) {
				bot = Bot.Bots.Values.FirstOrDefault(targetBot => targetBot.IsConnectedAndLoggedOn);

				if (bot == null) {
					return;
				}
			}

			await bot.SendMessage(ChatGroupID, ChatID, message).ConfigureAwait(false);
		}

		private async Task SendPrivateMessage(string message, Bot bot = null) {
			if (string.IsNullOrEmpty(message)) {
				ASF.ArchiLogger.LogNullError(nameof(message));
				return;
			}

			if (bot == null) {
				bot = Bot.Bots.Values.FirstOrDefault(targetBot => targetBot.IsConnectedAndLoggedOn && (targetBot.CachedSteamID != SteamID));

				if (bot == null) {
					return;
				}
			}

			await bot.SendMessage(SteamID, message).ConfigureAwait(false);
		}
	}
}