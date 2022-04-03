using System.Collections.Generic;
using SpaceCraft;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using System.Text;
using MijuTools;

public class Teleporter : MonoBehaviour
{
    public TextMeshProUGUI AddressLabel;
    public Transform TeleportPlayerLocation;

    private bool initialized = false;
    private string id;

    private static Dictionary<string, Teleporter> teleportersById = new Dictionary<string, Teleporter>(); 

    public static bool DestinationValid(string destinationId)
    {
        return teleportersById.ContainsKey(destinationId);
    }

    public static Transform GetDestinationTranform(string destinationId)
    {
        if (DestinationValid(destinationId))
        {
            return teleportersById[destinationId].TeleportPlayerLocation;
        }
        return null;
    }

    private void Awake()
    {
        AddressLabel.text = "******";
    }

    private void Start()
    {
        WorldObjectAssociated associatedObject = this.GetComponent<WorldObjectAssociated>();
        if (associatedObject != null)
        {
            WorldObject worldObject = associatedObject.GetWorldObject(); 
            if (worldObject != null)
            {
                initialized = true;
                id = ConvertObjectIdToString(worldObject.GetId());
                AddressLabel.text = id;
                teleportersById[id] = this;
            }
        }
    }

    private string ConvertObjectIdToString(int objectId)
    {
        int variablePartOfId = objectId & 0xFFFFFF;
        return variablePartOfId.ToString("X");
    }

    private void OnDestroy()
    {
        if (initialized)
        {
            teleportersById.Remove(id);
        }
    }    
}
