
using UdonSharp;
using UnityEngine;
using VRC.SDK3.Data;
using VRC.SDKBase;
using VRC.Udon;
namespace kleinba.Talos
{
    public class PressureButton : UdonSharpBehaviour
    {
        public Toggleable target;

        DataList list = new DataList();
        bool localPlayerInside;
        bool currentState;

        public override void OnPlayerTriggerEnter(VRCPlayerApi player)
        {
            if (!player.isLocal)
                return;
            localPlayerInside = true;
            _UpdateState();
        }
        public override void OnPlayerTriggerExit(VRCPlayerApi player)
        {
            if (!player.isLocal)
                return;
            localPlayerInside = false;
            _UpdateState();
        }

        public void OnTriggerEnter(Collider other)
        {
            if (!list.Contains(other))
            {
                list.Add(other);
            }
            _UpdateState();
        }
        public void OnTriggerExit(Collider other)
        {
            list.Remove(other);
            _UpdateState();
        }

        private void _UpdateState()
        {
            if (currentState)
            {
                if (list.Count == 0 && !localPlayerInside)
                {
                    currentState = false;
                    target._Power(false);
                }
            }
            else
            {
                if (list.Count != 0 || localPlayerInside)
                {
                    currentState = true;
                    target._Power(true);
                }
            }
        }
    }
}