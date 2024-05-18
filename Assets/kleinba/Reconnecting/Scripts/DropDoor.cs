
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
namespace kleinba.Talos
{
    public class DropDoor : UdonSharpBehaviour
    {
        public override void OnPlayerTriggerStay(VRCPlayerApi player)
        {
            if (!player.isLocal)
                return;

            VRC_Pickup pickupl = player.GetPickupInHand(VRC_Pickup.PickupHand.Left);
            if (Utilities.IsValid(pickupl))
            {
                pickupl.transform.position = player.GetTrackingData(VRCPlayerApi.TrackingDataType.Head).position;
                pickupl.Drop();
            }

            VRC_Pickup pickupr = player.GetPickupInHand(VRC_Pickup.PickupHand.Right);
            if (Utilities.IsValid(pickupr))
            {
                pickupr.transform.position = player.GetTrackingData(VRCPlayerApi.TrackingDataType.Head).position;
                pickupr.Drop();
            }
        }
    }
}