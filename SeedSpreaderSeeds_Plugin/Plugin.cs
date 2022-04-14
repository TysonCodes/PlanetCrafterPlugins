using System;
using System.Collections.Generic;
using BepInEx;
using SpaceCraft;
using UnityEngine;
using PluginFramework;

namespace SeedSpreaderSeeds_Plugin
{
    [BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
    [BepInProcess("Planet Crafter.exe")]
    [BepInDependency(PluginFramework.PluginInfo.PLUGIN_GUID, PluginFramework.PluginInfo.PLUGIN_VERSION)]    // In BepInEx 5.4.x this ia a minimum version, BepInEx 6.x has range semantics.
    public class Plugin : BaseUnityPlugin
    {
        private void Awake()
        {
            Framework.GroupDataLoaded += OnGroupDataLoaded;

            Logger.LogInfo($"Plugin {PluginInfo.PLUGIN_GUID} is loaded!");
        }

        private void OnGroupDataLoaded()
        {
            EnablePickingUpGrownItems("SeedSpreader1");
            EnablePickingUpGrownItems("SeedSpreader2");

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

            foreach (var replacement in seedGrowablesToReplaceAndTheirSeeds)
            {
                try
                {
                    MakeGrabbingGrowableReturnSeed(replacement.Key, replacement.Value);
                }
                catch (Exception ex)
                {
                    Logger.LogError($"Caught exception '{ex.Message}' trying to modify '{replacement.Key}' to return '{replacement.Value}'.");
                }
            }
        }

        private static void EnablePickingUpGrownItems(string machineName)
        {
            // Hack the game object so that it doesn't remove colliders from grown seeds.
            if (Framework.GameObjectByName[machineName] && Framework.GameObjectByName[machineName]
                .TryGetComponent<MachineOutsideGrower>(out MachineOutsideGrower grower))
            {
                grower.canRecolt = true;
            }
        }

        private static void MakeGrabbingGrowableReturnSeed(string growableName, string seedName)
        {
            GameObject growableGO = Framework.GameObjectByName[growableName];
            growableGO.AddComponent<WorldUniqueId>();
            WorldObjectFromScene seedFromGrowable = growableGO.AddComponent<WorldObjectFromScene>();
            seedFromGrowable.chanceToAppear = 100.0f;
            seedFromGrowable.randomAppearance = false;
            HarmonyLib.AccessTools.FieldRefAccess<WorldObjectFromScene, GroupData>(seedFromGrowable, "groupData") = 
                Framework.GroupDataById[seedName];
            growableGO.GetComponent<CapsuleCollider>().isTrigger = true;
        }
    }
    
}
