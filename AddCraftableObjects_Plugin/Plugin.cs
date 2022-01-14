using System.Collections.Generic;
using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using SpaceCraft;

namespace AddCraftableObjects_Plugin
{
    [BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
    [BepInProcess("Planet Crafter.exe")]
    public class Plugin : BaseUnityPlugin
    {
        private static ConfigEntry<int> configSpawnRate;

        private readonly Harmony harmony = new Harmony(PluginInfo.PLUGIN_GUID);

        private void Awake()
        {
            // Get configuration values
            configSpawnRate = Config.Bind<int>("Ore_Generator", "spawnRate", 60, "Seconds between creation of each ore item");

            harmony.PatchAll(typeof(AddCraftableObjects_Plugin.Plugin));

            Logger.LogInfo($"Plugin {PluginInfo.PLUGIN_GUID} is loaded!");
        }
        
        [HarmonyPrefix]
        [HarmonyPatch(typeof(StaticDataHandler), "LoadStaticData")]
        private static bool GroupsHandler_SetAllGroups_Prefix(ref List<GroupData> ___groupsData)  
        {
            // Get the existing T4 backpack as a reference for now.
            GroupDataItem t4Backpack = GetGroupDataItemById(___groupsData, "Backpack4");

            // Create new GroupDataItem
            GroupDataItem advancedBackpack = new GroupDataItem();
            advancedBackpack.id = "AdvancedBackpack";
            advancedBackpack.associatedGameObject = t4Backpack.associatedGameObject;    // Use existing GameObject (model, particles, etc.) for now
            advancedBackpack.icon = t4Backpack.icon;                                    // Use existing Sprite for icon for now
            advancedBackpack.recipeIngredients = new List<GroupDataItem>()
            {
                GetGroupDataItemById(___groupsData, "Backpack4"),
                GetGroupDataItemById(___groupsData, "Iron"),
                GetGroupDataItemById(___groupsData, "Titanium")
            };
            advancedBackpack.unlockingWorldUnit = DataConfig.WorldUnitType.Null;
            advancedBackpack.unlockingValue = 0.0f;
            advancedBackpack.terraformStageUnlock = null;
            advancedBackpack.inventorySize = 0;
            advancedBackpack.value = 24;
            advancedBackpack.craftableInList = new List<DataConfig.CraftableIn>() {DataConfig.CraftableIn.CraftStationT3};
            advancedBackpack.equipableType = DataConfig.EquipableType.BackpackIncrease;
            advancedBackpack.usableType = DataConfig.UsableType.Null;
            advancedBackpack.itemCategory = DataConfig.ItemCategory.Equipment;
            advancedBackpack.growableGroup = null;
            advancedBackpack.unitMultiplierOxygen = 0.0f;
            advancedBackpack.unitMultiplierPressure = 0.0f;
            advancedBackpack.unitMultiplierHeat = 0.0f;
            advancedBackpack.unitMultiplierEnergy = 0.0f;
            advancedBackpack.unitMultiplierBiomass = 0.0f;
            
            // Inject into list of items for processing by StaticDataHandler.LoadStaticData
            ___groupsData.Add(advancedBackpack);

            return true;
        }

        private static GroupDataItem GetGroupDataItemById(List<GroupData> groupsData, string id)
        {
            return groupsData.Find((GroupData x) => x.id == id) as GroupDataItem;
        }

        private void OnDestroy()
        {
            harmony.UnpatchSelf();
        }
    }
    
}
