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

        public static ManualLogSource bepInExLogger;

        private void Awake()
        {
            bepInExLogger = Logger;

            harmony.PatchAll(typeof(OpenInteriorSpaces_Plugin.Plugin));

            Logger.LogInfo($"Plugin {PluginInfo.PLUGIN_GUID} is loaded!");
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(WorldObjectsHandler), "InstantiateWorldObject")]
        private static void WorldObjectsHandler_InstantiateWorldObject_Postfix(WorldObject _worldObject, bool _fromDb, ref GameObject __result)
        {
            // Only do this for Pods.
            if (_worldObject.GetGroup().GetId() == "pod")
            {
                PodInfo newPod = new PodInfo(_worldObject, __result);
            }
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(WorldObjectsHandler), "DestroyWorldObject")]
        private static bool WorldObjectsHandler_DestroyWorldObject_Prefix(WorldObject _worldObject)
        {
            return true;
        }

        private void OnDestroy()
        {
            harmony.UnpatchSelf();
        }
    }
    
}
