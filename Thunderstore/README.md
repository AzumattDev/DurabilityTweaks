Allows you to tweak the durability of torches, tools, weapons, bows, shields, and armour.

### Made at the request of `Tyson#2862` in the OdinPlus discord.

`If this mod is installed on the server, it will sync the configs.`

This mod provides config settings for each of the following:

**Torches**: affects the durability drain over time caused by holding a torch; another setting affects durability loss when attacking with a torch.

**Hammers**: affects the durability loss caused by each use of a hammer.

**Hoes**: affects the durability loss caused by each use of a hoe.

**Pickaxes**: affects the durability loss caused by each use of a pickaxe.

**Axes**: affects the durability loss caused by each use of an axe.

**Tools**: affects the durability loss caused by each use of any other tool.

**Weapons**: affects the durability loss caused by each use of the weapon.

**Bows**: affects the durability loss caused by each use of the bow.

**Shields**: affects the durability loss caused by blocking.

**Armour**: affects the durability loss caused by taking damage.

To change these settings, edit the file BepInEx/config/azumatt.DurabilityTweaks.cfg (created after running the game once with this mod) using a text editor.

You can adjust the config values in-game using the [Config Manager](https://github.com/BepInEx/BepInEx.ConfigurationManager/releases).

Setting any value to 0 should disable durability loss entirely for that item type.

Armour durability loss in Valheim is randomly applied to one piece of armour based on the damage received. This mod has a setting to multiply the durability loss by a given variable. i.e. 1.0 means no change, 0.5 means half as much loss, etc. There is also an option in the config to share durability loss between armor pieces equally.

Shield durability loss in Valheim is applied based on the damage blocked. This mod has a setting to multiply the durability loss by a given variable. i.e. 1.0 means no change, 0.5 means half as much loss, etc.

The other variables are straight replacements for the amount of durability lost or drained. Default values for this mod are the vanilla game values.


## Technical

To install this mod, the easiest way is to just use [r2modman](https://valheim.thunderstore.io/package/ebkr/r2modman/), the Thunderstore mod manager. It should take care of all dependencies.

To install manually, place the dll file in the BepInEx/plugins folder. You will need BepInEx.

Original Code is at https://github.com/aedenthorn/ValheimMods.

Server Sync verison's code is at https://github.com/AzumattDev/DurabilityTweaks


`Feel free to reach out to me on discord if you need manual download assistance.`

# Author Information

### Azumatt

`DISCORD:` Azumatt#2625

`STEAM:` https://steamcommunity.com/id/azumatt/


For Questions or Comments, find me in the Odin Plus Team Discord:
[![https://i.imgur.com/XXP6HCU.png](https://i.imgur.com/XXP6HCU.png)](https://discord.gg/Pb6bVMnFb2)

***
> # Update Information (Latest listed first)
> ### v1.0.3
> - Fixed issues with picking up items in Mistlands
> ### v1.0.2
> - Update ServerSync internally
> ### v1.0.1
> - Fix bug with armor durability not working
> ### v1.0.0
> - Initial Release