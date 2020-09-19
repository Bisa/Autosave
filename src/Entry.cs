using HarmonyLib;
using QModManager.API.ModLoading;
using QModManager.Utility;
using System;
using System.IO;
using System.Reflection;
using static QModManager.Utility.Logger;

namespace Autosave
{
	[QModCore]
	public static class Entry
	{
		internal static Config GetConfig = null;

		internal static string GetAssemblyDirectory
		{
			get
			{
				string fullPath = Assembly.GetExecutingAssembly().Location;

				return Path.GetDirectoryName(fullPath);
			}
		}

		internal static void LogFatal(string message, Exception exception = null)
		{
			LogMessage(message, Level.Fatal, exception);
		}

		internal static void LogError(string message, Exception exception = null)
		{
			LogMessage(message, Level.Error, exception);
		}

		internal static void LogWarning(string message, Exception exception = null)
		{
			LogMessage(message, Level.Warn, exception);
		}

		internal static void LogInfo(string message)
		{
			LogMessage(message, Level.Info);
		}

		internal static void LogDebug(string message, bool showOnScreen = false)
		{
			LogMessage(message, Level.Debug, null, showOnScreen);
		}

		internal static void DisplayMessage(string message, Level level = Level.Info)
		{
			LogInfo("Displaying message in-game:");
			Logger.Log(
				Level.Info,
				string.Format(
					"[Autosave{0}] {1}",
					(level == Level.Info) ? string.Empty : ":" + level.ToString(),
					message),
				null,
				true);
		}

		private static void LogMessage(string message, Level level = Level.Info, System.Exception exception = null, bool showOnScreen = false)
		{
			// Start by logging to file
			Logger.Log(
				level,
				message,
				exception,
				false);

			// ... and display on screen if we are debugging
			if(showOnScreen && Logger.DebugLogsEnabled)
			{
				DisplayMessage(message, level);
			}
		}

		[QModPatch]
		public static void Initialize()
		{
			ConfigHandler.LoadConfig();

			var harmony = new Harmony("io.github.bisa.autosave");
			#if DEBUG
			harmony.PatchAll(typeof(DebuggingPatches));
			#endif
			harmony.PatchAll(typeof(PlayerPatches));
			harmony.PatchAll(typeof(IngameMenuPatches));
			harmony.PatchAll(typeof(SubRootPatches));
			harmony.PatchAll(typeof(LanguagePatches));
		}
	}
}
