using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using SpaceCraft;
using MijuTools;
using UnityEngine;
using Newtonsoft.Json.Linq;
using System.Linq;
using PluginFramework;

namespace RecipeExportImport_Plugin
{
    [BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
    [BepInProcess("Planet Crafter.exe")]
    [BepInDependency(PluginFramework.PluginInfo.PLUGIN_GUID, PluginFramework.PluginInfo.PLUGIN_VERSION)]    // In BepInEx 5.4.x this ia a minimum version, BepInEx 6.x has range semantics.
    public class Plugin : BaseUnityPlugin
    {
        private delegate void SetGroupDataValue(GroupData groupToEdit, string valueToEdit, JToken newValue);

        private const string EXPORT_FILE_NAME = "CurrentRecipeList.json";
        private const string CRAFTABLE_LIST_FILE_NAME = "CraftableList.txt";
        private const string IMPORT_FILE_NAME = "RecipesToModifyAndAdd.jsonc";
        private const string ASSET_BUNDLE_FOLDER = "AssetBundles";

        private Dictionary<string, SetGroupDataValue> groupDataDelegates;
        private Dictionary<string, SetGroupDataValue> groupDataItemDelegates;
        private Dictionary<string, SetGroupDataValue> groupDataConstructibleDelegates;
        private Dictionary<string, SetGroupDataValue> overrideDelegates;

        private static ManualLogSource bepInExLogger;

        private ConfigEntry<bool> configExportRecipeList;
        private string dataFilePath;
        private string exportFilePath;
        private string craftableListFilePath;
        private string importFilePath;

        public Plugin()
        {
            PrintOutDependencies();
            dataFilePath = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "DataFiles");
            exportFilePath = Path.Combine(dataFilePath, EXPORT_FILE_NAME);
            craftableListFilePath = Path.Combine(dataFilePath, CRAFTABLE_LIST_FILE_NAME);
            importFilePath = Path.Combine(dataFilePath, IMPORT_FILE_NAME);

            groupDataDelegates = new Dictionary<string, SetGroupDataValue>();
            groupDataItemDelegates = new Dictionary<string, SetGroupDataValue>();
            groupDataConstructibleDelegates = new Dictionary<string, SetGroupDataValue>();
            overrideDelegates = new Dictionary<string, SetGroupDataValue>();

            // GroupData specific
            groupDataDelegates["recipeIngredients"] = (groupToEdit, valueToEidt, newValue) => {groupToEdit.recipeIngredients = Framework.GroupDataItemListFromIds(newValue.ToObject<List<string>>()); };
            groupDataDelegates["associatedGameObject"] = (groupToEdit, valueToEidt, newValue) =>
            {
                string associatedGameObjectName = newValue.ToObject<string>();
                if (!Framework.GameObjectByName.ContainsKey(associatedGameObjectName))
                {
                    bepInExLogger.LogWarning($"Attempt to set 'associatedGameObject' to unknown GameObject '{associatedGameObjectName}'");
                }
                else
                {
                    groupToEdit.associatedGameObject = Framework.GameObjectByName[associatedGameObjectName];
                }
            };
            groupDataDelegates["icon"] = (groupToEdit, valueToEidt, newValue) =>
            {
                string iconName = newValue.ToObject<string>();
                if (!Framework.IconByName.ContainsKey(iconName))
                {
                    bepInExLogger.LogWarning($"Attempt to set 'icon' to unknown Sprite '{iconName}'");
                }
                else
                {
                    groupToEdit.icon = Framework.IconByName[iconName];
                }
            };
            groupDataDelegates["hideInCrafter"] = (groupToEdit, valueToEidt, newValue) => { groupToEdit.hideInCrafter = newValue.ToObject<bool>(); };
            groupDataDelegates["unlockingWorldUnit"] = (groupToEdit, valueToEidt, newValue) => { groupToEdit.unlockingWorldUnit = newValue.ToObject<DataConfig.WorldUnitType>(); };
            groupDataDelegates["unlockingValue"] = (groupToEdit, valueToEidt, newValue) => { groupToEdit.unlockingValue = newValue.ToObject<float>(); };
            groupDataDelegates["terraformStageUnlock"] = (groupToEdit, valueToEidt, newValue) => { 
                string terraformStageId = newValue.ToObject<string>();
                if (!Framework.TerraformStageById.ContainsKey(terraformStageId))
                {
                    bepInExLogger.LogWarning($"Attempt to set 'terraformStageUnlock' to unknown stage '{terraformStageId}'");
                }
                else
                {
                    groupToEdit.terraformStageUnlock = Framework.TerraformStageById[terraformStageId];
                }};
            groupDataDelegates["inventorySize"] = (groupToEdit, valueToEidt, newValue) => { groupToEdit.inventorySize = newValue.ToObject<int>(); };

            // GroupDataItem specific
            groupDataItemDelegates["value"] = (groupToEdit, valueToEidt, newValue) => { (groupToEdit as GroupDataItem).value = newValue.ToObject<int>(); };
            groupDataItemDelegates["craftableInList"] = (groupToEdit, valueToEidt, newValue) => { (groupToEdit as GroupDataItem).craftableInList = newValue.ToObject<List<DataConfig.CraftableIn>>(); };
            groupDataItemDelegates["equipableType"] = (groupToEdit, valueToEidt, newValue) => { (groupToEdit as GroupDataItem).equipableType = newValue.ToObject<DataConfig.EquipableType>(); };
            groupDataItemDelegates["usableType"] = (groupToEdit, valueToEidt, newValue) => { (groupToEdit as GroupDataItem).usableType = newValue.ToObject<DataConfig.UsableType>(); };
            groupDataItemDelegates["itemCategory"] = (groupToEdit, valueToEidt, newValue) => { (groupToEdit as GroupDataItem).itemCategory = newValue.ToObject<DataConfig.ItemCategory>(); };
            groupDataItemDelegates["growableGroup"] = (groupToEdit, valueToEidt, newValue) => { (groupToEdit as GroupDataItem).growableGroup = Framework.GroupDataById[newValue.ToObject<string>()] as GroupDataItem; };
            groupDataItemDelegates["associatedGroups"] = (groupToEdit, valueToEidt, newValue) => { (groupToEdit as GroupDataItem).associatedGroups = Framework.GroupDataListById(newValue.ToObject<List<string>>()); };
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

        private void PrintOutDependencies()
        {
            Assembly executingAsm = Assembly.GetExecutingAssembly();
            var loadedAssemblies = AppDomain.CurrentDomain.GetAssemblies();
            Logger.LogDebug("Executing Assembly: " + executingAsm.GetName());
            Logger.LogDebug("References:");
            foreach (var asm in executingAsm.GetReferencedAssemblies())
            {
                Logger.LogDebug("\t" + asm.Name);
                Assembly actualAsm = loadedAssemblies.FirstOrDefault(assembly => assembly.GetName().Name == asm.Name);
                if (actualAsm != null)
                {
                    foreach (var subAsm in actualAsm.GetReferencedAssemblies())
                    {
                        Logger.LogDebug("\t\t" + subAsm.Name);
                    }
                }
            }
        }

        private void Awake()
        {
            bepInExLogger = Logger;

            configExportRecipeList = Config.Bind("General", "Export_Recipe_List", true, 
                "Enables or disables exporting the current recipe list on loading of the game. Slows down loading.");

            Framework.StaticGroupDataIndexed += OnStaticGroupDataIndexed;

            Logger.LogInfo($"Plugin {PluginInfo.PLUGIN_GUID} is loaded!");
        }

        private void OnStaticGroupDataIndexed()
        {
            if (configExportRecipeList.Value)
            {
                bepInExLogger.LogInfo($"Logging group data to {exportFilePath}");
                try
                {
                    ExportGroupDataToFile();
                }
                catch (Exception ex)
                {
                    bepInExLogger.LogError($"Caught exception '{ex.Message}' trying to export data.");
                }
            }

            bepInExLogger.LogInfo($"Logging craftables list to {craftableListFilePath}");
            try
            {
                WriteCraftableListToFile();
            }
            catch (Exception ex)
            {
                bepInExLogger.LogError($"Caught exception '{ex.Message}' trying to output craftable list.");
            }

            string assetBundleFolderPath = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), ASSET_BUNDLE_FOLDER);
            Framework.LoadAssetBundlesFromFolder(assetBundleFolderPath);

            try
            {
                ApplyChangesToGroupDataFromFile();
            }
            catch (Exception ex)
            {
                bepInExLogger.LogError($"Caught exception '{ex.Message}' trying to apply modifications/additions.");
            }
        }

        private void ExportGroupDataToFile()
        {
            FileStream exportFile = File.Open(exportFilePath, FileMode.Create);
            StringBuilder jsonStringBuilder = new StringBuilder("{", 100000);
            foreach (var entry in Framework.GroupDataById)
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

        private void WriteCraftableListToFile()
        {
            FileStream craftableListFile = File.Open(craftableListFilePath, FileMode.Create);
            StringBuilder craftableListStringBuilder = new StringBuilder("", 20000);
            foreach (var entry in Framework.GroupDataById)
            {
                craftableListStringBuilder.AppendFormat("\"{0}\" : {1}\n", GetNameForGroup(entry.Key), entry.Key);
            }
            string craftableListString = craftableListStringBuilder.ToString();
            craftableListFile.Write(Encoding.UTF8.GetBytes(craftableListString), 0, craftableListString.Length);
            craftableListFile.Close();
        }

        private string GetNameForGroup(string groupName)
        {
            string localizedString = Localization.GetLocalizedString(GameConfig.localizationGroupNameId + groupName);
            if (!(localizedString == ""))
            {
                return localizedString;
            }
            return "Not localized";
        }

        private void ApplyChangesToGroupDataFromFile()
        {
            JObject rootObject = JObject.Parse(File.ReadAllText(importFilePath));
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

        private void ApplyModification(KeyValuePair<string, JToken> modification)
        {
            if (Framework.GroupDataById.ContainsKey(modification.Key))
            {
                bepInExLogger.LogInfo($"Modifying '{modification.Key}'");
                foreach (var overriddenValue in (JObject)modification.Value)
                {
                    if (overrideDelegates.ContainsKey(overriddenValue.Key))
                    {
                        bepInExLogger.LogInfo($"\tOverriding '{overriddenValue.Key}'");
                        overrideDelegates[overriddenValue.Key].Invoke(Framework.GroupDataById[modification.Key], overriddenValue.Key, overriddenValue.Value);
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

        private void AddItem(KeyValuePair<string, JToken> itemToAdd)
        {
            bepInExLogger.LogInfo($"Adding new item '{itemToAdd.Key}'");
            GroupDataItem newItem = Framework.CreateItem(itemToAdd.Key);
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
            Framework.AddGroupDataToList(newItem);
        }

        private void AddBuilding(KeyValuePair<string, JToken> buildingToAdd)
        {
            bepInExLogger.LogInfo($"Adding new building '{buildingToAdd.Key}'");
            GroupDataConstructible newBuilding = Framework.CreateConstructible(buildingToAdd.Key);
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
            Framework.AddGroupDataToList(newBuilding);
        }
    }
    
}
