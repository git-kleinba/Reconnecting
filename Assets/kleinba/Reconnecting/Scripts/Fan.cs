
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
namespace kleinba.Talos
{
    public class Fan : Toggleable
    {
        [SerializeField]
        Transform direction;
        [SerializeField]
        float strength;

        [SerializeField]
        GameObject onVisual;

        public override void OnPlayerTriggerEnter(VRCPlayerApi player)
        {
            if (player.isLocal && !IsOn())
            {
                player.SetVelocity(direction.forward * strength);
                player.Immobilize(true);

                SendCustomEventDelayedFrames(nameof(_WhileFlying), 1);
            }
        }

        public override void UpdateVisual()
        {
            bool on = !IsOn();
            onVisual.SetActive(on);
        }

        public void _WhileFlying()
        {
            if (!Networking.LocalPlayer.IsPlayerGrounded())
            {
                SendCustomEventDelayedFrames(nameof(_WhileFlying), 1);
            }
            else
            {
                Networking.LocalPlayer.Immobilize(false);
            }
        }
    }
}
