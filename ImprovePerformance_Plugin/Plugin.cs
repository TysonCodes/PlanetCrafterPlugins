using System;
using System.Collections.Generic;
using BepInEx;
using BepInEx.Configuration;
using UnityEngine;
using PluginFramework;

namespace ImprovePerformance_Plugin
{
    [System.Serializable]
    public class BuildingList
    {
        public List<string> buildingGameObjectNames;
    }

    [BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
    [BepInProcess("Planet Crafter.exe")]
    [BepInDependency(PluginFramework.PluginInfo.PLUGIN_GUID, PluginFramework.PluginInfo.PLUGIN_VERSION)]    // In BepInEx 5.4.x this ia a minimum version, BepInEx 6.x has range semantics.
    public class Plugin : BaseUnityPlugin
    {
        private ConfigEntry<string> configListOfBuildingsToDisableLightsOn;
        private ConfigEntry<string> configListOfBuildingsToDisableParticleSystemsOn;

        private void Awake()
        {
            configListOfBuildingsToDisableLightsOn = Config.Bind("Remove_Components", "List_Of_Buildings_To_Disable_Lights_On",
                "{\"buildingGameObjectNames\" : [\"VegetableGrower1\", \"VegetableGrower2\", \"Heater1\", \"Heater2\", \"Heater3\", \"Heater4\", " +
                "\"EnergyGenerator4\", \"EnergyGenerator5\", \"EnergyGenerator6\", \"CraftStation1\", \"CraftStation2\"]}",
                "List of buildings to disable the lights in. Specify as JSON object (see default).");
            configListOfBuildingsToDisableParticleSystemsOn = Config.Bind("Remove_Components", "List_Of_Buildings_To_Disable_Particle_Systems_On",
                "{\"buildingGameObjectNames\" : [\"AlgaeSpreader1\", \"AlgaeSpreader2\", \"Heater1\", \"Heater2\", \"Heater3\", \"Heater4\", " +
                "\"EnergyGenerator4\", \"EnergyGenerator5\", \"EnergyGenerator6\", \"CraftStation1\", \"CraftStation2\", \"Vegetube1\", \"VegeTube2\", " +
                "\"VegetubeOutside1\", \"Drill0\", \"Drill1\", \"Drill2\", \"Drill3\", \"Beacon1\", \"GasExtractor\", \"Biodome1\", \"Wall_Door\"]}",
                "List of buildings to disable the lights in. Specify as JSON object (see default).");


            Framework.StaticGroupDataIndexed += OnStaticGroupDataIndexed;

            Logger.LogInfo($"Plugin {PluginInfo.PLUGIN_GUID} is loaded!");
        }

        private void OnStaticGroupDataIndexed()
        {
            try
            {
                RemoveLights();
                RemoveParticleSystems();
            }
            catch (Exception ex)
            {
                Logger.LogError($"Caught exception '{ex.Message}' trying to remove lights and particle systems.");
            }
        }

        private void RemoveLights()
        {
            BuildingList buildingsToRemoveLightsFrom = JsonUtility.FromJson<BuildingList>(configListOfBuildingsToDisableLightsOn.Value);
            foreach (string buildingGameObjectName in buildingsToRemoveLightsFrom.buildingGameObjectNames)
            {
                if (!Framework.GameObjectByName.ContainsKey(buildingGameObjectName))
                {
                    Logger.LogError($"Unable to find GameObject '{buildingGameObjectName}'. Maybe there's a typo?");
                    continue;
                }
                GameObject currentGO = Framework.GameObjectByName[buildingGameObjectName];
                Light [] lights = currentGO.GetComponentsInChildren<Light>();
                foreach (Light light in lights)
                {
                    light.gameObject.SetActive(false);
                }
            }
        }

        private void RemoveParticleSystems()
        {
            BuildingList buildingsToRemoveParticleSystemsFrom = JsonUtility.FromJson<BuildingList>(configListOfBuildingsToDisableParticleSystemsOn.Value);
            foreach (string buildingGameObjectName in buildingsToRemoveParticleSystemsFrom.buildingGameObjectNames)
            {
                if (!Framework.GameObjectByName.ContainsKey(buildingGameObjectName))
                {
                    Logger.LogError($"Unable to find GameObject '{buildingGameObjectName}'. Maybe there's a typo?");
                    continue;
                }                
                GameObject currentGO = Framework.GameObjectByName[buildingGameObjectName];
                ParticleSystem[] particleSystems = currentGO.GetComponentsInChildren<ParticleSystem>();
                foreach (ParticleSystem particleSystem in particleSystems)
                {
                    particleSystem.gameObject.SetActive(false);
                }
            }
        }
    }
    
}
