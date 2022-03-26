using HarmonyLib;
using MijuTools;
using UnityEngine;

namespace SpaceCraft
{
    public class ActionEnterVehicle : Actionnable
    {
        // Settings
        public float colliderRadius = 2.5f;

        private static PlayerMainController activePlayerController;
        private static PlayerMultitool playerMultitool;
        private static PlayerLookable playerLookable;
        private static PlayerCameraShake playerCameraShake;

        // Components
        private GameObject rootObject;
        private Collider vehiclePlayerExclusionCollider;
        private Collider openInventoryCollider;
        private Collider exitVehicleCollider;
        private Transform vehicleLocationTransform;
        private Transform playerHeadLocationTransform;
        private Transform playerExitLocationTransform;
        private WorldObject worldObject;

        // Other game objects
        private GameObject playerArmor;
        private AudioSource playerFootsteps;

        // Saved parameters to undo
        private bool inVehicle = false;
        private Transform previousParentTransform;
        private float previousPlayerCameraHeight = 0.0f;
        private Vector3 previousPlayerColliderCenter = Vector3.zero;
        private float previousPlayerColliderHeight = 0.0f;
        private float previousPlayerColliderRadius = 0.0f;
        private float previousFootstepsVolume = 0.0f;

        private new void Start()
		{
            if (activePlayerController == null || playerMultitool == null || playerLookable == null || playerCameraShake == null)
            {
                activePlayerController = Managers.GetManager<PlayersManager>().GetActivePlayerController();
                playerMultitool = activePlayerController.GetMultitool();
                playerLookable = activePlayerController.GetPlayerLookable();
                playerCameraShake = activePlayerController.GetPlayerCameraShake();
            }

            // Get various components on the vehicle.
            rootObject = gameObject.transform.parent.gameObject;
            vehiclePlayerExclusionCollider = rootObject.GetComponent<Collider>();
            openInventoryCollider = rootObject.transform.Find("TriggerOpenInventory").GetComponent<Collider>();
            GameObject triggerExitGO = rootObject.transform.Find("TriggerExit").gameObject;
            exitVehicleCollider = triggerExitGO.GetComponent<Collider>();
            triggerExitGO.AddComponent<ActionExitVehicle>().enterVehicleAction = this;
            vehicleLocationTransform = rootObject.transform;
            playerHeadLocationTransform = rootObject.transform.Find("PlayerHeadLocation");
            playerExitLocationTransform = rootObject.transform.Find("PlayerExitLocation");
            worldObject = rootObject.GetComponent<WorldObjectAssociated>().GetWorldObject();

            // Get other components
            GameObject playerGO = GameObject.Find("Player");
            playerArmor = playerGO.GetComponentInChildren<PlayerTEMPFIXANIM>().playerArmor;            
            playerFootsteps = playerGO.transform.Find("Audio/Footsteps").GetComponent<AudioSource>();
		}

        private void Update()
        {
            if (inVehicle)
            {
                worldObject.SetPositionAndRotation(vehicleLocationTransform.position, vehicleLocationTransform.rotation);
            }
        }

		public override void OnAction()
		{
			if (playerMultitool.GetState() == DataConfig.MultiToolState.Deconstruct)
			{
				return;
			}

            // Enter the vehicle
            inVehicle = true;

            // Disable Player Exclusion Collider
            vehiclePlayerExclusionCollider.enabled = false;

            // Disable inventory collider
            openInventoryCollider.enabled = false;

            // Set player location to center of vehicle
            Vector3 newPlayerLocation = new Vector3(vehicleLocationTransform.position.x + playerHeadLocationTransform.localPosition.x,
                                                    vehicleLocationTransform.position.y, 
                                                    vehicleLocationTransform.position.z + playerHeadLocationTransform.localPosition.z);
			activePlayerController.SetPlayerPlacement(newPlayerLocation, activePlayerController.transform.rotation);

            // Set the height of the player to match the vehicle height
            Vector3 curCameraLocalPosition = playerLookable.m_Camera.localPosition; 
            previousPlayerCameraHeight = curCameraLocalPosition.y;
            float newPlayerCameraHeight = playerHeadLocationTransform.localPosition.y;
            playerLookable.m_Camera.localPosition = new Vector3(curCameraLocalPosition.x, newPlayerCameraHeight, curCameraLocalPosition.z);

            // Update the local camera position used by the camera shaker
            UpdateCameraShakerOriginalLocalCameraPosition(playerLookable.m_Camera.localPosition);

            // Parent vehicle to player game object
            var curLocalYAngle = rootObject.transform.localEulerAngles.y;
            rootObject.transform.localEulerAngles = new Vector3(0.0f, curLocalYAngle, 0.0f);
            previousParentTransform = rootObject.transform.parent;
            rootObject.transform.SetParent(activePlayerController.transform, true);

            // Expand player controller collider
            CharacterController playerCharController = activePlayerController.GetComponent<CharacterController>();
            previousPlayerColliderCenter = playerCharController.center;
            previousPlayerColliderHeight = playerCharController.height;
            previousPlayerColliderRadius = playerCharController.radius;
            playerCharController.height = newPlayerCameraHeight + colliderRadius;
            playerCharController.radius = colliderRadius;
            playerCharController.center = new Vector3(0f, (newPlayerCameraHeight + colliderRadius) / 2f, 0f);

            // Hide multitool
            playerArmor.SetActive(false);

            // Turn off footsteps
            previousFootstepsVolume = playerFootsteps.volume;
            playerFootsteps.volume = 0.0f;

            // Disable player jumping
            activePlayerController.GetPlayerMovable().EnableJump = false;

            // Enable exit collider
            exitVehicleCollider.enabled = true;
		}

        private void UpdateCameraShakerOriginalLocalCameraPosition(Vector3 newPosition)
        {
            HarmonyLib.AccessTools.FieldRefAccess<PlayerCameraShake, Vector3>(playerCameraShake, "originalCamPosition") = newPosition;
        }

        public void ExitVehicle()
        {
            if (inVehicle)
            {
                inVehicle = false;

                // Disable exit collider
                exitVehicleCollider.enabled = false;

                // Enable player jumping
                activePlayerController.GetPlayerMovable().EnableJump = true;

                // Enable player footsteps
                playerFootsteps.volume = previousFootstepsVolume;

                // Show multitool
                playerArmor.SetActive(true);

                // Shrink player character collider
                CharacterController playerCharController = activePlayerController.GetComponent<CharacterController>();
                playerCharController.height = previousPlayerColliderHeight;
                playerCharController.radius = previousPlayerColliderRadius;
                playerCharController.center = previousPlayerColliderCenter;

                // Reparent vehicle to world object container
                rootObject.transform.SetParent(previousParentTransform, true);

                // Set vehicle to player location
                rootObject.transform.position = activePlayerController.transform.position;

                // Reset the height of the player
                Vector3 curCameraLocalPosition = playerLookable.m_Camera.localPosition; 
                playerLookable.m_Camera.localPosition = new Vector3(curCameraLocalPosition.x, previousPlayerCameraHeight, curCameraLocalPosition.z);
                UpdateCameraShakerOriginalLocalCameraPosition(playerLookable.m_Camera.localPosition);

                // Move player to exit location.
                activePlayerController.SetPlayerPlacement(playerExitLocationTransform.position, activePlayerController.transform.rotation);

                // Enable inventory collider
                openInventoryCollider.enabled = true;

                // Enable Player Exclusion collider
                vehiclePlayerExclusionCollider.enabled = true;
            }
        }

		public override void OnHover()
		{
			Actionnable.hudHandler.DisplayCursorText("Enter", 0f, "Enter SpaceCraft");
			base.OnHover();
		}

		public override void OnHoverOut()
		{
			Actionnable.hudHandler.CleanCursorTextIfCode("Enter");
			base.OnHoverOut();
		}

		private void OnDestroy()
		{
			Actionnable.hudHandler.CleanCursorTextIfCode("Enter");
		}
    }
}