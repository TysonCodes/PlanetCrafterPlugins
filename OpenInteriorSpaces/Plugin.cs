using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using MijuTools;
using SpaceCraft;
using UnityEngine;
using PluginFramework;

namespace OpenInteriorSpaces_Plugin
{
    [BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
    [BepInProcess("Planet Crafter.exe")]
    [BepInDependency(PluginFramework.PluginInfo.PLUGIN_GUID, PluginFramework.PluginInfo.PLUGIN_VERSION)]    // In BepInEx 5.4.x this ia a minimum version, BepInEx 6.x has range semantics.
    public class Plugin : BaseUnityPlugin
    {
        public static ManualLogSource bepInExLogger;

        public const DataConfig.BuildPanelSubType FIRST_CUSTOM_SUBTYPE = (DataConfig.BuildPanelSubType) 100;

        private const string POD_GAME_OBJECT_NAME = "Pod";

        private readonly Harmony harmony = new Harmony(PluginInfo.PLUGIN_GUID);

        private void Awake()
        {
            bepInExLogger = Logger;

            Framework.GameStateLoadingStarted += OnGameStateLoadingStarted;
            Framework.WorldObjectInstantiated += OnWorldObjectBeingInstantiated;
            Framework.WorldObjectBeingDestroyed += OnWorldObjectBeingDestroyed;

            harmony.PatchAll(typeof(OpenInteriorSpaces_Plugin.Plugin));

            Logger.LogInfo($"Plugin {PluginInfo.PLUGIN_GUID} is loaded!");
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(Panel), "SetPanel")]
        private static void Panel_SetPanel_Postfix(Panel __instance)
        {
            PodWidget pod = __instance.GetComponentInParent<PodWidget>();
            if (pod != null)
            {
                pod.RefreshPodWallsAndCorners();
                pod.RefreshAdjacentPodIfApplicable(__instance);
            }
        }

        private void OnWorldObjectBeingInstantiated(ref WorldObject worldObject, ref GameObject gameObject, bool fromSaveFile)
        {
            // Only do this for Pods.
            if (worldObject.GetGroup().GetId() == "pod" && gameObject.TryGetComponent<PodWidget>(out PodWidget pod))
            {
                pod.Initialize();
            }
        }

        private void OnWorldObjectBeingDestroyed(ref WorldObject worldObject)
        {
            if (worldObject.GetGroup().GetId() == "pod" && gameObject.TryGetComponent<PodWidget>(out PodWidget pod))
            {
                pod.Remove();
            }
        }

        private void OnGameStateLoadingStarted()
        {
            // Reset the PodInfo and PillarInfo static values.
            PodWidget.Reset();
            PillarInfo.Reset();
            InjectCorridorWallWidget();
            InjectPodWidget();
        }

        private void InjectCorridorWallWidget()
        {
            PanelsResources panelMgr = Managers.GetManager<PanelsResources>();
            GameObject corridorWallGameObject = panelMgr.GetPanelGameObject(DataConfig.BuildPanelSubType.WallCorridor);
            if (!corridorWallGameObject.TryGetComponent<CorridorWallWidget>(out CorridorWallWidget result))
            {
                CorridorWallWidget.InjectWidgetIntoCorridorWallPrefab(ref corridorWallGameObject);
            }
        }
        
        private void InjectPodWidget()
        {
            GameObject podPrefab = Framework.GameObjectByName[POD_GAME_OBJECT_NAME];
            if (!podPrefab.TryGetComponent<PodWidget>(out PodWidget result))
            {
                PodWidget.InjectWidgetIntoPodPrefab();
            }
        }

        private void OnDestroy()
        {
            harmony.UnpatchSelf();
        }
    }
}
