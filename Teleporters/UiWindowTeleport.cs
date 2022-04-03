using System;
using MijuTools;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SpaceCraft
{
	public class UiWindowTeleport : UiWindow
	{
        public static int UI_WINDOW_ID = 1000;
        
        public TextMeshProUGUI TeleportDestinationString;
        public Color ValidDestinationColor;
        public Color InvalidDestinationColor;
        public Button[] AddressDigits = new Button[16];
        public Button TeleportButton;
        
        private string destinationString = "";

		public override DataConfig.UiType GetUiIdentifier()
		{
			this.uiIdentifier = (DataConfig.UiType) UI_WINDOW_ID;
			return this.uiIdentifier;
		}

        private void Awake()
        {
            TeleportDestinationString.text = "";
            for (int buttonId = 0; buttonId < 16; buttonId++)
            {
                int localButtonId = buttonId;
                AddressDigits[buttonId].onClick.AddListener(delegate{ClickDigitButton(localButtonId);});
            }
            TeleportButton.onClick.AddListener(ClickTeleport);
        }

        private void OnEnable()
        {
            destinationString = "";
            UpdateDestinationString();
        }

        private void ClickDigitButton(int value)
        {
            Debug.Log("ClickDigitButton(" + value + "), value.ToString('X') = " + value.ToString("X"));
            destinationString += value.ToString("X");
            if (destinationString.Length > 6)
            {
                destinationString = destinationString.Substring(1);
            }
            UpdateDestinationString();
        }

        private void UpdateDestinationString()
        {
            TeleportDestinationString.text = destinationString;
            if (DestinationValid())
            {
                TeleportDestinationString.color = ValidDestinationColor;
            }
            else
            {
                TeleportDestinationString.color = InvalidDestinationColor;
            }
        }

        private bool DestinationValid()
        {
            return Teleporter.DestinationValid(destinationString);
        }

        private void ClickTeleport()
        {
            if (DestinationValid())
            {
                PlayerMainController activePlayerController = Managers.GetManager<PlayersManager>().GetActivePlayerController();
                Transform destination = Teleporter.GetDestinationTranform(destinationString);
                activePlayerController.SetPlayerPlacement(destination.position, destination.rotation);
            }
            Managers.GetManager<WindowsHandler>().CloseAllWindows();
        }
    }
}
