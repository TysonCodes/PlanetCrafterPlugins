using System.Collections.Generic;
using BepInEx;
using BepInEx.Configuration;
using UnityEngine.InputSystem;
using UnityEngine;
using UnityEngine.SceneManagement;
using SpaceCraft;
using HarmonyLib;

namespace ToggleFog_Plugin
{
    [BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
    [BepInProcess("Planet Crafter.exe")]
    public class Plugin : BaseUnityPlugin
    {
        private ConfigEntry<Key> configToggleFogConstraintsModifierKey;
        private ConfigEntry<Key> configToggleFogConstraintsKey;

        private readonly Harmony harmony = new Harmony(PluginInfo.PLUGIN_GUID);

        private static bool showFog = true;
        private Dictionary<string, List<GameObject>> disabledParticleSystemsBySceneName = new Dictionary<string, List<GameObject>>();

        private static Dictionary<string, List<string>> objectsToDisable = new Dictionary<string, List<string>> 
        {
            {"OpenWorldTest", new List<string> {"Particle System", "FloorDustFront"}},
            {"Sector-Bassins", new List<string> {"SulfurFog"}}
        }; 

        private void Awake()
        {
            configToggleFogConstraintsModifierKey = Config.Bind("General", "Toggle_Fog_Modifier_Key", Key.LeftCtrl,
                "Pick the modifier key to use in combination with the key to toggle fog off/on.");
            configToggleFogConstraintsKey = Config.Bind("General", "Toggle_Fog_Key", Key.F,
                "Pick the key to use in combination with the modifier key to toggle fog off/on.");

            SceneManager.sceneLoaded += OnSceneLoaded;
            SceneManager.sceneUnloaded += OnSceneUnloaded;

            harmony.PatchAll(typeof(ToggleFog_Plugin.Plugin));

            Logger.LogInfo($"Plugin {PluginInfo.PLUGIN_GUID} is loaded!");
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(PlayerParticles), "SetGroundDustStatus")]
        private static void PlayerParticles_SetGroundDustStatus_Postfix(ref bool ___spawnDustOnGround)
        {
            if (!showFog)
            {
                ___spawnDustOnGround = false;
            }
        }

        private void Update()
        {
            bool modifierPressed = configToggleFogConstraintsModifierKey.Value == Key.None || Keyboard.current[configToggleFogConstraintsModifierKey.Value].isPressed;
            bool keyPressed = Keyboard.current[configToggleFogConstraintsKey.Value].wasPressedThisFrame;
            if (modifierPressed && keyPressed)
            {
                ToggleFog();
            }
            SetFogObjectsVisibility();
        }

        private void ToggleFog()
        {
            showFog = !showFog;
            RenderSettings.fog = showFog;
            Logger.LogInfo($"Fog toggled to: {showFog}");
        }

        private void SetFogObjectsVisibility()
        {
            foreach (var sceneObjects in disabledParticleSystemsBySceneName)
            {
                foreach (var gameObject in sceneObjects.Value)
                {
                    if (gameObject != null)
                    {
                        gameObject.SetActive(showFog);
                    }
                }
            }
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            string sceneName = scene.name;
            disabledParticleSystemsBySceneName[sceneName] = new List<GameObject>();
            foreach (var gameObject in scene.GetRootGameObjects())
            {
                ParticleSystem[] particleSystems = gameObject.GetComponentsInChildren<ParticleSystem>();
                foreach (ParticleSystem particleSystem in particleSystems)
                {
                    string gameObjectName = particleSystem.gameObject.name;
                    Logger.LogDebug($"Found particle system on GameObject '{gameObjectName}' in Scene '{sceneName}'. Enabled = {particleSystem.gameObject.activeSelf}");
                    if (objectsToDisable.ContainsKey(sceneName) && objectsToDisable[sceneName].Contains(gameObjectName))
                    {
                        particleSystem.gameObject.SetActive(showFog);
                        disabledParticleSystemsBySceneName[sceneName].Add(particleSystem.gameObject);
                        Logger.LogInfo($"Tracking '{gameObjectName}' for disabling.");
                    }
                }
            }
        }

        private void OnSceneUnloaded(Scene scene)
        {
            string sceneName = scene.name;
            foreach (var gameObject in disabledParticleSystemsBySceneName[sceneName])
            {
                if (gameObject != null)
                {
                    gameObject.SetActive(true);
                }
            }
            disabledParticleSystemsBySceneName.Remove(sceneName);
        }

        private void OnDestroy()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.sceneUnloaded -= OnSceneUnloaded;
        }
    }
    
}
