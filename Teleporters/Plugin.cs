using System.Collections.Generic;
using System.IO;
using System.Reflection;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using MijuTools;
using SpaceCraft;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Teleporters_Plugin
{
    [System.Serializable]
    public class RecipeList
    {
        public List<string> ingredientNames;
    }

    [BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
    [BepInProcess("Planet Crafter.exe")]
    public class Plugin : BaseUnityPlugin
    {
        private static ManualLogSource bepInExLogger;
        private static ConfigEntry<string> configListOfIngredientForTeleporter;
        private AssetBundle teleporterAssetBundle;
        private List<GameObject> assetBundleGameObjects = new List<GameObject>();
        private static List<GroupDataConstructible> assetBundleGroupDataConstructibles = new List<GroupDataConstructible>();
        private static Dictionary<string, GroupData> groupDataById = new Dictionary<string, GroupData>();
        private static List<GroupDataItem> teleportRecipeIngredients = new List<GroupDataItem>();
        private static GameObject teleportUIGO;
        private static string TELEPORT_ID = "Portal";
        private readonly Harmony harmony = new Harmony(PluginInfo.PLUGIN_GUID);

        private void Awake()
        {
            bepInExLogger = Logger;

            configListOfIngredientForTeleporter = Config.Bind("Teleporter_Parameters", "List_Of_Ingredient_For_Teleporter", 
                "{\"ingredientNames\" : [\"Rod-iridium\", \"Rod-iridium\", \"Rod-iridium\", \"Rod-uranium\", \"Rod-uranium\", \"Rod-uranium\", \"RedPowder1\", \"PulsarQuartz\", \"Alloy\"]}",
                "List of ingredients to build a teleporter. Specify as JSON object (see default).");
            
            teleporterAssetBundle = AssetBundle.LoadFromFile(Path.Combine(Paths.PluginPath, "teleporters"));
            assetBundleGameObjects = new List<GameObject>(teleporterAssetBundle.LoadAllAssets<GameObject>());
            assetBundleGroupDataConstructibles = new List<GroupDataConstructible>(teleporterAssetBundle.LoadAllAssets<GroupDataConstructible>());

            teleportUIGO = assetBundleGameObjects.Find((GameObject go) => go.name == "TeleportUI");

            // Manually patch WindowsHandler as it doesn't seem to work automatically.
            var original = HarmonyLib.AccessTools.Method(typeof(WindowsHandler), "Start");
            var prefix = HarmonyLib.AccessTools.Method(typeof(Teleporters_Plugin.Plugin), "WindowsHandler_Start_Prefix");
            var result = harmony.Patch(original, prefix: new HarmonyMethod(prefix));

            harmony.PatchAll(typeof(Teleporters_Plugin.Plugin));

            Logger.LogInfo($"Plugin {PluginInfo.PLUGIN_GUID} is loaded!");
        }

        private static bool WindowsHandler_Start_Prefix(ref WindowsHandler __instance)
        {
            Transform windowsHandlerTransform = __instance.gameObject.transform;
            var teleportUi = UnityEngine.Object.Instantiate<GameObject>(teleportUIGO, windowsHandlerTransform);
            teleportUi.SetActive(false);
            return true;
        }
                
        [HarmonyPrefix]
        [HarmonyPatch(typeof(StaticDataHandler), "LoadStaticData")]
        private static bool StaticDataHandler_LoadStaticData_Prefix(ref List<GroupData> ___groupsData)  
        {
            GenerateTeleportRecipeIngredientsList(___groupsData);
            
            // Index all of the existing group data
            foreach (var groupData in ___groupsData)
            {
                groupDataById[groupData.id] = groupData;
            }
            bepInExLogger.LogInfo($"Created index of previous group data. Size = {groupDataById.Count}");

            foreach(var constructible in assetBundleGroupDataConstructibles)
            {
                if (constructible.id == TELEPORT_ID)
                {
                    constructible.recipeIngredients = teleportRecipeIngredients;
                }
                AddGroupDataToList(ref ___groupsData, constructible);
            }

            return true;
        }

        private static void GenerateTeleportRecipeIngredientsList(List<GroupData> groupsData)
        {
            RecipeList teleporterRecipe = JsonUtility.FromJson<RecipeList>(configListOfIngredientForTeleporter.Value);

            foreach (string ingredient in teleporterRecipe.ingredientNames)
            {
                teleportRecipeIngredients.Add(GetGroupDataItem(groupsData, ingredient));
            }
        }

        private static GroupDataItem GetGroupDataItem(List<GroupData> groupsData, string id)
        {
            return groupsData.Find((GroupData gData) => gData.id == id) as GroupDataItem;
        }

        private static void AddGroupDataToList(ref List<GroupData> groupsData, GroupData toAdd)
        {
                bool alreadyExists = groupDataById.ContainsKey(toAdd.id);
                if (!alreadyExists)
                {
                    bepInExLogger.LogInfo($"Adding {toAdd.id} to group data.");
                    groupsData.Add(toAdd);
                    groupDataById[toAdd.id] = toAdd;
                }            
        }

        private void OnDestroy()
        {
            assetBundleGroupDataConstructibles = null;
            assetBundleGameObjects = null;
            if (teleporterAssetBundle != null)
            {
                teleporterAssetBundle.Unload(true);
            }            
            teleporterAssetBundle = null;
            harmony.UnpatchSelf();
        }
    }
    
}
