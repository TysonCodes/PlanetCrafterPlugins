using System.Collections.Generic;
using BepInEx;
using HarmonyLib;
using SpaceCraft;

namespace RecyclingVeggies_Plugin
{
    [BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
    [BepInProcess("Planet Crafter.exe")]
    public class Plugin : BaseUnityPlugin
    {
        private readonly Harmony harmony = new Harmony(PluginInfo.PLUGIN_GUID);
        private static Dictionary<string, Group> vegetableSeedGroupByVegetableGroupData;

        private void Awake()
        {
            vegetableSeedGroupByVegetableGroupData = new Dictionary<string, Group>();
           
            harmony.PatchAll(typeof(RecyclingVeggies_Plugin.Plugin));

            Logger.LogInfo($"Plugin {PluginInfo.PLUGIN_GUID} is loaded!");
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(ActionRecycle), "OnAction")]
        private static bool ActionRecycle_OnAction_Prefix(ActionRecycle __instance)
        {
            PopulateVegetableDictionary();
            Inventory recyclerInventory = __instance.GetComponentInParent<InventoryAssociated>().GetInventory();
            if (recyclerInventory.GetInsideWorldObjects().Count > 0)
            {
                WorldObject objectInRecycler = recyclerInventory.GetInsideWorldObjects()[0];
                string objectInRecyclerGroupId = objectInRecycler.GetGroup().id;
                if (vegetableSeedGroupByVegetableGroupData.ContainsKey(objectInRecyclerGroupId))
                {
                    recyclerInventory.RemoveItem(objectInRecycler, true);
                    WorldObject newSeed = WorldObjectsHandler.CreateNewWorldObject(vegetableSeedGroupByVegetableGroupData[objectInRecyclerGroupId]);
                    recyclerInventory.AddItem(newSeed);
                }
            }
            return true;
        }

        private static void PopulateVegetableDictionary()
        {
            if (vegetableSeedGroupByVegetableGroupData.Count == 0)
            {
                vegetableSeedGroupByVegetableGroupData["Vegetable0Growable"] = GroupsHandler.GetGroupViaId("Vegetable0Seed");
                vegetableSeedGroupByVegetableGroupData["Vegetable1Growable"] = GroupsHandler.GetGroupViaId("Vegetable1Seed");
                vegetableSeedGroupByVegetableGroupData["Vegetable2Growable"] = GroupsHandler.GetGroupViaId("Vegetable2Seed");
                vegetableSeedGroupByVegetableGroupData["Vegetable3Growable"] = GroupsHandler.GetGroupViaId("Vegetable3Seed");
            }
        }

        private void OnDestroy()
        {
            harmony.UnpatchSelf();
        }
    }
    
}
