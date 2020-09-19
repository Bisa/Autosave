using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UWE;
using System.Text.RegularExpressions;

using static QModManager.Utility.Logger;

namespace Autosave
{
	/* Some of the following code is based on "Safe Autosave" by berkay2578:
	 * https://www.nexusmods.com/subnautica/mods/94
	 * https://github.com/berkay2578/SubnauticaMods/tree/master/SafeAutosave
	 *
	 * Directory replication code from:
	 * https://stackoverflow.com/questions/58744/copy-the-entire-contents-of-a-directory-in-c-sharp */

	public class AutosaveController : MonoBehaviour
	{
		private const int RetryTicks = 5;

		string savePath = "";

		private const int PriorWarningTicks = 30;

		private const string autoSaveIdFormat  = "{0:0000}";

		private const string autoSaveIdSearchPattern  = "-????";

		private bool isSaving = false;

		private int totalTicks = 0;

		private int nextSaveTriggerTick = 120;

		private static Regex autosaveSlotRegex = new Regex(@"^slot\d{4}-\d{4}$", RegexOptions.Compiled);
		
		private static Regex saveSlotRegex = new Regex(@"^slot\d{4}$", RegexOptions.Compiled);

		internal static string GetSavePath()
		{
			PlatformServices platformServices = PlatformUtils.main.GetServices();
			DirectoryInfo SavedGames = null;	

			if (platformServices is PlatformServicesEpic)
			{
				SavedGames = new DirectoryInfo(Path.Combine(
					Application.persistentDataPath,
					"Subnautica",
					"SavedGames"));
			}
			else if (platformServices is PlatformServicesSteam)
			{
				SavedGames = new DirectoryInfo(Path.Combine(
					new DirectoryInfo(Application.dataPath).Parent.FullName,
					"SNAppData",
					"SavedGames"));
			}
			
			if(SavedGames.Exists)
			{
				return SavedGames.FullName;
			}

			return string.Empty;
		}

		private List<AutosaveSlot> GetAutosaveSlotsOldestFirst(string slot)
		{
			List<AutosaveSlot> slots = new List<AutosaveSlot>();
			
			string saveGamesPath = GetSavePath();
			string slotSaveGamesPath = Path.Combine(
				saveGamesPath,
				slot);

			if (new DirectoryInfo(slotSaveGamesPath).Exists)
			{
				// find all autosaves for the slot
				DirectoryInfo[] slotDirectories = new DirectoryInfo(saveGamesPath).GetDirectories(
					slot + autoSaveIdSearchPattern,
					SearchOption.TopDirectoryOnly);
				
				if (slotDirectories.Count<DirectoryInfo>() > 0)
				{
					// collect ids used for all autosaves to this slot
					foreach (DirectoryInfo dir in
						slotDirectories.OrderBy(d => d.GetFiles("gameinfo.json")[0].LastWriteTime))
					{
						string autoSaveIdStr = dir.Name.SubstringFromOccuranceOf(AutosaveSlot.Delimiter, 0);
						int autoSaveId = -1;

						if(int.TryParse(autoSaveIdStr, out autoSaveId))
						{
							slots.Add(new AutosaveSlot(slot, dir, autoSaveId));
						}

						else
						{
							Entry.LogWarning(string.Format(
								"Unable to parse autosave id from '{0}', ignoring directory",
								dir.Name));
						}
					}
				}
			}

			return slots;
		}

		private AutosaveSlot GetNextAutosaveSlot(string slot)
		{
			List<AutosaveSlot> slots = GetAutosaveSlotsOldestFirst(slot);
			
			if(slots.Count == 0)
			{
				Entry.LogDebug(string.Format(
						"Found no slots for {0}, starting at {1}.",
						slot,
						string.Format(AutosaveSlot.Format,  0)),
					true);

				return new AutosaveSlot(slot, 0);
			}

			// up the last id by one
			return new AutosaveSlot(slot, slots.Last().GetId() + 1);
		}

		private bool IsSafePlayerHealth(float minHealthPercent)
		{
			float playerHealthPercent = Player.main.liveMixin.GetHealthFraction() * 100f;

			return playerHealthPercent >= minHealthPercent;
		}

		// Modified IngameMenu.GetAllowSaving
		private bool IsSafeToSave()
		{
			if (IntroVignette.isIntroActive || LaunchRocket.isLaunching)
			{
				Entry.LogDebug($"Did not save, isIntroActive == {IntroVignette.isIntroActive} / isLaunching == {LaunchRocket.isLaunching}");

				return false;
			}

			if (PlayerCinematicController.cinematicModeCount > 0 && Time.time - PlayerCinematicController.cinematicActivityStart <= 30f)
			{
				Entry.LogDebug("Did not save because cinematics are active");

				return false;
			}

			float safeHealthFraction = Entry.Config.MinimumPlayerHealthPercent;

			if (safeHealthFraction > 0f && !this.IsSafePlayerHealth(safeHealthFraction))
			{
				LiveMixin playerLiveMixin = Player.main.liveMixin;
				Entry.LogDebug($"Did not save because player health was {playerLiveMixin.health} / {playerLiveMixin.maxHealth}");

				return false;
			}

			return !SaveLoadManager.main.isSaving;
		}

		private IEnumerator CopyDirCoroutine(string sourceDirName, string destDirName, bool copySubDirs = true)
		{
			DirectoryInfo dir = new DirectoryInfo(sourceDirName);

			if (!dir.Exists)
			{
				throw new DirectoryNotFoundException(
					"Source directory does not exist or could not be found: "
					+ sourceDirName);
			}

			// Get the subdirectories for the specified directory.
			DirectoryInfo[] dirs = dir.GetDirectories();
			yield return null;
			
			// If the destination directory doesn't exist, create it.       
			Directory.CreateDirectory(destDirName);        

			// Get the files in the directory and copy them to the new location.
			FileInfo[] files = dir.GetFiles();
			yield return null;

			foreach (FileInfo file in files)
			{
				string temppath = Path.Combine(destDirName, file.Name);
				file.CopyTo(temppath, false);

				yield return null;
			}

			// If copying subdirectories, copy them and their contents to new location.
			if (copySubDirs)
			{
				foreach (DirectoryInfo subdir in dirs)
				{
					string temppath = Path.Combine(destDirName, subdir.Name);
					yield return CopyDirCoroutine(subdir.FullName, temppath, copySubDirs);
				}
			}

			yield break;
		}

		private IEnumerator DelDirCoroutine(string dirPath)
		{
			DirectoryInfo dir = new DirectoryInfo(dirPath);

			if (!dir.Exists)
			{
				yield break;
			}

			// Get subdirs in the directory.
			DirectoryInfo[] dirs = dir.GetDirectories();
			yield return null;
			
			// Delete files in the directory.
			FileInfo[] files = dir.GetFiles();
			yield return null;

			foreach (FileInfo file in files)
			{
				file.Delete();

				yield return null;
			}

			// Delete subdirs in the directory.
			foreach (DirectoryInfo subdir in dirs)
			{
				yield return DelDirCoroutine(subdir.FullName);
			}

			// Delete the directory itself
			dir.Delete();
			
			yield break;
		}

		private IEnumerator RotateAutosaveSlots(string slot)
		{
			// get the current slots
			List<AutosaveSlot> slots = GetAutosaveSlotsOldestFirst(slot);
			
			// break if theres nothing to do
			if(slots.Count == 0)
			{
				Entry.LogInfo(string.Format(
					"Found no autosave slots for {0}, nothing to rotate",
					slot,
					string.Format(AutosaveSlot.Format, 0)));

				yield break;
			}

			Entry.LogDebug(string.Format(
					"Oldest slot: {0}",
					slots.First().ToString()),
				true);

			Entry.LogDebug(string.Format(
					"Most recent slot: {0}",
					slots.Last().ToString()),
				true);

			// check if we've reached the maximum number of saves, remove oldest save
			if(slots.Count >= Entry.Config.MaxAutosaveSlots)
			{
				Entry.LogInfo(string.Format(
					"Reached maximum number of autosave slots ({0}) for {1}, removing the oldest {2}",
					Entry.Config.MaxAutosaveSlots,
					slot,
					string.Format(
						AutosaveSlot.Format,
						slots.First().GetId())));

				yield return CoroutineHost.StartCoroutine(this.DelDirCoroutine(
					slots.First().GetDirectoryInfo().FullName));
			}

			// handle max ids for the next slot by restarting at 0
			AutosaveSlot nextSlot = GetNextAutosaveSlot(slot);
			yield return null;
			if(nextSlot.GetId() > AutosaveSlot.MaxID)
			{
				// if there's already a 0 slot, remove it
				nextSlot = new AutosaveSlot(slot, 0);
				if(nextSlot.GetDirectoryInfo().Exists)
				{
					yield return CoroutineHost.StartCoroutine(this.DelDirCoroutine(
						nextSlot.GetDirectoryInfo().FullName));
				}
				
				// move our latest save to id 0
				slots.Last().GetDirectoryInfo().MoveTo(nextSlot.GetDirectoryInfo().FullName);
			}

			yield break;
		}

		private bool IsAutosaveSlot(string slot)
		{
			return (autosaveSlotRegex.Matches(slot).Count == 1);
		}

		// Modified IngameMenu.SaveGameAsync
		internal void ChangeSlotIfOnAutosaveSlot()
        {

			if(IsPlayingPermaDeath())
			{
				Entry.LogDebug("We won't be changing any slots while playing perma death.", true);
				return;
			}

            string slot = SaveLoadManager.main.GetCurrentSlot();
			if(IsAutosaveSlot(slot))
			{
				// find all original game slots
				List<string> slots = new List<string>();
				foreach(string name in SaveLoadManager.main.GetPossibleSlotNames())
				{
					if(saveSlotRegex.Matches(name).Count == 1)
					{
						slots.Add(name);
					}
				}
				
				// up the id from the last one to get a new id
				int slotId = 0;
				if(slots.Count > 0)
				{
					slots.Sort();
					
					if(int.TryParse(slots.Last().SubstringFromOccuranceOf("slot", 0), out slotId))
					{
						slotId++;
					}
				}

				// tell the game we want a new slot
				SaveLoadManager.main.SetCurrentSlot(string.Format(
					"slot{0:0000}",
					slotId));
			}
        }

		private static bool IsPlayingPermaDeath()
		{
		    return GameModeUtils.IsPermadeath();
		}

		private IEnumerator AutosaveCoroutine()
		{

			this.isSaving = true;

			if(IsPlayingPermaDeath() && !Entry.Config.AutoSavePermaDeath)
			{
				Entry.LogDebug("Will not autosave a game with permanent death."); // TODO: Translate
				
				UpdateTick();
				
				yield break;
			}

			Entry.DisplayMessage("AutosaveStarting".Translate());
			
			// ensure we do not autosave our own slots when a player loaded them,
			// just give them a new primary slot
			ChangeSlotIfOnAutosaveSlot();

#if DEBUG
			// Close ingame menu if open, used for testing
			IngameMenu.main.Close();
#endif

			yield return null;

			// trigger original save
			IEnumerator saveGameAsync = (IEnumerator)typeof(IngameMenu).GetMethod(
				"SaveGameAsync", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(IngameMenu.main, null);

			yield return saveGameAsync;

			Entry.LogDebug("Executed _SaveGameAsync");

			if(IsPlayingPermaDeath())
			{
				Entry.LogDebug("Playing with permanent death, won't create any new save slots.", true);
			}
			
			else
			{
				string currentSaveSlotPath = string.Empty;
				string saveGamesPath = GetSavePath();

				if(!Directory.Exists(saveGamesPath))
				{
					Entry.LogFatal(string.Format(
						"Unable to find save directory, the expected '{0}' does not exist.",
						saveGamesPath));
					Entry.DisplayMessage("Could not find your SavedGames directory, see log for details!", // TODO: Translate
						Level.Fatal);

					this.isSaving = false;
					throw new FileNotFoundException(savePath);
				}

				currentSaveSlotPath = Path.Combine(saveGamesPath, SaveLoadManager.main.GetCurrentSlot());
				// rotate our current autosave slots
				string slot = SaveLoadManager.main.GetCurrentSlot();
				yield return CoroutineHost.StartCoroutine(this.RotateAutosaveSlots(slot));
				
				// figure out the next slot
				AutosaveSlot nextAutosaveSlot = GetNextAutosaveSlot(slot);
				string nextAutosavePath = nextAutosaveSlot.GetDirectoryInfo().FullName;

				// make a copy of the current slot
				yield return CoroutineHost.StartCoroutine(this.CopyDirCoroutine(
					currentSaveSlotPath, nextAutosavePath));

				Entry.LogDebug(string.Format(
						"Copied from '{0}' to '{1}'",
						currentSaveSlotPath,
						nextAutosavePath),
					true);
			}

			UpdateTick();
			
			Entry.DisplayMessage("AutosaveEnding".FormatTranslate((Entry.Config.MinutesBetweenAutosaves * 60).ToString()));

			yield break;
		}

		private void UpdateTick()
		{
			this.nextSaveTriggerTick += (Entry.Config.MinutesBetweenAutosaves * 60);
			this.isSaving = false;
		}

		private void Tick()
		{
			this.totalTicks++;

			if (this.totalTicks % 30 == 0)
			{
				Entry.LogDebug($"totalTicks reached {this.totalTicks} ticks");
			}

			if (this.totalTicks == this.nextSaveTriggerTick - PriorWarningTicks)
			{
				if(IsPlayingPermaDeath() && !Entry.Config.AutoSavePermaDeath)
				{
					Entry.LogDebug($"Will not announce the next autosave. AutosavePermaDeath == {Entry.Config.AutoSavePermaDeath}", true);
				}
				Entry.DisplayMessage("AutosaveWarning".FormatTranslate(PriorWarningTicks.ToString()));
			}

			else if (this.totalTicks >= this.nextSaveTriggerTick && !this.isSaving)
			{
				if (this.IsSafeToSave())
				{
					this.ExecuteAutosave();
				}

				else
				{
					Entry.LogDebug("IsSafeToSave false. Delaying autosave.");

					this.DelayAutosave();
				}
			}
		}

		// Monobehaviour.Awake(), called before Start()
		private void Awake()
		{
			this.nextSaveTriggerTick = (Entry.Config.MinutesBetweenAutosaves * 60);

			Entry.LogDebug("Configuration:", true);
			Entry.LogDebug(Entry.Config.ToString(), true);
		}

		// Monobehaviour.Start
		private void Start()
		{
			this.InvokeRepeating(nameof(AutosaveController.Tick), 1f, 1f);
		}

		public void DelayAutosave()
		{
			Entry.LogDebug("Delaying autosave...", true);
			
			this.nextSaveTriggerTick += RetryTicks;
		}

		public void ExecuteAutosave()
		{
			if (!this.isSaving)
			{
				try
				{
					CoroutineHost.StartCoroutine(this.AutosaveCoroutine());
				}

				catch (Exception ex)
				{
					Entry.LogError("Failed to execute save coroutine. Something went wrong.", ex);
					Entry.DisplayMessage("Failed to execute save coroutine, see log for details!", Level.Error); // TODO: Translate

					// TODO: Handle the exception?
				}
			}

			else
			{
				Entry.DisplayMessage("AutosaveInProgress".Translate());
			}
		}
	}
}
