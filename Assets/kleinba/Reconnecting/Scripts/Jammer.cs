
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace kleinba.Talos
{
    public class Jammer : UdonSharpBehaviour
    {
        [HideInInspector] public LaserEvaluator manager;
        [SerializeField] private Transform head;
        [SerializeField] private Transform preview;
        [SerializeField] private LayerMask RaycastMask;
        [SerializeField] private LineRenderer previewLine;
        [SerializeField] private Toggleable preTarget;

        [UdonSynced] public int netTarget = -1;
        [UdonSynced] public Vector3 netJamPosition;

        private Toggleable target;
        private Vector3 jamPosition;

        private bool isheld;

        private bool isJamming;

        public void Start()
        {
            previewLine.positionCount = 2;

            //if (preTarget != null)
            //{
            //    jamPosition = preTarget.transform.position + preTarget.boundingBox.center;
            //    target = preTarget;
            //    _WhileJamming();
            //}
            if (Networking.LocalPlayer.isMaster)
            {
                _Reset();
            }
        }

        public override void OnPickup()
        {
            //head.localRotation = Quaternion.identity;

            netTarget = -1;
            _Sync();

            previewLine.gameObject.SetActive(true);
            isheld = true;
            _WhileHeld();
        }
        public override void OnDrop()
        {
            isheld = false;
            preview.gameObject.SetActive(false);
            previewLine.gameObject.SetActive(false);

            if (target != null)
            {
                target._Jam(this, true);
            }
        }

        public override void OnPickupUseDown()
        {
            Wall w = GetWall();
            if (w != null)
            {
                netTarget = w.id;
                netJamPosition = jamPosition;
                _Sync();
                GetComponent<VRC_Pickup>().Drop();
            }
        }


        public void _WhileHeld()
        {
            if (isheld)
            {
                SetPreview(GetWall());
                SendCustomEventDelayedFrames(nameof(_WhileHeld), 1);
            }
        }

        private Wall GetWall()
        {
            Ray ray = new Ray(head.position, head.forward);
            float distance = float.PositiveInfinity;
            if (Physics.Raycast(ray, out RaycastHit hit, 50.0f, 1))
            {
                distance = hit.distance;
            }

            //Find closest closed door, else use the furthest open door
            RaycastHit[] hits = Physics.SphereCastAll(ray, 0.1f, distance, RaycastMask);

            float closestClosed = float.PositiveInfinity;
            Wall closestClosedDoor = null;
            float farthestOpen = 0;
            Wall farthestOpenWall = null;

            Vector3 closestClosedPoint = Vector3.zero;
            Vector3 farthestOpenPoint = Vector3.zero;

            foreach (RaycastHit h in hits)
            {
                Wall w = h.transform.GetComponent<Wall>();
                if (!Utilities.IsValid(w))
                    continue;

                if (w.IsOn())
                {
                    if (h.distance > farthestOpen)
                    {
                        farthestOpen = h.distance;
                        farthestOpenPoint = h.point;
                        farthestOpenWall = w;
                    }
                }
                else
                {
                    if (h.distance < closestClosed)
                    {
                        closestClosed = h.distance;
                        closestClosedPoint = h.point;
                        closestClosedDoor = w;
                    }
                }
            }

            if (closestClosedDoor != null)
            {
                jamPosition = closestClosedPoint;
                return closestClosedDoor;
            }

            jamPosition = farthestOpenPoint;
            return farthestOpenWall;
        }

        private void SetPreview(Wall w)
        {
            preview.gameObject.SetActive(w != null);

            if (w != null)
            {
                BoxCollider b = w.boundingBox;
                preview.SetPositionAndRotation(w.transform.position + w.transform.rotation * b.center, w.transform.rotation);
                preview.localScale = Vector3.Scale(b.size, w.transform.localScale) / 0.75f + Vector3.one * 0.05f;
            }

            previewLine.SetPosition(0, head.position);
            previewLine.SetPosition(1, head.position + head.forward * 50.0f);
        }

        public void _WhileJamming()
        {
            if (isJamming && target != null)
            {
                head.LookAt(jamPosition);

                Vector3 ab = jamPosition - head.position;
                Ray ray = new Ray(head.position, ab.normalized);

                if (Physics.SphereCast(ray, 0.05f, out RaycastHit hit, ab.magnitude + 0.05f, 1 | 1 << 27))
                {
                    //Something in the way but it's just itself / something close
                    target._Jam(this, ab.magnitude - Mathf.Min(hit.distance, ab.magnitude) < 0.075f || hit.transform.parent == target.transform); // hit.transform == target.transform);
                }
                else
                {
                    //Nothing in the way
                    target._Jam(this, true);
                }

                SendCustomEventDelayedFrames(nameof(_WhileJamming), 1);
            }
            else
            {
                isJamming = false;
            }
        }

        public void _Reset()
        {
            if (target != null)
            {
                target._Jam(this, false);
                target = null;
            }

            if (preTarget != null)
            {
                netTarget = preTarget.id;
                netJamPosition = preTarget.transform.position + preTarget.boundingBox.center;
                _Sync();
            }
            else
            {
                netTarget = -1;
                _Sync();
            }
        }

        public void _Sync()
        {
            RequestSerialization();
            OnDeserialization();
        }

        public override void OnDeserialization()
        {
            if (netTarget >= 0 && netTarget < manager.toggleables.Length)
            {
                jamPosition = netJamPosition;

                if (target != null && target.id != netTarget)
                {
                    target._Jam(this, false);
                }

                target = manager.toggleables[netTarget];

                if (!isJamming)
                {
                    isJamming = true;
                    _WhileJamming();
                }
            }
            else
            {
                isJamming = false;

                if (target != null)
                {
                    target._Jam(this, false);
                    target = null;
                }

                head.localRotation = Quaternion.identity;
            }

        }
    }
}