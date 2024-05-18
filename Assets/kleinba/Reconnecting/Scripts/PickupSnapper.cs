
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
namespace kleinba.Talos
{
    [DefaultExecutionOrder(-1), UdonBehaviourSyncMode(BehaviourSyncMode.NoVariableSync)]
    public class PickupSnapper : UdonSharpBehaviour
    {
        public Transform stackingPoint;
        public Transform originPoint;
        public Transform preview;
        public float shrinkScale = 0.75f;

        public LayerMask collisionMask;

        private Vector3 localOriginPointPosition;
        private Quaternion localOriginPointRotation;
        private bool skipDistanceCheck;
        private bool isholding;
        private Rigidbody rb;

        private Vector3 startPosition;
        private Quaternion startRotation;

        public void Start()
        {
            startPosition = transform.position;
            startRotation = transform.rotation;

            rb = GetComponent<Rigidbody>();

            if (originPoint != null)
            {
                localOriginPointPosition = Quaternion.Inverse(transform.rotation) * (originPoint.position - transform.position);
                localOriginPointRotation = Quaternion.Inverse(transform.rotation) * originPoint.rotation;
            }

        }

        public override void OnPickup()
        {
            preview.gameObject.SetActive(true);
            isholding = true;
            _WhileHeld();

            //Check for doors and push player to same side as pickup
            Vector3 headPos = Networking.LocalPlayer.GetTrackingData(VRCPlayerApi.TrackingDataType.Head).position;
            Vector3 direction = (headPos - transform.position);
            Ray r = new Ray(transform.position, direction.normalized);
            int noInteract = 1 << 22;
            if (Physics.SphereCast(r, 0.05f, out RaycastHit hit, direction.magnitude, noInteract))
            {
                Vector3 tpPos = r.GetPoint(hit.distance);
                tpPos.y = Networking.LocalPlayer.GetPosition().y;
                Networking.LocalPlayer.TeleportTo(tpPos, Networking.LocalPlayer.GetRotation());
            }
            else if (Physics.SphereCast(r, 0.05f, out RaycastHit hit2, direction.magnitude, 1))
            {
                skipDistanceCheck = true;
                GetComponent<VRC_Pickup>().Drop();
                skipDistanceCheck = false;
            }

            //Custom layer
            gameObject.layer = 24;
            transform.localScale = transform.localScale * shrinkScale;
            GetComponent<Rigidbody>().freezeRotation = false;
        }

        public override void OnDrop()
        {
            preview.gameObject.SetActive(false);
            isholding = false;

            transform.localScale = transform.localScale / shrinkScale;

            if (!skipDistanceCheck && Vector3.Distance(transform.position, Networking.LocalPlayer.GetPosition() + Vector3.up * 0.25f) > 1.25f)
            {
                //Trying to drop it too far away from you. Drops it on you
                transform.position = Networking.LocalPlayer.GetPosition();
            }

            //Reset rb and rotation

            rb.velocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            transform.eulerAngles = new Vector3(0, transform.eulerAngles.y, 0);
            rb.freezeRotation = true;

            Ray r = new Ray(transform.position + Vector3.up * 0.5f, Vector3.down);
            //Collides with default and pickup
            Vector3 pointToSnap = originPoint.position;
            bool hasNotHit = true;
            if (Physics.SphereCast(r, 0.1f, out RaycastHit hit, 2.0f, collisionMask))
            {
                if (Utilities.IsValid(hit.transform))
                {
                    PickupSnapper other = hit.transform.GetComponent<PickupSnapper>();
                    if (other != null && other.stackingPoint != null)
                    {
                        //TODO: Check if there's actually space there
                        pointToSnap = other.stackingPoint.position;
                        //transform.position += other.stackingPoint.position - originPoint.position;
                        transform.rotation *= Quaternion.Inverse(originPoint.rotation) * other.stackingPoint.rotation;
                        hasNotHit = false;
                    }
                }
            }

            if (hasNotHit)
            {
                if (Physics.SphereCast(r, 0.1f, out RaycastHit hit2, 2.0f, 1))
                {
                    pointToSnap = hit.point;
                    //transform.position += (hit2.distance - 0.35f) * Vector3.down;
                }
            }

            transform.position += pointToSnap - originPoint.position;

            //TODO: Visually show the place where it will be put down

            //Pickup
            gameObject.layer = 13;
        }

        public void _WhileHeld()
        {
            if (isholding)
            {
                SendCustomEventDelayedFrames(nameof(_WhileHeld), 0);
                preview.position = transform.position;
                preview.eulerAngles = new Vector3(0, transform.eulerAngles.y, 0);
                SetProjectedPosition(false, preview);
            }
        }

        private void SetProjectedPosition(bool skipDistance, Transform toSnap)
        {
            if (!skipDistance && Vector3.Distance(transform.position, Networking.LocalPlayer.GetPosition() + Vector3.up * 0.25f) > 1.25f)
            {
                //Trying to drop it too far away from you. Drops it on you
                toSnap.position = Networking.LocalPlayer.GetPosition();
            }

            Ray r = new Ray(toSnap.position + Vector3.up * 0.5f, Vector3.down);
            //Collides with default and pickup
            Vector3 pointToSnap = toSnap.position + toSnap.rotation * localOriginPointPosition;
            bool hasNotHit = true;
            if (Physics.SphereCast(r, 0.1f, out RaycastHit hit, 2.0f, collisionMask))
            {
                if (Utilities.IsValid(hit.transform))
                {
                    PickupSnapper other = hit.transform.GetComponent<PickupSnapper>();
                    if (other != null && other.stackingPoint != null)
                    {
                        //TODO: Check if there's actually space there
                        pointToSnap = other.stackingPoint.position;
                        //transform.position += other.stackingPoint.position - originPoint.position;
                        toSnap.rotation = localOriginPointRotation * other.stackingPoint.rotation;
                        hasNotHit = false;
                    }
                }
            }
            // TODO: fix snapping to bottom using  

            if (hasNotHit)
            {
                if (Physics.SphereCast(r, 0.1f, out RaycastHit hit2, 2.0f, 1))
                {
                    pointToSnap = hit.point;
                    //transform.position += (hit2.distance - 0.35f) * Vector3.down;
                }
            }

            toSnap.position = pointToSnap - localOriginPointPosition;
        }

        public void OnTriggerEnter(Collider other)
        {
            if (other.name.Contains("Drop"))
            {
                GetComponent<VRC_Pickup>().Drop();
            }
        }

        public void OnCollisionEnter(Collision collision)
        {
            if (collision.transform.name.Contains("Connector"))
            {
                Vector3 direction = transform.position - collision.transform.position;
                direction.y = 0;
                if (direction.magnitude < 0.05f)
                {
                    if (rb != null)
                    {
                        rb.AddForce(direction.normalized * 5.0f);
                    }
                }
            }
        }

        public void _Reset()
        {
            transform.SetPositionAndRotation(startPosition, startRotation);
        }
    }
}