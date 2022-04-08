using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using SpaceCraft;
using UnityEngine;
using UnityEngine.UI;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace RecipeExportImport_Plugin
{
    [BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
    [BepInProcess("Planet Crafter.exe")]
    public class Plugin : BaseUnityPlugin
    {
        private delegate void ApplyOverride(GroupData groupToEdit, string valueToEdit, JToken newValue);

        private const string EXPORT_FILE_NAME = "CurrentRecipeList.json";
        private const string IMPORT_FILE_NAME = "RecipesToModifyAndAdd.jsonc";
        private static Dictionary<string, ApplyOverride> overrideDelegates;

        private static ManualLogSource bepInExLogger;

        private static ConfigEntry<bool> configExportRecipeList;

        private static Dictionary<string, GroupData> groupDataById = new Dictionary<string, GroupData>(); 

        private readonly Harmony harmony = new Harmony(PluginInfo.PLUGIN_GUID);

        public Plugin()
        {
            overrideDelegates = new Dictionary<string, ApplyOverride>();
            overrideDelegates["recipeIngredients"] = ApplyRecipeOverride;
            overrideDelegates["craftableInList"] = (groupToEdit, valueToEidt, newValue) => 
                {(groupToEdit as GroupDataItem).craftableInList = newValue.ToObject<List<DataConfig.CraftableIn>>();};
            overrideDelegates["unlockingWorldUnit"] = (groupToEdit, valueToEidt, newValue) =>
                {groupToEdit.unlockingWorldUnit = newValue.ToObject<DataConfig.WorldUnitType>(); };
            overrideDelegates[""] = (groupToEdit, valueToEidt, newValue) => {};
        }

        private static void ApplyRecipeOverride(GroupData groupToEdit, string valueToEdit, JToken newValue)
        {
            List<string> newIngredients = newValue.ToObject<List<string>>();
            groupToEdit.recipeIngredients = GenerateRecipeIngredientsList(newIngredients);
        }

        private static List<GroupDataItem> GenerateRecipeIngredientsList(List<string> ingredientIds)
        {
            List<GroupDataItem> ingredients = new List<GroupDataItem>();

            foreach (string id in ingredientIds)
            {
                ingredients.Add(groupDataById[id] as GroupDataItem);
            }

            return ingredients;
        }
        
        private void Awake()
        {
            bepInExLogger = Logger;

            configExportRecipeList = Config.Bind("General", "Export_Recipe_List", false, 
                "Enables or disables exporting the current recipe list on loading of the game. Slows down loading.");

            harmony.PatchAll(typeof(RecipeExportImport_Plugin.Plugin));

            Logger.LogInfo($"Plugin {PluginInfo.PLUGIN_GUID} is loaded!");
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

            if (configExportRecipeList.Value)
            {
                ExportGroupDataToFile();
            }

            ApplyChangesToGroupDataFromFile();

            return true;
        }

        private static void ExportGroupDataToFile()
        {
            string exportFileName = Path.Combine(Paths.PluginPath, EXPORT_FILE_NAME);
            FileStream exportFile = File.Open(exportFileName, FileMode.Create);
            StringBuilder jsonStringBuilder = new StringBuilder("{", 100000);
            foreach (var entry in groupDataById)
            {
                if (entry.Value is GroupDataItem)
                {
                    ExportableGroupDataItem exportableItem = new ExportableGroupDataItem(entry.Value as GroupDataItem);
                    jsonStringBuilder.AppendFormat("\"{0}\" : {1}, ", entry.Key, JsonUtility.ToJson(exportableItem, true));
                }
                else
                {
                    ExportableGroupDataConstructible exportableConstructible = new ExportableGroupDataConstructible(entry.Value as GroupDataConstructible);
                    jsonStringBuilder.AppendFormat("\"{0}\" : {1}, ", entry.Key, JsonUtility.ToJson(exportableConstructible, true));
                }
            }
            jsonStringBuilder.Remove(jsonStringBuilder.Length - 2, 2);
            jsonStringBuilder.Append("}");
            string jsonString = jsonStringBuilder.ToString();
            exportFile.Write(Encoding.UTF8.GetBytes(jsonString), 0, jsonString.Length);
            exportFile.Close();
        }

        private static void ApplyChangesToGroupDataFromFile()
        {
            string importFilename = Path.Combine(Paths.PluginPath, IMPORT_FILE_NAME);
            JObject rootObject = JObject.Parse(File.ReadAllText(importFilename));
            foreach (var modification in (JObject) rootObject["Modifications"])
            {
                if (groupDataById.ContainsKey(modification.Key))
                {
                    bepInExLogger.LogInfo($"Modifying {modification.Key}");
                    foreach (var overriddenValue in (JObject)modification.Value)
                    {
                        if (overrideDelegates.ContainsKey(overriddenValue.Key))
                        {
                            bepInExLogger.LogInfo($"\tOverriding {overriddenValue.Key}");
                            overrideDelegates[overriddenValue.Key].Invoke(groupDataById[modification.Key], overriddenValue.Key, overriddenValue.Value);
                        }
                        else
                        {
                            bepInExLogger.LogWarning($"\tUnsupported override '{overriddenValue.Key}'");
                        }
                    }
                }
                else
                {
                    bepInExLogger.LogWarning($"Can't modify unknown object {modification.Key}");
                }
            }
        }

        private void OnDestroy()
        {
            harmony.UnpatchSelf();
        }
    }
    
}
