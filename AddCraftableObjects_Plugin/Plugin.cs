using System.Collections.Generic;
using System.IO;
using BepInEx;
using BepInEx.Configuration;
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
        private static GameObject advancedBackpackGameObject;
        private static Sprite advancedBackpackIcon;

        private readonly Harmony harmony = new Harmony(PluginInfo.PLUGIN_GUID);

        private void Awake()
        {
            // Load the Sprite and GameObject prefab from the asset bundle.
            var assetBundle = AssetBundle.LoadFromFile(Path.Combine(Paths.PluginPath, "addcraftableobjects_plugin"));
            advancedBackpackGameObject = assetBundle.LoadAsset<GameObject>("AdvancedBackpackPrefab");
            advancedBackpackIcon = assetBundle.LoadAsset<Sprite>("AdvancedBackpackIcon");

            // Add some required scripts so we can pick it up and track it.
            var worldObjectAssociated = advancedBackpackGameObject.AddComponent<WorldObjectAssociated>();
            var grabbable = advancedBackpackGameObject.AddComponent<ActionGrabable>();
 
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
            // Create new GroupDataItem
            GroupDataItem advancedBackpack = ScriptableObject.CreateInstance(typeof(GroupDataItem)) as GroupDataItem;
            advancedBackpack.id = "AdvancedBackpack";
            advancedBackpack.associatedGameObject = advancedBackpackGameObject;
            advancedBackpack.icon =advancedBackpackIcon;
            advancedBackpack.recipeIngredients = new List<GroupDataItem>()
            {
                GetGroupDataItemById(___groupsData, "Backpack4"),
                GetGroupDataItemById(___groupsData, "Alloy"),
                GetGroupDataItemById(___groupsData, "Alloy"),
                GetGroupDataItemById(___groupsData, "Alloy"),
                GetGroupDataItemById(___groupsData, "Alloy"),
                GetGroupDataItemById(___groupsData, "Alloy"),
            };
            advancedBackpack.unlockingWorldUnit = DataConfig.WorldUnitType.Terraformation;
            advancedBackpack.unlockingValue = 0.0f;
            advancedBackpack.terraformStageUnlock = null;
            advancedBackpack.inventorySize = 0;
            advancedBackpack.value = 28;
            advancedBackpack.craftableInList = new List<DataConfig.CraftableIn>() {DataConfig.CraftableIn.CraftStationT3};
            advancedBackpack.equipableType = DataConfig.EquipableType.BackpackIncrease;
            advancedBackpack.usableType = DataConfig.UsableType.Null;
            advancedBackpack.itemCategory = DataConfig.ItemCategory.Equipment;
            advancedBackpack.growableGroup = null;
            advancedBackpack.associatedGroups = new List<GroupData>();
            advancedBackpack.assignRandomGroupAtSpawn = false;
            advancedBackpack.replaceByRandomGroupAtSpawn = false;
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
