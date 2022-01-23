using System.Collections.Generic;
using System.IO;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using SpaceCraft;
using UnityEngine;
using UnityEngine.UI;

namespace AddCraftableObjects_Plugin
{
    [System.Serializable]
    public class BundlesToLoad
    {
        public List<string> bundleNames;
    }

    [System.Serializable]
    public class ItemsToLoad
    {
        public List<string> itemNames;
    }

    [System.Serializable]
    public class ConstructiblesToLoad
    {
        public List<string> constructibleNames;
    }

    [BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
    [BepInProcess("Planet Crafter.exe")]
    public class Plugin : BaseUnityPlugin
    {
        private ConfigEntry<bool> configLimitLoadedAssets;
        private ConfigEntry<string> configAssetBundleNames;
        private ConfigEntry<string> configListOfItemsToLoad;
        private ConfigEntry<string> configListOfConstructiblesToLoad;
        private static ConfigEntry<bool> configAddWaterBasedVegetube2;
        private static ManualLogSource bepInExLogger;
        private List<AssetBundle> assetBundles = new List<AssetBundle>();
        private List<GameObject> assetBundleGameObjects = new List<GameObject>();
        private static List<GroupDataItem> assetBundleGroupDataItems = new List<GroupDataItem>();
        private static List<GroupDataConstructible> assetBundleGroupDataConstructibles = new List<GroupDataConstructible>();
        private static Dictionary<string, GroupData> groupDataById = new Dictionary<string, GroupData>(); 

        private readonly Harmony harmony = new Harmony(PluginInfo.PLUGIN_GUID);

        private void Awake()
        {
            // Get the configurations
            configLimitLoadedAssets = Config.Bind("General", "Limit_Loaded_Assets", false, 
                "Enables or disables limiting which items and constructibles from asset bundles are added to the game.");
            configAssetBundleNames = Config.Bind("Asset_Bundles", "List_Of_Bundles_To_Load", "{\"bundleNames\" : [\"addcraftableobjects_plugin\"]}",
                "List of asset bundles to load specified as JSON array (see default for example).");
            configListOfItemsToLoad = Config.Bind("Asset_Bundles", "List_Of_Items_To_Load", "{\"itemNames\" : [\"AdvancedBackpack\", \"Coconut\"]}",
                "List of items to add to the game if Limit_Loaded_Assets is true. Specify as JSON object (see default). Must exist in a loaded AssetBundle.");
            configListOfConstructiblesToLoad = Config.Bind("Asset_Bundles", "List_Of_Constructibles_To_Load", "{\"constructibleNames\" : [\"PalmTree\"]}",
                "List of constructibles (buildings via Q menu) to add to the game if Limit_Loaded_Assets is true. Specify as JSON object (see default). " +
                "Must exist in a loaded AssetBundle.");
            configAddWaterBasedVegetube2 = Config.Bind("General", "Add_Water_Based_Vegetube2", true, 
                "Whether or not to add a duplicate vegetube T2 which uses water instead of ice for late-game decoration.");
            bepInExLogger = Logger;

            Logger.LogInfo($"configAssetBundleNames.Value:'{configAssetBundleNames.Value}'");
            Logger.LogInfo($"configAssetBundleNames.Value:'{configListOfItemsToLoad.Value}'");
            Logger.LogInfo($"configAssetBundleNames.Value:'{configListOfConstructiblesToLoad.Value}'");

            BundlesToLoad bundlesToLoad = JsonUtility.FromJson<BundlesToLoad>(configAssetBundleNames.Value);
            ItemsToLoad itemsToLoad = JsonUtility.FromJson<ItemsToLoad>(configListOfItemsToLoad.Value);
            ConstructiblesToLoad constructiblesToLoad = JsonUtility.FromJson<ConstructiblesToLoad>(configListOfConstructiblesToLoad.Value);

            Logger.LogInfo($"bundlesToLoad={bundlesToLoad.ToString()}, itemsToLoad={itemsToLoad.ToString()}, constructiblesToLoad={constructiblesToLoad.ToString()}");

            foreach (var assetBundleName in bundlesToLoad.bundleNames)
            {
                var assetBundle = AssetBundle.LoadFromFile(Path.Combine(Paths.PluginPath, assetBundleName));
                assetBundles.Add(assetBundle);
                LoadCraftablesFromAssetBundleBasedOnConfig(assetBundle, itemsToLoad.itemNames, constructiblesToLoad.constructibleNames);
            }
 
            harmony.PatchAll(typeof(AddCraftableObjects_Plugin.Plugin));

            Logger.LogInfo($"Plugin {PluginInfo.PLUGIN_GUID} is loaded!");
        }

        private void LoadCraftablesFromAssetBundleBasedOnConfig(AssetBundle bundle, List<string> itemNames, List<string> constructibleNames)
        {
            // Load the Sprite and GameObject prefab from the asset bundle.
            assetBundleGameObjects.AddRange(bundle.LoadAllAssets<GameObject>());
            var loadedItems = bundle.LoadAllAssets<GroupDataItem>();
            var loadedConstructibles = bundle.LoadAllAssets<GroupDataConstructible>();

            foreach (var item in loadedItems)
            {
                if (!configLimitLoadedAssets.Value || itemNames.Contains(item.id))
                {
                    assetBundleGroupDataItems.Add(item);
                }
            }
 
            foreach (var constructible in loadedConstructibles)
            {
                if (!configLimitLoadedAssets.Value || constructibleNames.Contains(constructible.id))
                {
                    assetBundleGroupDataConstructibles.Add(constructible);
                }
            }
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(InventoryDisplayer), "TrueRefreshContent")]
        private static bool InventoryDisplayer_TrueRefreshContent_Prefix(Inventory ___inventory, ref GridLayoutGroup ___grid)
        {
            RectTransform parentTransform = ___grid.transform.parent as RectTransform;
            if (___inventory.GetSize() > 32)
            {
                // If the inventory would run off the screen because it is too large then increase width to allow 5 items
                // across and mess with the alignment so the sorting button doesn't overlap.
                // There are nicer ways to do this but more work to get auto-sizing working or add scrolling
                ___grid.childAlignment = TextAnchor.MiddleRight;
                parentTransform.sizeDelta = new Vector2(620, parentTransform.sizeDelta.y);
            }
            else
            {
                // In case we reduce inventory size go back to the previous settings - ugly hard coding...
                ___grid.childAlignment = TextAnchor.MiddleCenter;
                parentTransform.sizeDelta = new Vector2(475, parentTransform.sizeDelta.y);
            }
            return true;
        }
        
        [HarmonyPrefix]
        [HarmonyPatch(typeof(StaticDataHandler), "LoadStaticData")]
        private static bool StaticDataHandler_LoadStaticData_Prefix(ref List<GroupData> ___groupsData)  
        {
            // Index all of the existing group data
            foreach (var groupData in ___groupsData)
            {
                groupDataById[groupData.id] = groupData;
            }
            bepInExLogger.LogInfo($"Created index of previous group data. Size = {groupDataById.Count}");

            // Inject into list of items for processing by StaticDataHandler.LoadStaticData
            foreach (var item in assetBundleGroupDataItems)
            {
                AddGroupDataToList(ref ___groupsData, item);
            }
            foreach(var constructible in assetBundleGroupDataConstructibles)
            {
                AddGroupDataToList(ref ___groupsData, constructible);
            }

            if (configAddWaterBasedVegetube2.Value)
            {
                GroupDataConstructible originalVegetube2 = groupDataById["Vegetube2"] as GroupDataConstructible;
                GroupDataConstructible waterVegetube2 = Instantiate<GroupDataConstructible>(originalVegetube2);
                waterVegetube2.name = originalVegetube2.name;
                waterVegetube2.id = "Vegetube2-Water";
                GroupDataItem waterBottle = groupDataById["WaterBottle1"] as GroupDataItem;
                List<GroupDataItem> newRecipe = new List<GroupDataItem>();
                foreach(var ingredient in waterVegetube2.recipeIngredients)
                {
                    if (ingredient.id == "ice")
                    {
                        newRecipe.Add(waterBottle);
                    }
                    else
                    {
                        newRecipe.Add(ingredient);
                    }
                }
                waterVegetube2.recipeIngredients = newRecipe;
                AddGroupDataToList(ref ___groupsData, waterVegetube2);
            }

            return true;
        }

        private static void AddGroupDataToList(ref List<GroupData> groupsData, GroupData toAdd)
        {
                bepInExLogger.LogInfo($"Adding {toAdd.id} to group data.");
                bool alreadyExists = groupDataById.ContainsKey(toAdd.id);
                groupsData.Add(toAdd);
                groupDataById[toAdd.id] = toAdd;
                if (alreadyExists)
                {
                    bepInExLogger.LogWarning($"Adding duplicate group data with id '{toAdd.id}'");
                }            
        }

        private void OnDestroy()
        {
            assetBundleGroupDataItems = null;
            assetBundleGroupDataConstructibles = null;
            assetBundleGameObjects = null;
            foreach (var assetBundle in assetBundles)
            {
                if (assetBundle != null)
                {
                    assetBundle.Unload(true);
                }
            }
            assetBundles = null;
            harmony.UnpatchSelf();
        }
    }
    
}
