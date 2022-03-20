using System.Collections.Generic;
using BepInEx;
using HarmonyLib;
using SpaceCraft;
using UnityEngine;

namespace SeedSpreaderSeeds_Plugin
{
    [BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
    [BepInProcess("Planet Crafter.exe")]
    public class Plugin : BaseUnityPlugin
    {
        private readonly Harmony harmony = new Harmony(PluginInfo.PLUGIN_GUID);

        private void Awake()
        {
            harmony.PatchAll(typeof(SeedSpreaderSeeds_Plugin.Plugin));

            Logger.LogInfo($"Plugin {PluginInfo.PLUGIN_GUID} is loaded!");
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(StaticDataHandler), "LoadStaticData")]
        private static bool StaticDataHandler_LoadStaticData_Prefix(ref List<GroupData> ___groupsData)  
        {
            // Hack the seed spreader so that it doesn't remove colliders from grown seeds.
            EnablePickingUpGrownItems(ref ___groupsData, "SeedSpreader1");
            EnablePickingUpGrownItems(ref ___groupsData, "SeedSpreader2");

            Dictionary<string, string> seedGrowablesToReplaceAndTheirSeeds = new Dictionary<string, string>()
            {
                {"Seed0Growable", "Seed0"},
                {"Seed1Growable", "Seed1"},
                {"Seed2Growable", "Seed2"},
                {"Seed3Growable", "Seed3"},
                {"Seed4Growable", "Seed4"},
                {"Seed5Growable", "Seed5"},
                {"Seed6Growable", "Seed6"},
                {"SeedGoldGrowable", "SeedGold"}
            };

            foreach (var growable in seedGrowablesToReplaceAndTheirSeeds.Keys)
            {
                MakeGrabbingGrowableReturnSeed(___groupsData, growable, seedGrowablesToReplaceAndTheirSeeds[growable]);
            }

            return true;
        }

        private static void EnablePickingUpGrownItems(ref List<GroupData> groupsData, string machineName)
        {
            if (GetGameObjectForGroup(groupsData, machineName)
                .TryGetComponent<MachineOutsideGrower>(out MachineOutsideGrower grower))
            {
                grower.canRecolt = true;
            }
        }

        private static void MakeGrabbingGrowableReturnSeed(List<GroupData> groupData, string growableName, string seedName)
        {
            GameObject growableGO = GetGameObjectForGroup(groupData, growableName);
            growableGO.AddComponent<WorldUniqueId>();
            WorldObjectFromScene seedFromGrowable = growableGO.AddComponent<WorldObjectFromScene>();
            seedFromGrowable.chanceToAppear = 100.0f;
            seedFromGrowable.randomAppearance = false;
            HarmonyLib.AccessTools.FieldRefAccess<WorldObjectFromScene, GroupData>(seedFromGrowable, "groupData") = 
                groupData.Find((GroupData gData) => gData.id == seedName);
            growableGO.GetComponent<CapsuleCollider>().isTrigger = true;
        }

        private static GameObject GetGameObjectForGroup(List<GroupData> groups, string id)
        {
            return groups.Find((GroupData gData) => gData.id == id).associatedGameObject;
        }

        private void OnDestroy()
        {
            harmony.UnpatchSelf();
        }
    }
    
}
