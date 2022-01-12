using System.Collections.Generic;
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

        private readonly Harmony harmony = new Harmony(PluginInfo.PLUGIN_GUID);

        private void Awake()
        {
            harmony.PatchAll(typeof(AdvancedOreExtractor_Plugin.Plugin));

            Logger.LogInfo($"Plugin {PluginInfo.PLUGIN_GUID} is loaded!");
        }
        
        [HarmonyPrefix]
        [HarmonyPatch(typeof(MachineGenerator), "SetGeneratorInventory")]
        private static bool MachineGenerator_SetGeneratorInventory(ref int ___spawnEveryXSec)
        {
            ___spawnEveryXSec = 1;
            return true;
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(MachineGenerator), "SetGeneratorInventory")]
        private static void MachineGenerator_SetGeneratorInventory(WorldObject ___worldObject, ref List<GroupData> ___groupDatas)
        {

        }

        private void OnDestroy()
        {
            harmony.UnpatchSelf();
        }
    }
    
}
