
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
namespace kleinba.Talos
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class ScaleToEyeHeight : UdonSharpBehaviour
    {
        Vector3 originalScale;
        public float OriginalHeight = 1.8f;

        public void Start()
        {
            originalScale = transform.localScale;
        }

        public override void OnAvatarEyeHeightChanged(VRCPlayerApi player, float prevEyeHeightAsMeters)
        {
            if (player.isLocal)
            {
                float playerHeight = player.GetAvatarEyeHeightAsMeters();
                transform.localScale = originalScale * playerHeight / OriginalHeight;
            }
        }
    }
}