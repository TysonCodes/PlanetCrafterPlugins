using System.Collections.Generic;
using System.IO;
using System.Reflection;
using BepInEx;
using BepInEx.Logging;
using SpaceCraft;
using UnityEngine;
using PluginFramework;
using System;

namespace Vehicle_Plugin
{
    [BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
    [BepInProcess("Planet Crafter.exe")]
    [BepInDependency(PluginFramework.PluginInfo.PLUGIN_GUID, PluginFramework.PluginInfo.PLUGIN_VERSION)]    // In BepInEx 5.4.x this ia a minimum version, BepInEx 6.x has range semantics.
    public class Plugin : BaseUnityPlugin
    {
        private const string ASSET_BUNDLE_FOLDER = "AssetBundles";
        private const string ASSET_BUNDLE_NAME = "spacecraft";
        private const string SPACECRAFT_PREFAB_NAME = "SpaceCraftPrefab";
        private const string SPACECRAFT_ICON_NAME = "SpaceCraftIcon";
        private static string SPACECRAFT_CONSTRUCTIBLE_ID = "SpaceCraft";

        private static ManualLogSource bepInExLogger;

        private void Awake()
        {
            bepInExLogger = Logger;

            // Load vehicle AssetBundle
            string assetBundleFolderPath = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), ASSET_BUNDLE_FOLDER);
            Framework.LoadAssetBundleFromFile(Path.Combine(assetBundleFolderPath, ASSET_BUNDLE_NAME));

            Framework.StaticGroupDataIndexed += OnStaticGroupDataIndexed;

            Logger.LogInfo($"Plugin {PluginInfo.PLUGIN_GUID} is loaded!");
        }

        private void OnStaticGroupDataIndexed()
        {
            // Add new constructible for spacecraft.
            GroupDataConstructible spaceCraft = Framework.CreateConstructible(SPACECRAFT_CONSTRUCTIBLE_ID);
            spaceCraft.associatedGameObject = Framework.GameObjectByName[SPACECRAFT_PREFAB_NAME];
            spaceCraft.icon = Framework.IconByName[SPACECRAFT_ICON_NAME];   
            spaceCraft.recipeIngredients = new List<GroupDataItem>() {
                Framework.ItemInfoById("RocketReactor"),
                Framework.ItemInfoById("RocketReactor"),
                Framework.ItemInfoById("Backpack5"),
                Framework.ItemInfoById("OxygenTank4"),
                Framework.ItemInfoById("Rod-uranium"),
                Framework.ItemInfoById("Bioplastic1"),
                Framework.ItemInfoById("Osmium"),
                Framework.ItemInfoById("Alloy"),
                Framework.ItemInfoById("Alloy")
            };
            spaceCraft.unlockingWorldUnit = DataConfig.WorldUnitType.Terraformation;
            spaceCraft.unlockingValue = 500000f;
            spaceCraft.inventorySize = 40;
            spaceCraft.groupCategory = DataConfig.GroupCategory.Machines;

            Framework.AddGroupDataToList(spaceCraft);
        }
    }
    
}
