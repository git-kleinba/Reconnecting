
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
namespace kleinba.Talos
{
    public class SetTransformOnPlayerTriggerEnter : UdonSharpBehaviour
    {
        public Transform target;
        public Transform defaultPosition;

        public Transform to;

        bool isInPuzzle;

        public override void OnPlayerTriggerEnter(VRCPlayerApi player)
        {
            if (player.isLocal)
            {
                isInPuzzle = true;
                target.SetPositionAndRotation(to.position, to.rotation);
            }
        }

        public override void OnPlayerRespawn(VRCPlayerApi player)
        {
            if (player.isLocal)
            {
                if (isInPuzzle)
                {
                    target.SetPositionAndRotation(defaultPosition.position, defaultPosition.rotation);
                }
            }
        }
    }
}
