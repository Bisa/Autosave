using HarmonyLib;

namespace Autosave
{
	
	class PlayerPatches
    {
        [HarmonyPostfix]
		[HarmonyPatch(typeof(Player), "Awake")]
		static void Postfix(Player __instance)
		{
			__instance.gameObject.AddComponent<AutosaveController>();
		}
    }
}