using BepInEx;
using BepInEx.Logging;
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

        private void Awake()
        {
            bepInExLogger = Logger;

            Framework.GameStateLoadingStarted += OnGameStateLoadingStarted;
            Framework.WorldObjectInstantiated += OnWorldObjectBeingInstantiated;
            Framework.WorldObjectBeingDestroyed += OnWorldObjectBeingDestroyed;

            Logger.LogInfo($"Plugin {PluginInfo.PLUGIN_GUID} is loaded!");
        }

        private void OnWorldObjectBeingInstantiated(ref WorldObject worldObject, ref GameObject gameObject, bool fromSaveFile)
        {
            // Only do this for Pods.
            if (worldObject.GetGroup().GetId() == "pod")
            {
                PodInfo newPod = new PodInfo(worldObject, gameObject);
            }
        }

        private void OnWorldObjectBeingDestroyed(ref WorldObject worldObject)
        {
            if (worldObject.GetGroup().GetId() == "pod")
            {
                if (PodInfo.podsByWorldId.TryGetValue(worldObject.GetId(), out PodInfo podToDelete))
                {
                    podToDelete.Remove();
                }
            }
        }

        private void OnGameStateLoadingStarted()
        {
            // Reset the PodInfo and PillarInfo static values.
            PodInfo.Reset();
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
    }
}
