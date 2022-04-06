using System.Collections.Generic;
using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using SpaceCraft;
using UnityEngine;

namespace Zeolite_Plugin
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
        private static ConfigEntry<bool> configZeoliteCraftingEnabled;
        private ConfigEntry<string> configListOfIngredientForZeolite;
        private static ConfigEntry<bool> configFabricCraftingEnabled;
        private ConfigEntry<string> configListOfIngredientForFabric;

        private static RecipeList zeoliteRecipeList;
        private static RecipeList fabricRecipeList;

        private readonly Harmony harmony = new Harmony(PluginInfo.PLUGIN_GUID);

        private void Awake()
        {
            configZeoliteCraftingEnabled = Config.Bind("Zeolite", "Zeolite_Crafting_Enabled", true, "Enable/disable crafting of Zeolite.");
            configListOfIngredientForZeolite = Config.Bind("Zeolite", "List_Of_Ingredient_For_Zeolite", 
                "{\"ingredientNames\" : [\"Bioplastic1\", \"Fertilizer2\", \"Mutagen1\"]}",
                "List of ingredients to craft Zeolite. Specify as JSON object (see default).");
            configFabricCraftingEnabled = Config.Bind("Fabric", "Fabric_Crafting_Enabled", true, "Enable/disable crafting of Fabric.");
            configListOfIngredientForFabric = Config.Bind("Fabric", "List_Of_Ingredient_For_Fabric", 
                "{\"ingredientNames\" : [\"Bioplastic1\", \"Bioplastic1\", \"Cobalt\"]}",
                "List of ingredients to craft Fabric. Specify as JSON object (see default).");

            zeoliteRecipeList = JsonUtility.FromJson<RecipeList>(configListOfIngredientForZeolite.Value);
            fabricRecipeList = JsonUtility.FromJson<RecipeList>(configListOfIngredientForFabric.Value);

            harmony.PatchAll(typeof(Zeolite_Plugin.Plugin));

            Logger.LogInfo($"Plugin {PluginInfo.PLUGIN_GUID} is loaded!");
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(StaticDataHandler), "LoadStaticData")]
        private static bool StaticDataHandler_LoadStaticData_Prefix(ref List<GroupData> ___groupsData)  
        {
            if (configZeoliteCraftingEnabled.Value)
            {
                GroupDataItem zeolite = ___groupsData.Find((GroupData gData) => gData.id == "Zeolite") as GroupDataItem;
                zeolite.craftableInList.Add(DataConfig.CraftableIn.CraftBioLab);
                zeolite.unlockingWorldUnit = DataConfig.WorldUnitType.Terraformation;
                zeolite.recipeIngredients = GenerateRecipeIngredientsList(___groupsData, zeoliteRecipeList.ingredientNames);
            }

            if (configFabricCraftingEnabled.Value)
            {
                GroupDataItem fabric = ___groupsData.Find((GroupData gData) => gData.id == "FabricBlue") as GroupDataItem;
                fabric.craftableInList.Add(DataConfig.CraftableIn.CraftStationT2);
                fabric.craftableInList.Add(DataConfig.CraftableIn.CraftStationT3);
                fabric.unlockingWorldUnit = DataConfig.WorldUnitType.Terraformation;
                fabric.recipeIngredients = GenerateRecipeIngredientsList(___groupsData, fabricRecipeList.ingredientNames);
            }

            return true;
        }

        private static List<GroupDataItem> GenerateRecipeIngredientsList(List<GroupData> groupsData, List<string> ingredientIds)
        {
            List<GroupDataItem> ingredients = new List<GroupDataItem>();

            foreach (string id in ingredientIds)
            {
                ingredients.Add(groupsData.Find((GroupData gData) => gData.id == id) as GroupDataItem);
            }

            return ingredients;
        }

        private void OnDestroy()
        {
            harmony.UnpatchSelf();
        }
    }
    
}
