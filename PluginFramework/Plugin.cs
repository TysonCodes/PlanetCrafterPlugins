using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using MijuTools;
using SpaceCraft;
using UnityEngine;

namespace PluginFramework
{
    public delegate void Trigger();

    [BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
    [BepInProcess("Planet Crafter.exe")]
    public class Framework : BaseUnityPlugin
    {
        #region Events
        public static event Trigger GroupDataLoaded;
        #endregion

        #region PublicAPI
        public static IReadOnlyDictionary<string, GroupData> GroupDataById {get {return groupDataById;}} 
        public static IReadOnlyDictionary<string, TerraformStage> TerraformStageById {get {return terraformStageById;}}
        public static IReadOnlyDictionary<string, GameObject> GameObjectById {get {return gameObjectById;}}
        public static IReadOnlyDictionary<string, Sprite> IconById {get {return iconById;}}

        public static void LoadAssetBundlesFromFolder(string folderPath)
        {
            try
            {
                LoadAssetBundles(folderPath);
            }
            catch (Exception ex)
            {
                bepInExLogger.LogError($"Caught exception '{ex.Message}' trying to load asset bundles.");
            }            
        }

        public static GroupDataItem ItemInfoById (string id)
        {
            if (groupDataById.ContainsKey(id))
            {
                return groupDataById[id] as GroupDataItem;
            }
            return null;
        }

        public static void AddGroupDataToList(GroupData toAdd)
        {
            bool alreadyExists = groupDataById.ContainsKey(toAdd.id);
            if (alreadyExists)
            {
                bepInExLogger.LogWarning($"Skipping duplicate group data with id '{toAdd.id}'");
            }
            else
            {
                bepInExLogger.LogInfo($"Adding {toAdd.id} to group data.");
                gameGroupData.Add(toAdd);
                groupDataById[toAdd.id] = toAdd;
            }
        }

        #endregion

        private static ManualLogSource bepInExLogger;

        private readonly Harmony harmony = new Harmony(PluginInfo.PLUGIN_GUID);

        private static List<GroupData> gameGroupData;
        private static Dictionary<string, GroupData> groupDataById = new Dictionary<string, GroupData>();
        private static Dictionary<string, TerraformStage> terraformStageById = new Dictionary<string, TerraformStage>();
        private static Dictionary<string, GameObject> gameObjectById = new Dictionary<string, GameObject>();
        private static Dictionary<string, Sprite> iconById = new Dictionary<string, Sprite>();

        private static List<AssetBundle> loadedAssetBundles = new List<AssetBundle>();

        public Framework()
        {
            bepInExLogger = Logger;
        }

        private void Awake()
        {            
            harmony.PatchAll(typeof(PluginFramework.Framework));

            Logger.LogInfo($"Plugin {PluginInfo.PLUGIN_GUID} is loaded!");
        }

        private static void LoadAssetBundles(string folderPath)
        {
            string[] assetBundlePaths = Directory.GetFiles(folderPath);
            foreach (string assetBundlePath in assetBundlePaths)
            {
                bepInExLogger.LogInfo($"Loading AssetBundle: '{assetBundlePath}'");
                AssetBundle curAssetBundle = AssetBundle.LoadFromFile(assetBundlePath);
                loadedAssetBundles.Add(curAssetBundle);
                var assetBundleGameObjects = curAssetBundle.LoadAllAssets<GameObject>();
                foreach (var gameObject in assetBundleGameObjects)
                {
                    bepInExLogger.LogInfo($"\tAdding GameObject: '{gameObject.name}'");
                    gameObjectById[gameObject.name] = gameObject;
                }
                var assetBundleSprites = curAssetBundle.LoadAllAssets<Sprite>();
                foreach (var sprite in assetBundleSprites)
                {
                    bepInExLogger.LogInfo($"\tAdding Sprite: '{sprite.name}'");
                    iconById[sprite.name] = sprite;
                }
            }
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(StaticDataHandler), "LoadStaticData")]
        private static bool StaticDataHandler_LoadStaticData_Prefix(ref List<GroupData> ___groupsData)
        {
            // Save reference to group data.
            gameGroupData = ___groupsData;

            IndexExistingGroupData();
            LoadTerraformStages();

            GroupDataLoaded?.Invoke();

            return true;
        }

        private static void IndexExistingGroupData()
        {
            foreach (var groupData in gameGroupData)
            {
                groupDataById[groupData.id] = groupData;
                if (groupData.associatedGameObject)
                {
                    gameObjectById[groupData.associatedGameObject.name] = groupData.associatedGameObject;
                }
                if (groupData.icon)
                {
                    iconById[groupData.icon.name] = groupData.icon;
                }
            }
            bepInExLogger.LogInfo($"Created index of previous group data. Size = {groupDataById.Count}");
        }

        private static void LoadTerraformStages()
        {
            if (Managers.GetManager<TerraformStagesHandler>())
            {
                List<TerraformStage> terraformStages = Managers.GetManager<TerraformStagesHandler>().GetAllTerraGlobalStages();
                foreach (TerraformStage stage in terraformStages)
                {
                    terraformStageById[stage.GetTerraId()] = stage;
                }
            }
        }

        private void OnDestroy()
        {
            foreach (var assetBundle in loadedAssetBundles)
            {
                if (assetBundle != null)
                {
                    assetBundle.Unload(true);
                }
            }
            loadedAssetBundles = null;
            harmony.UnpatchSelf();
        }
    }
}
