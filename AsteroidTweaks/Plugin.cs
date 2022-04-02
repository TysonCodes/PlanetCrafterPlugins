using System.Collections.Generic;
using System.Reflection;
using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using MijuTools;
using SpaceCraft;
using UnityEngine.InputSystem;

namespace AsteroidTweaks_Plugin
{
    [BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
    [BepInProcess("Planet Crafter.exe")]
    public class Plugin : BaseUnityPlugin
    {
        private static ConfigEntry<float> configDebrisDestroyTimeMultiplier;
        private ConfigEntry<Key> configSpawnRandomMeteoEventHotkey;
        private ConfigEntry<bool> configOnlyAsteroidEvents;
        private static MeteoHandler meteoHandlerInstance;
        private static MethodInfo launchRandomMeteoEventMethod;
        private static MethodInfo queueMeteoEventMethod;
        private static List<MeteoEventData> asteroidEvents = null;
        private static WorldUnitsHandler worldUnitsHandler;

        private readonly Harmony harmony = new Harmony(PluginInfo.PLUGIN_GUID);

        private void Awake()
        {
            configDebrisDestroyTimeMultiplier = Config.Bind("General", "Debris_Destroy_Time_Multiplier", 1.0f, 
                "How long debris should last as a multiple of how long it currently lasts. 1.0 is no change. 0.5 would be half as long. 2.0 would be twice as long." + 
                "Note this will also affect how long resource spawns last.");
            configSpawnRandomMeteoEventHotkey = Config.Bind("General", "Spawn_Random_Meteo_Event_Hotkey", Key.F3, 
                "Pick the key to use to spawn a random meteo event if no event is happening.");
            configOnlyAsteroidEvents = Config.Bind("General", "Only_Aseroid_Events", false, "Limit random meteo events to those that have asteroids.");

            harmony.PatchAll(typeof(AsteroidTweaks_Plugin.Plugin));

            Logger.LogInfo($"Plugin {PluginInfo.PLUGIN_GUID} is loaded!");
        }
                
        private void Update()
		{
            if (Keyboard.current[configSpawnRandomMeteoEventHotkey.Value].wasPressedThisFrame)
			{
                if (meteoHandlerInstance != null)
                {
                    if (configOnlyAsteroidEvents.Value && asteroidEvents.Count > 0)
                    {
                        var availableAsteroidEvents = asteroidEvents.FindAll((MeteoEventData eventData) => 
                            worldUnitsHandler.IsWorldValuesAreBetweenStages(eventData.GetMeteoStartTerraformStage(), eventData.GetMeteoStopTerraformStage()));
                        if (availableAsteroidEvents.Count > 0)
                        {
                            var selectedEvent = availableAsteroidEvents[UnityEngine.Random.Range(0, availableAsteroidEvents.Count)];
                            Logger.LogInfo("Launching '" + selectedEvent.name + "' Meteo Event");
                            queueMeteoEventMethod.Invoke(meteoHandlerInstance, new object[] {selectedEvent});
                        }
                    }
                    else
                    {
                        Logger.LogInfo("Launching random Meteo Event");
                        launchRandomMeteoEventMethod.Invoke(meteoHandlerInstance, new object[] {});
                    }
                }
			}
		}

        [HarmonyPostfix]
        [HarmonyPatch(typeof(MeteoHandler), "Start")]
        private static void MeteoHandler_Start_Postfix(MeteoHandler __instance)
        {
            meteoHandlerInstance = __instance;
            launchRandomMeteoEventMethod = HarmonyLib.AccessTools.Method(typeof(MeteoHandler), "LaunchRandomMeteoEvent");
            queueMeteoEventMethod = HarmonyLib.AccessTools.Method(typeof(MeteoHandler), "QueueMeteoEvent");
            asteroidEvents = __instance.meteoEvents.FindAll((MeteoEventData mEvent) => mEvent.asteroidEventData != null);
            worldUnitsHandler = Managers.GetManager<WorldUnitsHandler>();
        } 

        [HarmonyPostfix]
        [HarmonyPatch(typeof(Asteroid), "GetDebrisDestroyTime")]
        private static void Asteroid_GetDebrisDestroyTime_Postfix(ref float __result)
        {
            __result *= configDebrisDestroyTimeMultiplier.Value;
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(AsteroidsImpactHandler), "Start")]
        private static bool AsteroidsImpactHandler_Start_Prefix(ref int ___spawnedResourcesDestroyMultiplier)
        {
            float newMultiplier = (float) ___spawnedResourcesDestroyMultiplier / configDebrisDestroyTimeMultiplier.Value;
            ___spawnedResourcesDestroyMultiplier = (int)(newMultiplier + 0.5f); // Round up.
            return true;
        }

        private void OnDestroy()
        {
            harmony.UnpatchSelf();
        }
    }
    
}
