using System.Collections.Generic;
using System.Reflection;
using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using SpaceCraft;

namespace AdvancedOreExtractor_Plugin
{
    [BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
    [BepInProcess("Planet Crafter.exe")]
    public class Plugin : BaseUnityPlugin
    {
        private static ConfigEntry<int> configSpawnRate;

        private readonly Harmony harmony = new Harmony(PluginInfo.PLUGIN_GUID);

        private void Awake()
        {
            // Get configuration values
            configSpawnRate = Config.Bind<int>("Ore_Generator", "spawnRate", 60, "Seconds between creation of each ore item");

            harmony.PatchAll(typeof(AdvancedOreExtractor_Plugin.Plugin));

            Logger.LogInfo($"Plugin {PluginInfo.PLUGIN_GUID} is loaded!");
        }
        
        [HarmonyPrefix]
        [HarmonyPatch(typeof(MachineGenerator), "SetGeneratorInventory")]
        private static bool MachineGenerator_SetGeneratorInventory(ref int ___spawnEveryXSec)
        {
            ___spawnEveryXSec = configSpawnRate.Value;
            return true;
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(MachineGenerator), "SetGeneratorInventory")]
        private static void MachineGenerator_SetGeneratorInventory(WorldObject ___worldObject, ref List<GroupData> ___groupDatas)
        {
            if (___worldObject.GetGroup().id == "OreExtractor1")
            {
                FieldInfo staticDataHandlerInstanceFieldInfo = HarmonyLib.AccessTools.Field(typeof(StaticDataHandler), "Instance");
                StaticDataHandler referenceToInstance = HarmonyLib.AccessTools.StaticFieldRefAccess<StaticDataHandler, StaticDataHandler>(
                    staticDataHandlerInstanceFieldInfo);
                List<GroupData> staticGroups = HarmonyLib.AccessTools.FieldRefAccess<StaticDataHandler, List<GroupData>>(referenceToInstance, "groupsData");
                GroupData iridiumGroupData = staticGroups.Find((GroupData x) => x.id == "Iridium");
                GroupData uraniumGroupData = staticGroups.Find((GroupData x) => x.id == "Uranim");
                ___groupDatas.Add(iridiumGroupData);
                ___groupDatas.Add(uraniumGroupData);
            }
        }

        private void OnDestroy()
        {
            harmony.UnpatchSelf();
        }
    }
    
}
