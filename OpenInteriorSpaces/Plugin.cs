using System.Collections;
using System.Collections.Generic;
using BepInEx;
using BepInEx.Configuration;
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
        private readonly Harmony harmony = new Harmony(PluginInfo.PLUGIN_GUID);

        public static ManualLogSource bepInExLogger;

        public const DataConfig.BuildPanelSubType WALL_INTERIOR_NONE_SUBTYPE = (DataConfig.BuildPanelSubType) 100;
        public const DataConfig.BuildPanelSubType WALL_INTERIOR_LEFT_SUBTYPE = (DataConfig.BuildPanelSubType) 101;
        public const DataConfig.BuildPanelSubType WALL_INTERIOR_RIGHT_SUBTYPE = (DataConfig.BuildPanelSubType) 102;

        private static bool newPanelsCreated = false;

        private const string GAME_OBJECT_PATH_TO_FLOOR = "Container/4BlocRoom/Common/Floor/P_Floor_Tinny_02_LP";
        private const string GAME_OBJECT_PATH_TO_HALF_WALL = "Container/4BlocRoom/Common/P_Wall_Half_01";

        private GameObject interiorRoomWallPanelGO;
        private GameObject interiorRoomWallPanelLeftGO;
        private GameObject interiorRoomWallPanelRightGO;

        private void Awake()
        {
            bepInExLogger = Logger;

            harmony.PatchAll(typeof(OpenInteriorSpaces_Plugin.Plugin));

            Framework.GameStateLoadingStarted += OnGameStateLoadingStarted;

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
            if (_worldObject.GetGroup().GetId() == "pod")
            {
                if (PodInfo.podsByWorldId.TryGetValue(_worldObject.GetId(), out PodInfo podToDelete))
                {
                    podToDelete.Remove();
                }
            }
            return true;
        }

        private void OnGameStateLoadingStarted()
        {
            if (!newPanelsCreated)
            {
                CreatePanels();
                AddPanelsToPanelsResources();
                newPanelsCreated = true;
            }
        }

        private void CreatePanels()
        {
            GameObject largeRoomGO = Framework.GameObjectByName["Pod4x"];
            GameObject commonFloorPanelGO = Instantiate(largeRoomGO.transform.Find(GAME_OBJECT_PATH_TO_FLOOR).gameObject, null);
            commonFloorPanelGO.transform.localPosition = Vector3.zero;
            commonFloorPanelGO.transform.localEulerAngles = Vector3.zero;
            commonFloorPanelGO.transform.localScale = Vector3.one;
            GameObject commonHalfWallGO = Instantiate(largeRoomGO.transform.Find(GAME_OBJECT_PATH_TO_HALF_WALL).gameObject, null);
            commonHalfWallGO.transform.localPosition = Vector3.zero;
            commonHalfWallGO.transform.localEulerAngles = Vector3.zero;
            commonHalfWallGO.transform.localScale = Vector3.one;
            commonHalfWallGO.name = "HalfWall";

            // Interior wall panel without left/right walls
            interiorRoomWallPanelGO = new GameObject("Wall_Interior_None");

            // Floor
            GameObject floorGO = new GameObject("Floor");
            floorGO.transform.SetParent(interiorRoomWallPanelGO.transform, false);
            floorGO.transform.localPosition = new Vector3(-1.0f, 0.0f, 1.0f);
            floorGO.transform.localEulerAngles = new Vector3(0.0f, 90.0f, 0.0f);
            floorGO.transform.localScale = new Vector3(4.0f / 3.0f, 1.0f, 0.5f);
            Instantiate(commonFloorPanelGO, floorGO.transform).name = commonFloorPanelGO.name;

            // Ceiling
            GameObject ceilingGO = Instantiate(floorGO, interiorRoomWallPanelGO.transform);
            ceilingGO.name = "Ceiling";
            ceilingGO.transform.localPosition = new Vector3(0.0f, 4.0f, 1.0f);
            ceilingGO.transform.localEulerAngles = new Vector3(180.0f, 90.0f, 0.0f);

            // Interior wall panel with left wall
            interiorRoomWallPanelLeftGO = Instantiate(interiorRoomWallPanelGO);
            interiorRoomWallPanelLeftGO.name = "Wall_Interior_Left";

            // LeftWall
            GameObject leftWallGO = new GameObject("LeftWall");
            leftWallGO.transform.SetParent(interiorRoomWallPanelLeftGO.transform, false);
            leftWallGO.transform.localPosition = new Vector3(-1.0f, 0.0f, -6.0f);
            leftWallGO.transform.localEulerAngles = new Vector3(0.0f, -90.0f, 0.0f);
            leftWallGO.transform.localScale = new Vector3(1.0f, 1.0f, 1.0f / 3.0f);
            Instantiate(commonHalfWallGO, leftWallGO.transform).name = commonHalfWallGO.name;

            // Interior wall panel with right wall
            interiorRoomWallPanelRightGO = Instantiate(interiorRoomWallPanelGO);
            interiorRoomWallPanelRightGO.name = "Wall_Interior_Right";

            // RightWall
            GameObject rightWallGO = new GameObject("RightWall");
            rightWallGO.transform.SetParent(interiorRoomWallPanelRightGO.transform, false);
            rightWallGO.transform.localPosition = Vector3.zero;
            rightWallGO.transform.localEulerAngles = new Vector3(0.0f, 90.0f, 0.0f);
            rightWallGO.transform.localScale = new Vector3(1.0f, 1.0f, 1.0f / 3.0f);
            Instantiate(commonHalfWallGO, rightWallGO.transform).name = commonHalfWallGO.name;
        }

        private void AddPanelsToPanelsResources()
        {
            PanelsResources panelMgr = Managers.GetManager<PanelsResources>();
            
            panelMgr.panelsSubtypes.Add(WALL_INTERIOR_NONE_SUBTYPE);
            panelMgr.panelsGameObjects.Add(interiorRoomWallPanelGO);
            panelMgr.panelsGroupItems.Add(null);

            panelMgr.panelsSubtypes.Add(WALL_INTERIOR_LEFT_SUBTYPE);
            panelMgr.panelsGameObjects.Add(interiorRoomWallPanelLeftGO);
            panelMgr.panelsGroupItems.Add(null);

            panelMgr.panelsSubtypes.Add(WALL_INTERIOR_RIGHT_SUBTYPE);
            panelMgr.panelsGameObjects.Add(interiorRoomWallPanelRightGO);
            panelMgr.panelsGroupItems.Add(null);
        }

        private void OnDestroy()
        {
            harmony.UnpatchSelf();
        }
    }
    
}
