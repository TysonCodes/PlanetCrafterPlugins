using System.Collections.Generic;
using System.Reflection;
using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
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
        private static MeteoHandler meteoHandlerInstance;
        private static MethodInfo launchRandomMeteoEventMethod;
        private static MethodInfo queueMeteoEventMethod;
        private static MeteoEventData fullRedMeteoEvent;

        private readonly Harmony harmony = new Harmony(PluginInfo.PLUGIN_GUID);

        private void Awake()
        {
            configDebrisDestroyTimeMultiplier = Config.Bind("General", "Debris_Destroy_Time_Multiplier", 1.0f, 
                "How long debris should last as a multiple of how long it currently lasts. 1.0 is no change. 0.5 would be half as long. 2.0 would be twice as long." + 
                "Note this will also affect how long resource spawns last.");
            configSpawnRandomMeteoEventHotkey = Config.Bind("General", "Spawn_Random_Meteo_Event_Hotkey", Key.F3, 
                "Pick the key to use to spawn a random meteo event if no event is happening.");

            harmony.PatchAll(typeof(AsteroidTweaks_Plugin.Plugin));

            Logger.LogInfo($"Plugin {PluginInfo.PLUGIN_GUID} is loaded!");
        }
                
        private void Update()
		{
            if (Keyboard.current[configSpawnRandomMeteoEventHotkey.Value].wasPressedThisFrame)
			{
                Logger.LogInfo("Key Pressed");
                if (meteoHandlerInstance != null)
                {
                    Logger.LogInfo("Launching event - " + fullRedMeteoEvent.name);
                    queueMeteoEventMethod.Invoke(meteoHandlerInstance, new object[] {fullRedMeteoEvent});
                    //launchRandomMeteoEventMethod.Invoke(meteoHandlerInstance, new object[] {});
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
            fullRedMeteoEvent = __instance.meteoEvents.Find((MeteoEventData mEvent) => mEvent.name.Contains("Red"));
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
