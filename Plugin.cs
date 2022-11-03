using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using ServerSync;
using UnityEngine;

namespace DurabilityTweaks
{
    [BepInPlugin(ModGUID, ModName, ModVersion)]
    public class DurabilityTweaksPlugin : BaseUnityPlugin
    {
        internal const string ModName = "DurabilityTweaks";
        internal const string ModVersion = "1.0.2";
        private const string Author = "Azumatt";
        private const string ModGUID = Author + "." + ModName;
        private static string ConfigFileName = ModGUID + ".cfg";
        private static string ConfigFileFullPath = Paths.ConfigPath + Path.DirectorySeparatorChar + ConfigFileName;


        private readonly Harmony _harmony = new(ModGUID);

        public static readonly ManualLogSource DurabilityLogger =
            BepInEx.Logging.Logger.CreateLogSource(ModName);

        private static readonly ConfigSync ConfigSync = new(ModGUID)
            { DisplayName = ModName, CurrentVersion = ModVersion, MinimumRequiredVersion = ModVersion };

        private void Awake()
        {
            _serverConfigLocked = config("General", "Force Server Config", true, "Force Server Config");
            _ = ConfigSync.AddLockingConfigEntry(_serverConfigLocked);

            torchDurabilityDrain = config("Durability", "TorchDurabilityDrain", 0.033f,
                "Torch durability drain over time.");
            torchDurabilityLoss = config("Durability", "TorchDurabilityLoss", 1f,
                "Torch durability loss when used to attack.");
            weaponDurabilityLoss = config("Durability", "WeaponDurabilityLoss", 1f,
                "Weapon durability loss per use.");
            bowDurabilityLoss =
                config("Durability", "BowDurabilityLoss", 1f, "Bow durability loss per use.");
            hammerDurabilityLoss = config("Durability", "HammerDurabilityLoss", 1f,
                "Hammer durability loss per use.");
            hoeDurabilityLoss =
                config("Durability", "HoeDurabilityLoss", 1f, "Hoe durability loss per use.");
            pickaxeDurabilityLoss = config("Durability", "PickaxeDurabilityLoss", 1f,
                "Pickaxe durability loss per use.");
            axeDurabilityLoss =
                config("Durability", "AxeDurabilityLoss", 1f, "Axe durability loss per use.");
            toolDurabilityLoss = config("Durability", "ToolDurabilityLoss", 1f,
                "Other tool durability loss per use.");

            shieldDurabilityLossMult = config("Durability", "ShieldDurabilityLossMult", 1f,
                "Shield durability loss multiplier.");
            armorDurabilityLossMult = config("Durability", "ArmorDurabilityLossMult", 1f,
                "Armor durability loss multiplier.");
            sharedArmorDurability = config("Options", "SharedArmorDurability", false,
                "If true, durability loss is shared between all armor worn.");


            modEnabled = config("General", "Enabled", true, "Enable this mod");

            if (!modEnabled.Value)
                return;

            _harmony.PatchAll();
            SetupWatcher();
        }

        private void OnDestroy()
        {
            Config.Save();
        }

        private void SetupWatcher()
        {
            FileSystemWatcher watcher = new(Paths.ConfigPath, ConfigFileName);
            watcher.Changed += ReadConfigValues;
            watcher.Created += ReadConfigValues;
            watcher.Renamed += ReadConfigValues;
            watcher.IncludeSubdirectories = true;
            watcher.SynchronizingObject = ThreadingHelper.SynchronizingObject;
            watcher.EnableRaisingEvents = true;
        }

        private void ReadConfigValues(object sender, FileSystemEventArgs e)
        {
            if (!File.Exists(ConfigFileFullPath)) return;
            try
            {
                DurabilityLogger.LogDebug("ReadConfigValues called");
                Config.Reload();
            }
            catch
            {
                DurabilityLogger.LogError($"There was an issue loading your {ConfigFileName}");
                DurabilityLogger.LogError("Please check your config entries for spelling and format!");
            }
        }

        [HarmonyPatch(typeof(ItemDrop), nameof(ItemDrop.Awake))]
        static class DurabiityTweaksItemDropPatch
        {
            static void Postfix(ItemDrop __instance)
            {
                if (!modEnabled.Value || __instance.name == null || __instance.m_itemData?.m_shared == null) return;
                //DurabilityLogger.LogDebug($"{__instance.name}, type: {Enum.GetName(typeof(ItemDrop.ItemData.ItemType), __instance.m_itemData.m_shared.m_itemType)} drain: {__instance.m_itemData.m_shared.m_durabilityDrain}, use: {__instance.m_itemData.m_shared.m_useDurabilityDrain}");

                if (__instance.name.StartsWith("Pickaxe"))
                    __instance.m_itemData.m_shared.m_useDurabilityDrain = pickaxeDurabilityLoss.Value;
                else if (__instance.name.StartsWith("Axe"))
                    __instance.m_itemData.m_shared.m_useDurabilityDrain = axeDurabilityLoss.Value;
                else
                {
                    switch (__instance.m_itemData.m_shared.m_itemType)
                    {
                        case ItemDrop.ItemData.ItemType.Torch:
                            __instance.m_itemData.m_shared.m_durabilityDrain = torchDurabilityDrain.Value;
                            __instance.m_itemData.m_shared.m_useDurabilityDrain = torchDurabilityLoss.Value;
                            break;
                        case ItemDrop.ItemData.ItemType.OneHandedWeapon:
                        case ItemDrop.ItemData.ItemType.TwoHandedWeapon:
                            __instance.m_itemData.m_shared.m_useDurabilityDrain = weaponDurabilityLoss.Value;
                            break;
                        case ItemDrop.ItemData.ItemType.Bow:
                            __instance.m_itemData.m_shared.m_useDurabilityDrain = bowDurabilityLoss.Value;
                            break;
                        case ItemDrop.ItemData.ItemType.Tool:
                            if (__instance.name.StartsWith("Hammer"))
                                __instance.m_itemData.m_shared.m_useDurabilityDrain = hammerDurabilityLoss.Value;
                            else if (__instance.name.StartsWith("Hoe"))
                                __instance.m_itemData.m_shared.m_useDurabilityDrain = hoeDurabilityLoss.Value;
                            else
                                __instance.m_itemData.m_shared.m_useDurabilityDrain = toolDurabilityLoss.Value;
                            break;
                        case ItemDrop.ItemData.ItemType.None:
                            break;
                        case ItemDrop.ItemData.ItemType.Material:
                            break;
                        case ItemDrop.ItemData.ItemType.Consumable:
                            break;
                        case ItemDrop.ItemData.ItemType.Shield:
                            break;
                        case ItemDrop.ItemData.ItemType.Helmet:
                            break;
                        case ItemDrop.ItemData.ItemType.Chest:
                            break;
                        case ItemDrop.ItemData.ItemType.Ammo:
                            break;
                        case ItemDrop.ItemData.ItemType.Customization:
                            break;
                        case ItemDrop.ItemData.ItemType.Legs:
                            break;
                        case ItemDrop.ItemData.ItemType.Hands:
                            break;
                        case ItemDrop.ItemData.ItemType.Trophie:
                            break;
                        case ItemDrop.ItemData.ItemType.Misc:
                            break;
                        case ItemDrop.ItemData.ItemType.Shoulder:
                            break;
                        case ItemDrop.ItemData.ItemType.Utility:
                            break;
                        case ItemDrop.ItemData.ItemType.Attach_Atgeir:
                            break;
                        default:
                            throw new ArgumentOutOfRangeException();
                    }
                }
            }
        }

        [HarmonyPatch(typeof(Player), nameof(Player.DamageArmorDurability))]
        static class DurabiityTweaksDamageArmorDurabilityPatch
        {
            static void Prefix(Player __instance, ref float[] __state, ItemDrop.ItemData ___m_chestItem,
                ItemDrop.ItemData ___m_legItem, ItemDrop.ItemData ___m_shoulderItem, ItemDrop.ItemData ___m_helmetItem)
            {
                __state = new float[4];
                if (!modEnabled.Value) return;
                __state[0] = ___m_chestItem?.m_durability ?? -1f;
                __state[1] = ___m_legItem?.m_durability ?? -1f;
                __state[2] = ___m_shoulderItem?.m_durability ?? -1f;
                __state[3] = ___m_helmetItem?.m_durability ?? -1f;
            }

            static void Postfix(Player __instance, float[] __state, ref ItemDrop.ItemData ___m_chestItem,
                ref ItemDrop.ItemData ___m_legItem, ref ItemDrop.ItemData ___m_shoulderItem,
                ref ItemDrop.ItemData ___m_helmetItem, HitData hit)
            {
                if (!modEnabled.Value) return;
                float amount = (hit.GetTotalPhysicalDamage() + hit.GetTotalElementalDamage()) *
                               armorDurabilityLossMult.Value;
                if (amount <= 0)
                    return;

                if (sharedArmorDurability.Value)
                {
                    int count = 0;
                    if (___m_chestItem != null)
                        count++;
                    if (___m_legItem != null)
                        count++;
                    if (___m_shoulderItem != null)
                        count++;
                    if (___m_helmetItem != null)
                        count++;

                    // if (___m_chestItem != null)
                    //     ___m_chestItem.m_durability = Mathf.Max(0, __state[0] - amount / count);
                    // if (___m_legItem != null)
                    //     ___m_legItem.m_durability = Mathf.Max(0, __state[1] - amount / count);
                    // if (___m_shoulderItem != null)
                    //     ___m_shoulderItem.m_durability = Mathf.Max(0, __state[2] - amount / count);
                    // if (___m_helmetItem != null)
                    //     ___m_helmetItem.m_durability = Mathf.Max(0, __state[3] - amount / count);

                    if (___m_chestItem != null)
                        ___m_chestItem.m_durability = Mathf.Max(0,
                            __state[0] - ((__state[0] - ___m_chestItem.m_durability) * armorDurabilityLossMult.Value) /
                            count);
                    if (___m_legItem != null)
                        ___m_legItem.m_durability = Mathf.Max(0,
                            __state[1] - ((__state[1] - ___m_chestItem.m_durability) * armorDurabilityLossMult.Value) /
                            count);
                    if (___m_shoulderItem != null)
                        ___m_shoulderItem.m_durability = Mathf.Max(0,
                            __state[2] - ((__state[2] - ___m_chestItem.m_durability) * armorDurabilityLossMult.Value) /
                            count);
                    if (___m_helmetItem != null)
                        ___m_helmetItem.m_durability = Mathf.Max(0,
                            __state[3] - ((__state[3] - ___m_chestItem.m_durability) * armorDurabilityLossMult.Value) /
                            count);
                }
                else
                {
                    if (___m_chestItem != null && __state[0] > ___m_chestItem.m_durability)
                    {
                        //DurabilityLogger.LogDebug($"chest old {__state[0]} new {___m_chestItem.m_durability} final {__state[0] - amount}");
                        //___m_chestItem.m_durability = Mathf.Max(0, __state[0] - amount);
                        ___m_chestItem.m_durability = Mathf.Max(0,
                            __state[0] - (__state[0] - ___m_chestItem.m_durability) * armorDurabilityLossMult.Value);
                    }

                    if (___m_legItem != null && __state[1] > ___m_legItem.m_durability)
                    {
                        //DurabilityLogger.LogDebug($"leg old {__state[1]} new {___m_legItem.m_durability} final {__state[1] - amount}");
                        //___m_legItem.m_durability = Mathf.Max(0, __state[1] - amount);
                        ___m_legItem.m_durability = Mathf.Max(0,
                            __state[1] - (__state[1] - ___m_legItem.m_durability) * armorDurabilityLossMult.Value);
                    }

                    if (___m_shoulderItem != null && __state[2] > ___m_shoulderItem.m_durability)
                    {
                        //DurabilityLogger.LogDebug($"shoulder old {__state[2]} new {___m_shoulderItem.m_durability} final {__state[2] - amount}");
                        //___m_shoulderItem.m_durability = Mathf.Max(0, __state[2] - amount);
                        ___m_shoulderItem.m_durability = __state[2] - (__state[2] - ___m_shoulderItem.m_durability) *
                            armorDurabilityLossMult.Value;
                    }

                    if (___m_helmetItem != null && __state[3] > ___m_helmetItem.m_durability)
                    {
                        //DurabilityLogger.LogDebug($"helmet old {__state[3]} new {___m_helmetItem.m_durability} final {__state[3] - amount}");

                        // ___m_helmetItem.m_durability = Mathf.Max(0, __state[3] - amount);
                        ___m_helmetItem.m_durability = __state[3] -
                                                       (__state[3] - ___m_helmetItem.m_durability) *
                                                       armorDurabilityLossMult.Value;
                    }
                }
            }
        }


        [HarmonyPatch(typeof(Humanoid), nameof(Humanoid.BlockAttack))]
        static class DurabiityTweaksBlockAttackPatch
        {
            static void Prefix(Humanoid __instance, ref float __state, ItemDrop.ItemData ___m_leftItem)
            {
                if (modEnabled.Value && __instance.IsPlayer() && ___m_leftItem != null)
                {
                    __state = ___m_leftItem.m_durability;
                }
            }

            static void Postfix(Humanoid __instance, float __state, ref ItemDrop.ItemData ___m_leftItem)
            {
                if (!modEnabled.Value || !__instance.IsPlayer()) return;
                if (!(__state > 0) || ___m_leftItem == null || !(__state > ___m_leftItem.m_durability) ||
                    ___m_leftItem.m_shared.m_itemType != ItemDrop.ItemData.ItemType.Shield) return;
                DurabilityLogger.LogDebug(
                    $"shield old {__state} new {___m_leftItem.m_durability} final {__state - (__state - ___m_leftItem.m_durability) * shieldDurabilityLossMult.Value}");

                ___m_leftItem.m_durability = Mathf.Max(0,
                    __state - (__state - ___m_leftItem.m_durability) * shieldDurabilityLossMult.Value);
            }
        }


        [HarmonyPatch(typeof(Attack), nameof(Attack.DoAreaAttack))]
        static class DurabiityTweaksDoAreaAttackPatch
        {
            public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
            {
                DurabilityLogger.LogDebug($"Transpiling DoAreaAttack");

                List<CodeInstruction> codes = new List<CodeInstruction>(instructions);
                for (int i = 0; i < codes.Count; i++)
                {
                    if (codes[i].opcode != OpCodes.Dup || codes[i + 2].opcode != OpCodes.Ldc_R4 ||
                        (float)(codes[i + 2].operand) != 1 || codes[i + 3].opcode != OpCodes.Sub) continue;
                    DurabilityLogger.LogDebug($"got -1, replacing with {weaponDurabilityLoss.Value}");
                    codes[i + 2].operand = weaponDurabilityLoss.Value;
                }

                return codes.AsEnumerable();
            }
        }

        #region ConfigOptions

        private static ConfigEntry<bool>? _serverConfigLocked;

        private static ConfigEntry<float> torchDurabilityDrain;
        internal static ConfigEntry<float> weaponDurabilityLoss;
        internal static ConfigEntry<float> bowDurabilityLoss;

        internal static ConfigEntry<float> toolDurabilityLoss;
        internal static ConfigEntry<float> torchDurabilityLoss;
        internal static ConfigEntry<float> hammerDurabilityLoss;
        internal static ConfigEntry<float> hoeDurabilityLoss;
        internal static ConfigEntry<float> pickaxeDurabilityLoss;
        internal static ConfigEntry<float> axeDurabilityLoss;

        internal static ConfigEntry<float> shieldDurabilityLossMult;
        internal static ConfigEntry<float> armorDurabilityLossMult;

        internal static ConfigEntry<bool> sharedArmorDurability;

        internal static ConfigEntry<bool> modEnabled;

        private ConfigEntry<T> config<T>(string group, string name, T value, ConfigDescription description,
            bool synchronizedSetting = true)
        {
            ConfigDescription extendedDescription =
                new(
                    description.Description +
                    (synchronizedSetting ? " [Synced with Server]" : " [Not Synced with Server]"),
                    description.AcceptableValues, description.Tags);
            ConfigEntry<T> configEntry = Config.Bind(group, name, value, extendedDescription);
            //var configEntry = Config.Bind(group, name, value, description);

            SyncedConfigEntry<T> syncedConfigEntry = ConfigSync.AddConfigEntry(configEntry);
            syncedConfigEntry.SynchronizedConfig = synchronizedSetting;

            return configEntry;
        }

        private ConfigEntry<T> config<T>(string group, string name, T value, string description,
            bool synchronizedSetting = true)
        {
            return config(group, name, value, new ConfigDescription(description), synchronizedSetting);
        }

        private class ConfigurationManagerAttributes
        {
            public bool? Browsable = false;
        }

        #endregion
    }
}