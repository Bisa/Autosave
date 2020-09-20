using HarmonyLib;
using UnityEngine.UI;

namespace Autosave
{
	class MainMenuLoadPanelPatches
    {
		[HarmonyPostfix]
		[HarmonyPatch(typeof(MainMenuLoadPanel), "UpdateLoadButtonState")]
		static void Postfix(MainMenuLoadButton lb)
		{
            // ignore invalid gameinfos
            SaveLoadManager.GameInfo gameInfo = SaveLoadManager.main.GetGameInfo(lb.saveGame);
            if(gameInfo == null) return;

            Text gameModeTextComponent = lb.load.FindChild("SaveGameMode").GetComponent<Text>();
            string gamemode = gameModeTextComponent.text;
            string slotname;

            if(AutosaveController.IsAutosaveSlot(lb.saveGame))
            {
                slotname = gamemode + "\n[ASave|" + lb.saveGame.Substring(0, 8) + "]";
            }

            else
            {
                slotname = gamemode + "\n[" + lb.saveGame + "]";
            }

		    gameModeTextComponent.text = slotname;
		}
    }
}