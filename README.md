## **A Subnautica Autosaving Mod**

#### **Description:**
An automated save system which saves in time intervals. The autosave slots are separate from the normal save by default, but can be configured otherwise.
You can define several custom parameters in the settings file (see the Configuration section).

#### **Installation:**
1) Make sure [QMods](https://www.nexusmods.com/subnautica/mods/201) (v4.x) is installed.
2) Download the zip file from the [Files tab](https://www.nexusmods.com/subnautica/mods/<unknown>/?tab=files) in nexus or [Releases](https://github.com/Bisa/Autosave/releases) on github.
3) Unzip the contents of the zip to the game's main directory (where Subnautica.exe can be found)

#### **(Optional) Configuration:**
1) Navigate to the mod's directory (*Subnautica\QMods\Autosave*).
2) Edit settings.json with your favourite text editor.
3) Define custom values according to your preference. The settings available are -
   *  "SecondsBetweenAutosaves": 900 -- The time (in seconds) between autosave attempts. Must be at least 120.
   *  "MaxSaveFiles": 3  -- The maximum amount of autosave slots. Must be at least 1.
   *  "MinimumPlayerHealthPercent": 25 -- If player health is below this percent, no save will occur. Change to 0 to disable this option.
   *  "HardcoreMode": false -- If true, autosaves will override the normal save slot instead of using separate slots.

#### **(Optional) Translation:**
If you want to contribute a translation for this mod, please follow these steps:
1) Cop the *src\Languages\English.json*
2) Change the name to your language. It needs to match the file name in *Subnautica\Subnautica_Data\StreamingAssets\SNUnmanagedData\LanguageFiles*
3) Translate the file. Do not touch the keys ("AutosaveStarting"), only the translated values ("Autosave sequence...")
4) Fork the github repo and add a pull request with your new translation.

#### **(Optional) Building:**
If you want to build from source you may use the following variables:
* **SubnauticaPath** - 
The path to your Subnautica install directory where msbuild can find its dependencies, this will default to "```C:\Program Files (x86)\Steam\steamapps\common\Subnautica```".
* **SubnauticaQModsPath** - 
The path to Subnautica the *Subnautica\QMods* directory, this will default to "```$(SubnauticaPath)\QMods```" so you can fire up Subnautica and debug the mod in-game. **WARNING!** The *$(SubnauticaQModsPath)\Autosave* directory will be removed during a succesfull build - resetting the mod to a clean installation.
* **Author** - This will be used to set the *mod.json* author for QMods, it will default to an empty string. 
* **Tag** - This will be used to set the ```AssemblyVersion``` and *mod.json* version for QMods, it will default to the value of ```git describe --tags --abbrev=0``` being run within your working directory.
* **InfoVersion** - This will be used to set the ```AssemblyInformationalVersion```, it will default to the value of ```git describe --long --tags --dirty``` being run within your working directory and the value of ```-$(Configuration)``` will always be appended.

Example build command:

```msbuild -p:Platform=AnyCPU -p:Configuration=Release -p:SubnauticaPath="D:\Games\Steam\steamapps\common\Subnautica" -t:Clean,Build```

#### **FAQ:**
* **Q. Is this mod safe to add or remove from an existing save file?**
* A. Yes, the mod is using Subnauticas original ```SaveGameAsync()``` method and copies the resulting *slot{0000}* directory, it will not touch your original.
* **Q. Does this mod have any known conflicts?**
* A. No, but you should probably not use this mod with [Safe Autosave](https://www.nexusmods.com/subnautica/mods/94) due to redundancy.
* **Q. Does this mod impact performance?**
* A. It should not during normal circumstances, since using the original Subnautica ```SaveGameAsync()``` methos gameplay will "freeze" for a few seconds when saving and then continue normally - just as if you pressed the save button yourself.

#### **Credits:**
- Forked from [DingoDjango's snAutosave](https://github.com/DingoDjango/snAutosave)
- Powered by [Harmony](https://github.com/pardeike/Harmony)
- Powered by [QMods](https://www.nexusmods.com/subnautica/mods/201)

#### **Source/Changelog:**
[github](https://github.com/Bisa/Autosave)
