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

        private Dictionary<WorldObject, PodInfo> podsByWorldObject = new Dictionary<WorldObject, PodInfo>();

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
                bepInExLogger.LogInfo($"Adding Pod. WorldObject: {_worldObject.GetId()}, Location:{_worldObject.GetPosition()}");
                PodInfo newPod = new PodInfo();
                newPod.associatedWorldObj = _worldObject;
                newPod.associatedGameObj = __result;
                var panels = newPod.associatedGameObj.GetComponentsInChildren<Panel>();
                for (int i = 0; i < panels.Length; i++)
                {
                    newPod.panelByDirection[(PodDirection)i] = panels[i];
                    PodInfo.podInfoByPanel[panels[i]] = newPod;
                }
                newPod.DetectAdjacentPods();
                newPod.GeneratePillarInfo();
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
