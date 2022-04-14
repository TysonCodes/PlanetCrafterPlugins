using System.Collections.Generic;
using System.IO;
using System.Reflection;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using SpaceCraft;
using UnityEngine;
using PluginFramework;

namespace Teleporters_Plugin
{
    [System.Serializable]
    public class RecipeList
    {
        public List<string> ingredientNames;
    }

    [BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
    [BepInProcess("Planet Crafter.exe")]
    [BepInDependency(PluginFramework.PluginInfo.PLUGIN_GUID, PluginFramework.PluginInfo.PLUGIN_VERSION)]    // In BepInEx 5.4.x this ia a minimum version, BepInEx 6.x has range semantics.
    public class Plugin : BaseUnityPlugin
    {
        private const string ASSET_BUNDLE_FOLDER = "AssetBundles";
        private const string ASSET_BUNDLE_NAME = "teleporters";
        private const string TELEPORTER_PREFAB_NAME = "TeleporterPrefab";
        private const string TELEPORTER_ICON_NAME = "TeleporterIcon";
        private const string TELEPORTER_UI_NAME = "TeleporterUI";
        private static string TELEPORTER_BUILDING_ID = "Portal";

        private static ManualLogSource bepInExLogger;
        private static ConfigEntry<float> configTeleporterPowerUsage;
        private static ConfigEntry<string> configListOfIngredientForTeleporter;
        private static List<GroupDataItem> teleportRecipeIngredients = new List<GroupDataItem>();
        private static GameObject teleportUIGO;
        private readonly Harmony harmony = new Harmony(PluginInfo.PLUGIN_GUID);

        private void Awake()
        {
            bepInExLogger = Logger;

            configTeleporterPowerUsage = Config.Bind("Teleporter_Parameters", "Teleporter_Power_Usage", 300.0f, "How much power the teleporter should use.");
            configListOfIngredientForTeleporter = Config.Bind("Teleporter_Parameters", "List_Of_Ingredient_For_Teleporter", 
                "{\"ingredientNames\" : [\"Rod-iridium\", \"Rod-iridium\", \"Rod-iridium\", \"Rod-uranium\", \"Rod-uranium\", \"Rod-uranium\", \"RedPowder1\", \"PulsarQuartz\", \"Alloy\"]}",
                "List of ingredients to build a teleporter. Specify as JSON object (see default).");

            string assetBundleFolderPath = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), ASSET_BUNDLE_FOLDER);
            Framework.LoadAssetBundleFromFile(Path.Combine(assetBundleFolderPath, ASSET_BUNDLE_NAME));

            teleportUIGO = Framework.GameObjectByName[TELEPORTER_UI_NAME];

            // Manually patch WindowsHandler as it doesn't seem to work automatically.
            var original = HarmonyLib.AccessTools.Method(typeof(WindowsHandler), "Start");
            var prefix = HarmonyLib.AccessTools.Method(typeof(Teleporters_Plugin.Plugin), "WindowsHandler_Start_Prefix");
            var result = harmony.Patch(original, prefix: new HarmonyMethod(prefix));

            harmony.PatchAll(typeof(Teleporters_Plugin.Plugin));

            Framework.StaticGroupDataIndexed += OnStaticGroupDataIndexed;

            Logger.LogInfo($"Plugin {PluginInfo.PLUGIN_GUID} is loaded!");
        }

        private static bool WindowsHandler_Start_Prefix(ref WindowsHandler __instance)
        {
            Transform windowsHandlerTransform = __instance.gameObject.transform;
            var teleportUi = UnityEngine.Object.Instantiate<GameObject>(teleportUIGO, windowsHandlerTransform);
            teleportUi.SetActive(false);
            return true;
        }
        
        private void OnStaticGroupDataIndexed()
        {
            // Add new constructible for teleporter.
            GroupDataConstructible teleporter = Framework.CreateBuilding(TELEPORTER_BUILDING_ID);
            teleporter.associatedGameObject = Framework.GameObjectByName[TELEPORTER_PREFAB_NAME];
            teleporter.icon = Framework.IconByName[TELEPORTER_ICON_NAME];
            teleporter.unitGenerationEnergy = -1.0f * configTeleporterPowerUsage.Value;
            teleporter.recipeIngredients = GetTeleportRecipeIngredientsList();
            teleporter.unlockingWorldUnit = DataConfig.WorldUnitType.Terraformation;
            teleporter.unlockingValue = 1e9f;
            teleporter.groupCategory = DataConfig.GroupCategory.BaseBuilding;

            Framework.AddGroupDataToList(teleporter);
        }

        private List<GroupDataItem> GetTeleportRecipeIngredientsList()
        {
            RecipeList teleporterRecipe = JsonUtility.FromJson<RecipeList>(configListOfIngredientForTeleporter.Value);
            return Framework.GroupDataItemListFromIds(teleporterRecipe.ingredientNames);
        }

        private void OnDestroy()
        {
            harmony.UnpatchSelf();
        }
    }
    
}
