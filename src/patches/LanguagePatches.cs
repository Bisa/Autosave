using HarmonyLib;

namespace Autosave
{
	class LanguagePatches
    {
		[HarmonyPostfix]
		[HarmonyPatch(typeof(Language), "SetCurrentLanguage")]
		static void Postfix()
		{
			Translation.ReloadLanguage();
		}
    }
}
