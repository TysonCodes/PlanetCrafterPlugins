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
        public static event Trigger StaticGroupDataIndexed;
        public static event Trigger GameStateLoadingStarted;
        public static event Trigger GameStateLoaded;
        public static event Trigger GameStateSavingStarted;
        public static event Trigger GameStateSaved;
        #endregion

        #region PublicAPI
        public static IReadOnlyDictionary<string, GroupData> GroupDataById {get {return groupDataById;}} 
        public static IReadOnlyDictionary<string, TerraformStage> TerraformStageById {get {return terraformStageById;}}
        public static IReadOnlyDictionary<string, GameObject> GameObjectByName {get {return gameObjectById;}}
        public static IReadOnlyDictionary<string, Sprite> IconByName {get {return iconById;}}

        public static void LoadAssetBundlesFromFolder(string folderPath)
        {
            try
            {
                LoadAssetBundles(folderPath);
            }
            catch (Exception ex)
            {
                bepInExLogger.LogError($"Caught exception '{ex.Message}' trying to load asset bundles from folder: '{folderPath}'.");
            }            
        }

        public static void LoadAssetBundleFromFile(string fullPathToAssetBundle)
        {
            try
            {
                LoadAssetBundle(fullPathToAssetBundle);
            }
            catch (Exception ex)
            {
                bepInExLogger.LogError($"Caught exception '{ex.Message}' trying to load asset bundles: '{fullPathToAssetBundle}'.");
            }
        }

        public static GroupDataItem ItemInfoById (string id)
        {
            groupDataById.TryGetValue(id, out GroupData item);
            return item as GroupDataItem;
        }

        public static GroupDataConstructible ConstructibleInfoById(string id)
        {
            groupDataById.TryGetValue(id, out GroupData constructible);
            return constructible as GroupDataConstructible;
        }

        public static List<GroupDataItem> GroupDataItemListFromIds(List<string> groupIds)
        {
            List<GroupDataItem> result = new List<GroupDataItem>();

            foreach (string id in groupIds)
            {
                GroupDataItem item = ItemInfoById(id);
                if (item)
                {
                    result.Add(item);
                }
                else
                {
                    bepInExLogger.LogError($"Attempt to use item that doesn't exist: '{id}'");
                }
            }

            return result;
        }

        public static List<GroupData> GroupDataListById(List<string> groupIds)
        {
            List<GroupData> result = new List<GroupData>();

            foreach (string id in groupIds)
            {
                if (groupDataById.ContainsKey(id))
                {
                    result.Add(Framework.GroupDataById[id]);
                }
                else
                {
                    bepInExLogger.LogError($"Attempt to use item that doesn't exist: '{id}'");
                }
            }

            return result;
        }

        public static GroupDataItem CreateItem(string id)
        {
            GroupDataItem newItem = ScriptableObject.CreateInstance<GroupDataItem>();
            newItem.id = id;
            newItem.recipeIngredients = new List<GroupDataItem>();
            newItem.craftableInList = new List<DataConfig.CraftableIn>();
            newItem.associatedGroups = new List<GroupData>();
            return newItem;
        }

        public static GroupDataConstructible CreateConstructible(string id)
        {
            GroupDataConstructible newConstructible = ScriptableObject.CreateInstance<GroupDataConstructible>();
            newConstructible.id = id;
            newConstructible.recipeIngredients = new List<GroupDataItem>();
            return newConstructible;
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
        private static bool staticDataIsLoaded = false;

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
                LoadAssetBundle(assetBundlePath);
            }
        }

        private static void LoadAssetBundle(string assetBundlePath)
        {
            bepInExLogger.LogInfo($"Loading AssetBundle: '{assetBundlePath}'");
            AssetBundle curAssetBundle = AssetBundle.LoadFromFile(assetBundlePath);
            if (curAssetBundle == null)
            {
                bepInExLogger.LogWarning($"Unable to load '{assetBundlePath}'. It may already be loaded.");
                return;
            }
            loadedAssetBundles.Add(curAssetBundle);

            LoadGameObjectsFromAssetBundle(curAssetBundle);
            LoadIconsFromAssetBundle(curAssetBundle);
        }

        private static void LoadGameObjectsFromAssetBundle(AssetBundle curAssetBundle)
        {
            var assetBundleGameObjects = curAssetBundle.LoadAllAssets<GameObject>();
            foreach (var gameObject in assetBundleGameObjects)
            {
                bepInExLogger.LogInfo($"\tAdding GameObject: '{gameObject.name}'");
                gameObjectById[gameObject.name] = gameObject;
            }
        }

        private static void LoadIconsFromAssetBundle(AssetBundle curAssetBundle)
        {
            var assetBundleSprites = curAssetBundle.LoadAllAssets<Sprite>();
            foreach (var sprite in assetBundleSprites)
            {
                bepInExLogger.LogInfo($"\tAdding Sprite: '{sprite.name}'");
                iconById[sprite.name] = sprite;
            }
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(StaticDataHandler), "LoadStaticData")]
        private static bool StaticDataHandler_LoadStaticData_Prefix(ref List<GroupData> ___groupsData)
        {
            LoadTerraformStages();  // Always try to load these as sometimes they don't exist the first time.

            if (staticDataIsLoaded) {return true;}  // Don't do this twice. Static data is static...

            // Save reference to group data.
            gameGroupData = ___groupsData;
            IndexExistingGroupData();
            StaticGroupDataIndexed?.Invoke();

            return true;
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(SavedDataHandler), "LoadSavedData")]
        private static bool SavedDataHandler_LoadSavedData_Prefix()
        {
            GameStateLoadingStarted?.Invoke();
            return true;
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(SavedDataHandler), "LoadSavedData")]
        private static void SavedDataHandler_LoadSavedData_Postfix()
        {
            GameStateLoaded?.Invoke();
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(SavedDataHandler), "SaveWorldData")]
        private static bool SavedDataHandler_SaveWorldData_Prefix()
        {
            GameStateSavingStarted?.Invoke();
            return true;
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(SavedDataHandler), "SaveWorldData")]
        private static void SavedDataHandler_SaveWorldData_Postfix()
        {
            GameStateSaved?.Invoke();
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
