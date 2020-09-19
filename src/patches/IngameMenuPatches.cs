using HarmonyLib;

namespace Autosave
{
	class IngameMenuPatches
    {
		[HarmonyPrefix]
        [HarmonyPatch(typeof(IngameMenu), "SaveGame")]
		static void Prefix()
        {
            Player.main.GetComponent<AutosaveController>().ChangeSlotIfOnAutosaveSlot();
        }
	}
}