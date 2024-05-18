
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
namespace kleinba.Talos
{
    public class EventRetargeter : UdonSharpBehaviour
    {
        public Terminal terminal;
        public int selection;

        public void _CallFunction()
        {
            terminal._MakeSelection(selection);
        }
    }
}
