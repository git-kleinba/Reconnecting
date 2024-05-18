
using UdonSharp;
using UnityEngine;
using VRC.SDK3.Data;
using VRC.SDKBase;
using VRC.Udon;
namespace kleinba.Talos
{
    public class Toggleable : UdonSharpBehaviour
    {
        [HideInInspector] public LaserEvaluator manager;
        [HideInInspector] public int id;

        public BoxCollider boundingBox;

        public OnState onState;
        private DataList jammers = new DataList();
        int laserValue;

        public void Start()
        {
            UpdateVisual();
        }

        public void _Power(bool value)
        {
            laserValue += value ? 1 : -1;
            UpdateVisual();
        }

        public void _Jam(Jammer jammer, bool value)
        {
            if (value)
            {
                if (!jammers.Contains(jammer))
                    jammers.Add(jammer);
            }
            else
            {
                jammers.Remove(jammer);
            }

            UpdateVisual();
        }

        public virtual void UpdateVisual()
        {

        }

        public bool IsOn()
        {
            bool temp = (onState == OnState.defaultOn) ? laserValue != 0 : laserValue == 0;
            return temp || (jammers.Count != 0);
        }

    }

    public enum OnState
    {
        defaultOn,
        defaultOff
    }
}