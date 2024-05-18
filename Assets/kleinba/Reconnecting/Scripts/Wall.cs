
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace kleinba.Talos
{
    public class Wall : Toggleable
    {
        public GameObject wallObject;

        public override void UpdateVisual()
        {
            bool isClosed = !IsOn();
            wallObject.SetActive(isClosed);
        }
    }
}