
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace kleinba.Talos
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class Laser : UdonSharpBehaviour
    {
        [HideInInspector] public Connection from;
        [HideInInspector] public Connection to;

        private Connection newfrom;
        private Connection newTo;

        private Vector3 newStart;
        private Vector3 newEnd;

        private Material newColor;
        private ReducedColors newReducedColor;
        private MeshRenderer[] renderers;
        private Collider laserCollider;

        public void Start()
        {
            renderers = GetComponentsInChildren<MeshRenderer>();
            laserCollider = GetComponent<Collider>();
        }

        public void _SetData(Connection fromConnection, Connection toConnection, Vector3 newA, Vector3 newB, Material newCol, ReducedColors color)
        {
            newfrom = fromConnection;
            newTo = toConnection;
            newStart = newA;
            newEnd = newB;
            newColor = newCol;
            newReducedColor = color;
        }

        public void _ApplyNewData()
        {
            from = newfrom;
            to = newTo;

            transform.position = newStart;
            transform.LookAt(newEnd);
            if (newReducedColor == ReducedColors.None)
            {
                transform.localScale = new Vector3(0.9f, 0.9f, Vector3.Distance(newStart, newEnd));
                if (laserCollider != null)
                    laserCollider.enabled = false;
            }
            else
            {
                transform.localScale = new Vector3(1.0f, 1.0f, Vector3.Distance(newStart, newEnd));
                if (laserCollider != null)
                    laserCollider.enabled = true;
            }


            if (renderers != null)
            {
                foreach (Renderer r in renderers)
                {
                    r.material = newColor;
                    if(from != null)
                    {
                        MaterialPropertyBlock m = new MaterialPropertyBlock();
                        m.SetFloat("_Distance", from.distance);
                        r.SetPropertyBlock(m);
                    }
                }
            }
        }
    }
}