using System.Collections;
using System.Collections.Generic;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using SpaceCraft;
using UnityEngine;

namespace OpenInteriorSpaces_Plugin
{
    [BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
    [BepInProcess("Planet Crafter.exe")]
    public class Plugin : BaseUnityPlugin
    {
        private readonly Harmony harmony = new Harmony(PluginInfo.PLUGIN_GUID);

        private static ManualLogSource bepInExLogger;

        private void Awake()
        {
            bepInExLogger = Logger;

            harmony.PatchAll(typeof(OpenInteriorSpaces_Plugin.Plugin));

            Logger.LogInfo($"Plugin {PluginInfo.PLUGIN_GUID} is loaded!");
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(EnvironmentDayNightCycle), "Start")]
        private static bool EnvironmentDayNightCycle_Start_Prefix(EnvironmentDayNightCycle __instance)
        {

            return true;
        }

        private void OnDestroy()
        {
            harmony.UnpatchSelf();
        }
    }
    
}
