using System.Collections.Generic;
using System.IO;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using SpaceCraft;
using UnityEngine;
using UnityEngine.UI;

namespace OreExtractorTweaks_Plugin
{
    [BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
    [BepInProcess("Planet Crafter.exe")]
    public class Plugin : BaseUnityPlugin
    {
        private static ConfigEntry<bool> configOnlyExtractDetectedOre;
        private static ConfigEntry<bool> configDetectedOreEveryTick;
        private static ConfigEntry<bool> configModifySpawnRates;
        private static ConfigEntry<int> configT1SpawnEveryXSeconds;
        private static ConfigEntry<int> configT2SpawnEveryXSeconds;

        private readonly Harmony harmony = new Harmony(PluginInfo.PLUGIN_GUID);

        private void Awake()
        {
            // Get the configurations
            configOnlyExtractDetectedOre = Config.Bind("General", "Only_Extract_Detected_Ore", true, 
                "Prevent the ore extractor from adding ores other than the one shown to the inventory.");
            configDetectedOreEveryTick = Config.Bind("General", "Detected_Ore_Every_Tick", false, 
                "Removes randomness so every time an ore is generated it is the detected one.");
            configModifySpawnRates = Config.Bind("General", "Modify_Spawn_Rates", false, 
                "Change the spawn rates to configured values.");
            configT1SpawnEveryXSeconds = Config.Bind("General", "T1_Spawn_Every_X_Seconds", 70, 
                "How long to wait between spawns of ore for T1 extractors in seconds.");
            configT2SpawnEveryXSeconds = Config.Bind("General", "T2_Spawn_Every_X_Seconds", 65, 
                "How long to wait between spawns of ore for T2 extractors in seconds.");
            harmony.PatchAll(typeof(OreExtractorTweaks_Plugin.Plugin));

            Logger.LogInfo($"Plugin {PluginInfo.PLUGIN_GUID} is loaded!");
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(MachineGenerator), "GenerateAnObject")]
        private static bool MachineGenerator_GenerateAnObject_Prefix(List<GroupData> ___groupDatas, ref Inventory ___inventory)
        {
            if (configOnlyExtractDetectedOre.Value && DetectedOre(___groupDatas).id != "Iron")
            {
                if (configDetectedOreEveryTick.Value || RandomOreIsDetected(___groupDatas))
                {
                    AddOre(___inventory, DetectedOre(___groupDatas));
                }
                return false;
            }
            return true;
        }

        private static GroupData DetectedOre(List<GroupData> groupDatas)
        {
            return groupDatas[groupDatas.Count - 1];
        }

        private static bool RandomOreIsDetected(List<GroupData> groupDatas)
        {
            return groupDatas[UnityEngine.Random.Range(0, groupDatas.Count)].id == DetectedOre(groupDatas).id;
        }

        private static void AddOre(Inventory inventory, GroupData ore)
        {
            WorldObject worldObject = WorldObjectsHandler.CreateNewWorldObject(GroupsHandler.GetGroupViaId(ore.id), 0);
            inventory.AddItem(worldObject);            
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(MachineGenerator), "SetGeneratorInventory")]
        private static bool MachineGenerator_SetGeneratorInventory_Prefix(MachineGenerator __instance, ref int ___spawnEveryXSec)
        {
            string generatorGroupId = __instance.GetComponent<WorldObjectAssociated>().GetWorldObject().GetGroup().GetId();
            if (configModifySpawnRates.Value)
            {
                if (generatorGroupId == "OreExtractor1")
                {
                    ___spawnEveryXSec = configT1SpawnEveryXSeconds.Value;
                }
                else if (generatorGroupId == "OreExtractor2")
                {
                    ___spawnEveryXSec = configT2SpawnEveryXSeconds.Value;
                }

            }
            return true;
        }

        private void OnDestroy()
        {
            harmony.UnpatchSelf();
        }
    }
    
}
