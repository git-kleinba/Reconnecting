
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
namespace kleinba.Talos
{
    public class Receiver : Connection
    {
        public ReducedColors wants;
        //public Color wants;
        public Toggleable[] targets;
        public float turnOnAfter = 1.0f;
        public Transform visual;

        float time = 0.0f;
        bool isOn;
        float lastUpdateTime;

        public void _UpdateReceiverVisuals()
        {
            if (color == wants)
            {
                time += Time.timeSinceLevelLoad - lastUpdateTime;

                if (time >= turnOnAfter)
                {
                    if (!isOn)
                    {
                        //posedge
                        _SetTargetsPower(true);
                        isOn = true;
                    }

                    time = turnOnAfter;
                }
            }
            //if (powerColor == wants)
            //{
            //    time += Time.timeSinceLevelLoad - lastUpdateTime;
            //
            //    if (time >= turnOnAfter)
            //    {
            //        if (!isOn)
            //        {
            //            //posedge
            //            target.On();
            //            isOn = true;
            //        }
            //
            //        time = turnOnAfter;
            //    }
            //}
            else
            {
                if (isOn)
                {
                    //negedge
                    _SetTargetsPower(false);
                    isOn = false;
                }
                time = 0.0f;
            }

            visual.localScale = new Vector3(Mathf.Lerp(5.0f, 100.0f, time / turnOnAfter), 100.0f, 100.0f);
            lastUpdateTime = Time.timeSinceLevelLoad;
        }

        public void _SetTargetsPower(bool value)
        {
            foreach (Toggleable t in targets)
            {
                t._Power(value);
            }
        }
    }
}
