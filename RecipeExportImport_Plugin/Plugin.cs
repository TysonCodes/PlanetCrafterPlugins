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
using MijuTools;
using System.Linq;

namespace RecipeExportImport_Plugin
{
    [BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
    [BepInProcess("Planet Crafter.exe")]
    public class Plugin : BaseUnityPlugin
    {
        private delegate void SetGroupDataValue(GroupData groupToEdit, string valueToEdit, JToken newValue);

        private const string EXPORT_FILE_NAME = "CurrentRecipeList.json";
        private const string IMPORT_FILE_NAME = "RecipesToModifyAndAdd.jsonc";
        private static Dictionary<string, SetGroupDataValue> groupDataDelegates;
        private static Dictionary<string, SetGroupDataValue> groupDataItemDelegates;
        private static Dictionary<string, SetGroupDataValue> groupDataConstructibleDelegates;
        private static Dictionary<string, SetGroupDataValue> overrideDelegates;

        private static ManualLogSource bepInExLogger;

        private static ConfigEntry<bool> configExportRecipeList;

        private static List<GroupData> gameGroupData;
        private static Dictionary<string, GroupData> groupDataById = new Dictionary<string, GroupData>(); 
        private static Dictionary<string, TerraformStage> terraformStageById = new Dictionary<string, TerraformStage>();
        private static Dictionary<string, GameObject> gameObjectById = new Dictionary<string, GameObject>();
        private static Dictionary<string, Sprite> iconById = new Dictionary<string, Sprite>();

        private readonly Harmony harmony = new Harmony(PluginInfo.PLUGIN_GUID);

        public Plugin()
        {
            groupDataDelegates = new Dictionary<string, SetGroupDataValue>();
            groupDataItemDelegates = new Dictionary<string, SetGroupDataValue>();
            groupDataConstructibleDelegates = new Dictionary<string, SetGroupDataValue>();
            overrideDelegates = new Dictionary<string, SetGroupDataValue>();

            // GroupData specific
            groupDataDelegates["recipeIngredients"] = (groupToEdit, valueToEidt, newValue) => {groupToEdit.recipeIngredients = GenerateGroupDataItemList(newValue.ToObject<List<string>>()); };
            groupDataDelegates["associatedGameObject"] = (groupToEdit, valueToEidt, newValue) =>
            {
                string associatedGameObjectName = newValue.ToObject<string>();
                if (!gameObjectById.ContainsKey(associatedGameObjectName))
                {
                    bepInExLogger.LogWarning($"Attempt to set 'associatedGameObject' to unknown GameObject '{associatedGameObjectName}'");
                }
                else
                {
                    groupToEdit.associatedGameObject = gameObjectById[associatedGameObjectName];
                }
            };
            groupDataDelegates["icon"] = (groupToEdit, valueToEidt, newValue) =>
            {
                string iconName = newValue.ToObject<string>();
                if (!iconById.ContainsKey(iconName))
                {
                    bepInExLogger.LogWarning($"Attempt to set 'icon' to unknown Sprite '{iconName}'");
                }
                else
                {
                    groupToEdit.icon = iconById[iconName];
                }
            };
            groupDataDelegates["hideInCrafter"] = (groupToEdit, valueToEidt, newValue) => { groupToEdit.hideInCrafter = newValue.ToObject<bool>(); };
            groupDataDelegates["unlockingWorldUnit"] = (groupToEdit, valueToEidt, newValue) => { groupToEdit.unlockingWorldUnit = newValue.ToObject<DataConfig.WorldUnitType>(); };
            groupDataDelegates["unlockingValue"] = (groupToEdit, valueToEidt, newValue) => { groupToEdit.unlockingValue = newValue.ToObject<float>(); };
            groupDataDelegates["terraformStageUnlock"] = (groupToEdit, valueToEidt, newValue) => { 
                string terraformStageId = newValue.ToObject<string>();
                if (!terraformStageById.ContainsKey(terraformStageId))
                {
                    bepInExLogger.LogWarning($"Attempt to set 'terraformStageUnlock' to unknown stage '{terraformStageId}'");
                }
                else
                {
                    groupToEdit.terraformStageUnlock = terraformStageById[terraformStageId];
                }};
            groupDataDelegates["inventorySize"] = (groupToEdit, valueToEidt, newValue) => { groupToEdit.inventorySize = newValue.ToObject<int>(); };

            // GroupDataItem specific
            groupDataItemDelegates["value"] = (groupToEdit, valueToEidt, newValue) => { (groupToEdit as GroupDataItem).value = newValue.ToObject<int>(); };
            groupDataItemDelegates["craftableInList"] = (groupToEdit, valueToEidt, newValue) => { (groupToEdit as GroupDataItem).craftableInList = newValue.ToObject<List<DataConfig.CraftableIn>>(); };
            groupDataItemDelegates["equipableType"] = (groupToEdit, valueToEidt, newValue) => { (groupToEdit as GroupDataItem).equipableType = newValue.ToObject<DataConfig.EquipableType>(); };
            groupDataItemDelegates["usableType"] = (groupToEdit, valueToEidt, newValue) => { (groupToEdit as GroupDataItem).usableType = newValue.ToObject<DataConfig.UsableType>(); };
            groupDataItemDelegates["itemCategory"] = (groupToEdit, valueToEidt, newValue) => { (groupToEdit as GroupDataItem).itemCategory = newValue.ToObject<DataConfig.ItemCategory>(); };
            groupDataItemDelegates["growableGroup"] = (groupToEdit, valueToEidt, newValue) => { (groupToEdit as GroupDataItem).growableGroup = groupDataById[newValue.ToObject<string>()] as GroupDataItem; };
            groupDataItemDelegates["associatedGroups"] = (groupToEdit, valueToEidt, newValue) => { (groupToEdit as GroupDataItem).associatedGroups = GenerateGroupDataList(newValue.ToObject<List<string>>()); };
            groupDataItemDelegates["assignRandomGroupAtSpawn"] = (groupToEdit, valueToEidt, newValue) => { (groupToEdit as GroupDataItem).assignRandomGroupAtSpawn = newValue.ToObject<bool>(); };
            groupDataItemDelegates["replaceByRandomGroupAtSpawn"] = (groupToEdit, valueToEidt, newValue) => { (groupToEdit as GroupDataItem).replaceByRandomGroupAtSpawn = newValue.ToObject<bool>(); };
            groupDataItemDelegates["unitMultiplierOxygen"] = (groupToEdit, valueToEidt, newValue) => { (groupToEdit as GroupDataItem).unitMultiplierOxygen = newValue.ToObject<float>(); };
            groupDataItemDelegates["unitMultiplierPressure"] = (groupToEdit, valueToEidt, newValue) => { (groupToEdit as GroupDataItem).unitMultiplierPressure = newValue.ToObject<float>(); };
            groupDataItemDelegates["unitMultiplierHeat"] = (groupToEdit, valueToEidt, newValue) => { (groupToEdit as GroupDataItem).unitMultiplierHeat = newValue.ToObject<float>(); };
            groupDataItemDelegates["unitMultiplierEnergy"] = (groupToEdit, valueToEidt, newValue) => { (groupToEdit as GroupDataItem).unitMultiplierEnergy = newValue.ToObject<float>(); };
            groupDataItemDelegates["unitMultiplierBiomass"] = (groupToEdit, valueToEidt, newValue) => { (groupToEdit as GroupDataItem).unitMultiplierBiomass = newValue.ToObject<float>(); };

            // GroupDataConstructible specific
            groupDataConstructibleDelegates["unitGenerationOxygen"] = (groupToEdit, valueToEidt, newValue) => { (groupToEdit as GroupDataConstructible).unitGenerationOxygen = newValue.ToObject<float>(); };
            groupDataConstructibleDelegates["unitGenerationPressure"] = (groupToEdit, valueToEidt, newValue) => { (groupToEdit as GroupDataConstructible).unitGenerationPressure = newValue.ToObject<float>(); };
            groupDataConstructibleDelegates["unitGenerationHeat"] = (groupToEdit, valueToEidt, newValue) => { (groupToEdit as GroupDataConstructible).unitGenerationHeat = newValue.ToObject<float>(); };
            groupDataConstructibleDelegates["unitGenerationEnergy"] = (groupToEdit, valueToEidt, newValue) => { (groupToEdit as GroupDataConstructible).unitGenerationEnergy = newValue.ToObject<float>(); };
            groupDataConstructibleDelegates["unitGenerationBiomass"] = (groupToEdit, valueToEidt, newValue) => { (groupToEdit as GroupDataConstructible).unitGenerationBiomass = newValue.ToObject<float>(); };
            groupDataConstructibleDelegates["rotationFixed"] = (groupToEdit, valueToEidt, newValue) => { (groupToEdit as GroupDataConstructible).rotationFixed = newValue.ToObject<bool>(); };
            groupDataConstructibleDelegates["groupCategory"] = (groupToEdit, valueToEidt, newValue) => { (groupToEdit as GroupDataConstructible).groupCategory = newValue.ToObject<DataConfig.GroupCategory>(); };
            groupDataConstructibleDelegates["worlUnitMultiplied"] = (groupToEdit, valueToEidt, newValue) => { (groupToEdit as GroupDataConstructible).worlUnitMultiplied = newValue.ToObject<DataConfig.WorldUnitType>(); };

            // Override (GroupData + GroupDataItem + GroupDataConstructible)
            groupDataDelegates.ToList().ForEach(x => overrideDelegates.Add(x.Key, x.Value));
            groupDataItemDelegates.ToList().ForEach(x => overrideDelegates.Add(x.Key, x.Value));
            groupDataConstructibleDelegates.ToList().ForEach(x => overrideDelegates.Add(x.Key, x.Value));

            // Add GroupData delegates to Items and Constructibles
            groupDataDelegates.ToList().ForEach(x => { groupDataItemDelegates.Add(x.Key, x.Value); groupDataConstructibleDelegates.Add(x.Key, x.Value);});
        }

        private static List<GroupDataItem> GenerateGroupDataItemList(List<string> groupIds)
        {
            List<GroupDataItem> result = new List<GroupDataItem>();

            foreach (string id in groupIds)
            {
                result.Add(groupDataById[id] as GroupDataItem);
            }

            return result;
        }

        private static List<GroupData> GenerateGroupDataList(List<string> groupIds)
        {
            List<GroupData> result = new List<GroupData>();

            foreach (string id in groupIds)
            {
                result.Add(groupDataById[id]);
            }

            return result;
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
            // Save reference to group data.
            gameGroupData = ___groupsData;

            LoadData();

            if (configExportRecipeList.Value)
            {
                bepInExLogger.LogInfo($"Logging group data to {EXPORT_FILE_NAME}");
                ExportGroupDataToFile();
            }

            ApplyChangesToGroupDataFromFile();

            return true;
        }

        private static void LoadData()
        {
            // Index all of the existing group data
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

            // Load Terraform stages
            List<TerraformStage> terraformStages = Managers.GetManager<TerraformStagesHandler>().GetAllTerraGlobalStages();
            foreach (TerraformStage stage in terraformStages)
            {
                terraformStageById[stage.GetTerraId()] = stage;
            }
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
                ApplyModification(modification);

            }
            foreach (var itemToAdd in (JObject) rootObject["ItemsToAdd"])
            {
                AddItem(itemToAdd);
            }
            foreach (var buildingToAdd in (JObject)rootObject["BuildingsToAdd"])
            {
                AddBuilding(buildingToAdd);
            }
        }

        private static void ApplyModification(KeyValuePair<string, JToken> modification)
        {
            if (groupDataById.ContainsKey(modification.Key))
            {
                bepInExLogger.LogInfo($"Modifying '{modification.Key}'");
                foreach (var overriddenValue in (JObject)modification.Value)
                {
                    if (overrideDelegates.ContainsKey(overriddenValue.Key))
                    {
                        bepInExLogger.LogInfo($"\tOverriding '{overriddenValue.Key}'");
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

        private static void AddItem(KeyValuePair<string, JToken> itemToAdd)
        {
            bepInExLogger.LogInfo($"Adding new item '{itemToAdd.Key}'");
            GroupDataItem newItem = ScriptableObject.CreateInstance<GroupDataItem>();
            newItem.id = itemToAdd.Key;
            foreach (var setting in (JObject)itemToAdd.Value)
            {
                if (groupDataItemDelegates.ContainsKey(setting.Key))
                {
                    bepInExLogger.LogInfo($"\tSetting '{setting.Key}'");
                    groupDataItemDelegates[setting.Key].Invoke(newItem, setting.Key, setting.Value);
                }
                else
                {
                    bepInExLogger.LogWarning($"\tUnsupported setting '{setting.Key}'");
                }
            }
            AddGroupDataToList(newItem);
        }

        private static void AddBuilding(KeyValuePair<string, JToken> buildingToAdd)
        {
            bepInExLogger.LogInfo($"Adding new building '{buildingToAdd.Key}'");
            GroupDataConstructible newBuilding = ScriptableObject.CreateInstance<GroupDataConstructible>();
            newBuilding.id = buildingToAdd.Key;
            foreach (var setting in (JObject)buildingToAdd.Value)
            {
                if (groupDataConstructibleDelegates.ContainsKey(setting.Key))
                {
                    bepInExLogger.LogInfo($"\tSetting '{setting.Key}'");
                    groupDataConstructibleDelegates[setting.Key].Invoke(newBuilding, setting.Key, setting.Value);
                }
                else
                {
                    bepInExLogger.LogWarning($"\tUnsupported setting '{setting.Key}'");
                }
            }
            AddGroupDataToList(newBuilding);
        }

        private static void AddGroupDataToList(GroupData toAdd)
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

        private void OnDestroy()
        {
            harmony.UnpatchSelf();
        }
    }
    
}
