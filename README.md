# Autosave

This is the missing autosave feature for Subnautica.

This mod will:
- Automatically save your game every N seconds
- Keep at most X of these autosaves for you to return to whenever you want (does not apply to games where perma death is active, see options!)

Never loose more than N (* X) seconds worth of gameplay due to bugs, glitches or mistakes again!

## Installation

1. Handle dependencies
   * Required:
     * [QMods](https://www.nexusmods.com/subnautica/mods/201)
     * [SMLHelper](https://www.nexusmods.com/subnautica/mods/113)
   * Optional:
     * [VersionChecker](https://www.nexusmods.com/subnautica/mods/467) (To get notified on new releases to this mod)

1. Download the latest zip file from the [Files tab](https://www.nexusmods.com/subnautica/mods/561/?tab=files) at Nexus or [Releases](https://github.com/Bisa/Autosave/releases) from github.

1. Unzip the contents of the zip to the game's main directory (where Subnautica.exe can be found)

### Configuration

1) Use the in-game *Options->Mods->Autosave* menu
2) Available options:

   **"MinutesBetweenAutosaves": 15** - The time (in minutes) between autosave attempts. Must be at least 1.

   **"MaxAutosaveSlots": 3** - The maximum amount of autosave slots. Must be at least 1. (This has no effect if perma death is active in your game).

   **"MinimumPlayerHealthPercent": 25** - If player health is below this percent, no save will occur. Change to 0 to disable this option.

   **"AutoSavePermaDeath": true** - If true, autosaving will be scheduled on games with permanent death - but only one save slot is used.

## Contributing

Feel free to clone [Bisa/Autosave](https://github.com/Bisa/Autosave) from github and submit a pull-request with your contributions.

Kindly add a MIT license statement to either the file you are contributing with or, in the case of many files - a license file with details on which parts of the "software" you contributed with.

### Translation

If you want to contribute a translation for this mod, please follow these steps:
1) Copy the *src\Languages\English.json*
2) Change the name to your language. It needs to match the file name in *Subnautica\Subnautica_Data\StreamingAssets\SNUnmanagedData\LanguageFiles*
3) Translate the file. Do not touch the keys ("AutosaveStarting"), only the translated values ("Autosave sequence...")

## Building

If you want to build from source you may use the following variables:

* **SubnauticaPath** - The path to your Subnautica install directory where msbuild can find its dependencies, this will default to "```C:\Program Files (x86)\Steam\steamapps\common\Subnautica```".

* **SubnauticaQModsPath** - The path to Subnautica the *Subnautica\QMods* directory, this will default to "```$(SubnauticaPath)\QMods```" so you can fire up Subnautica and debug the mod in-game. **WARNING!** The *$(SubnauticaQModsPath)\Autosave* directory will be removed during a succesfull build - resetting the mod to a clean installation.

* **Author** - This will be used to set the *mod.json* author for QMods, it will default to an empty string. 

* **Tag** - This will be used to set the ```AssemblyVersion``` and *mod.json* version for QMods, it will default to the value of ```git describe --tags --abbrev=0``` being run within your working directory.

* **InfoVersion** - This will be used to set the ```AssemblyInformationalVersion```, it will default to the value of ```git describe --long --tags --dirty``` being run within your working directory and the value of ```-$(Configuration)``` will always be appended.

* **VersionCheckUrl** - If supplied, this will add a section to *mod.json* for use by [VersionChecker](https://www.nexusmods.com/subnautica/mods/467) to check the latest version of this mod.

Example build command:

```msbuild -p:Platform=AnyCPU -p:Configuration=Release -p:SubnauticaPath="D:\Games\Steam\steamapps\common\Subnautica" -t:Clean,Build```

## FAQ

### **Q. Is this mod safe to add or remove from an existing save file?**

**A.** Yes, the mod is using Subnauticas original ```SaveGameAsync()``` method and copies the resulting *slot{0000}* directory, it will not touch your original.

### **Q. Does this mod have any known conflicts?**

**A.** No, but while you can install [Subnautica Autosave](https://www.nexusmods.com/subnautica/mods/237), [Safe Autosave](https://www.nexusmods.com/subnautica/mods/94) and this mod - you probably should not due to redundancy.

### **Q. Does this mod impact performance?**

**A.** It should not during normal circumstances, since using the original Subnautica ```SaveGameAsync()``` method gameplay will "freeze" for a few seconds when saving and then continue normally - just as if you pressed the save button yourself.

## Credits

- Forked from [DingoDjango/snAutosave](https://github.com/DingoDjango/snAutosave) ([Nexus](https://www.nexusmods.com/subnautica/mods/237))
- Powered by
  - [QMods](https://www.nexusmods.com/subnautica/mods/201)
  - [Harmony](https://github.com/pardeike/Harmony)

## **Source/Changelog**

[Bisa/Autosave](https://github.com/Bisa/Autosave) on github
