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
    [BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
    [BepInProcess("Planet Crafter.exe")]
    public class Plugin : BaseUnityPlugin
    {
        private ConfigEntry<string> configAssetBundleName;
        private static ConfigEntry<bool> addWaterBasedVegetube2;
        private static ManualLogSource bepInExLogger;
        private AssetBundle assetBundle;
        private GameObject[] assetBundleGameObjects;
        private static GroupDataItem[] assetBundleGroupDataItems;
        private static GroupDataConstructible[] assetBundleGroupDataConstructibles;
        private static Dictionary<string, GroupData> groupDataById = new Dictionary<string, GroupData>(); 

        private readonly Harmony harmony = new Harmony(PluginInfo.PLUGIN_GUID);

        private void Awake()
        {
            // Get the configurations
            configAssetBundleName = Config.Bind("General", "Asset_Bundle_Name", "addcraftableobjects_plugin", "The name of the file of the AssetBundle to load.");
            addWaterBasedVegetube2 = Config.Bind("General", "Add_Water_Based_Vegetube2", true, 
                "Whether or not to add a duplicate vegetube T2 which uses water instead of ice for late-game decoration.");
            bepInExLogger = Logger;

            // Load the Sprite and GameObject prefab from the asset bundle.
            assetBundle = AssetBundle.LoadFromFile(Path.Combine(Paths.PluginPath, configAssetBundleName.Value));
            assetBundleGameObjects = assetBundle.LoadAllAssets<GameObject>();
            assetBundleGroupDataItems = assetBundle.LoadAllAssets<GroupDataItem>();
            assetBundleGroupDataConstructibles = assetBundle.LoadAllAssets<GroupDataConstructible>();
 
            harmony.PatchAll(typeof(AddCraftableObjects_Plugin.Plugin));

            Logger.LogInfo($"Plugin {PluginInfo.PLUGIN_GUID} is loaded!");
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

            if (addWaterBasedVegetube2.Value)
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
            if (assetBundle != null)
            {
                assetBundle.Unload(true);
                assetBundle = null;
            }
            harmony.UnpatchSelf();
        }
    }
    
}
