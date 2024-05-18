
using UdonSharp;
using UnityEngine;
using VRC.SDK3.Data;
using VRC.SDKBase;
using VRC.Udon;
namespace kleinba.Talos
{
    [DefaultExecutionOrder(-1), UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class LaserEvaluator : UdonSharpBehaviour
    {
        [HideInInspector] public Connection[] allConnections;
        [HideInInspector] public Toggleable[] toggleables;
        private LaserEmitter[] Lasers;
        private Receiver[] receivers;

        [SerializeField] private Transform laserColliderParent;
        [SerializeField] private GameObject laserColliderPrefab;
        [SerializeField] private GameObject laserCollideObject;
        [SerializeField] private Material[] colors;
        [SerializeField] private LayerMask laserCollidesWith;

        private int nextLaserObjectIndex = 0;
        private DataList laserObjects = new DataList();
        private int nextLaserCollisionObjectIndex = 0;
        private DataList laserCollisionObjects = new DataList();

        public void Start()
        {
            Lasers = GetComponentsInChildren<LaserEmitter>();
            allConnections = GetComponentsInChildren<Connection>();

            for (int i = 0; i < allConnections.Length; i++)
            {
                Connection c = allConnections[i];
                c.id = i;
                c.manager = this;
            }

            receivers = GetComponentsInChildren<Receiver>();
            toggleables = GetComponentsInChildren<Toggleable>();

            for (int i = 0; i < toggleables.Length; i++)
            {
                Toggleable t = toggleables[i];
                t.id = i;
                t.manager = this;
            }

            Jammer[] jammers = GetComponentsInChildren<Jammer>();
            foreach (Jammer j in jammers)
            {
                j.manager = this;
            }

            SendCustomEventDelayedFrames(nameof(_UpdateLasers), 0);
        }

        public void _UpdateLasers()
        {
            SendCustomEventDelayedFrames(nameof(_UpdateLasers), 1);

            nextLaserObjectIndex = 0;
            nextLaserCollisionObjectIndex = 0;

            foreach (Connection c in allConnections)
            {
                c.distance = int.MaxValue;
                c.color = ReducedColors.None;
            }

            DataList list = new DataList();
            foreach (LaserEmitter c in Lasers)
            {
                c.distance = 0;
                c.color = c.laserColor;
                list.Add(c);
            }

            int maxIters = 100;
            int iters = 0;

            while (list.Count != 0 && iters < maxIters)
            {
                iters++;

                Connection c = (Connection)list[0].Reference;
                list.RemoveAt(0);

                if (c.color == ReducedColors.None)
                    continue;

                if (c.name.StartsWith("Receiver"))
                    continue;

                DataToken[] connections = c.connections.ToArray();

                for (int i = 0; i < connections.Length; i++)
                {
                    DataToken dtc2 = connections[i];
                    Connection c2 = (Connection)dtc2.Reference;
                    Vector3 direction = c2.connectionPoint.position - c.connectionPoint.position;
                    Ray r = new Ray(c.connectionPoint.position, direction.normalized);

                    RaycastHit[] hits = Physics.SphereCastAll(r, 0.02f, direction.magnitude, laserCollidesWith);

                    bool noCollision = true;
                    if (hits.Length > 0 && GetShortestRayCastHit(hits, c, c2, out RaycastHit shortest) != float.MaxValue)
                    {
                        noCollision = false;
                        CreateLaserObject(c, null, r.GetPoint(shortest.distance + 0.02f), c.color);
                        CreateLaserCollisionObject(r.GetPoint(shortest.distance + 0.02f));
                    }

                    if (noCollision)
                    {
                        if (c2.distance <= c.distance && c2.color == ReducedColors.None)
                        {
                            CreateLaserObject(c, c2, c2.connectionPoint.position, c.color);
                        }
                        else if (c2.distance == c.distance)
                        {
                            CreateLaserObject(c, null, Vector3.Lerp(c.connectionPoint.position, c2.connectionPoint.position, 0.5f), c.color);
                            if (c.color != c2.color)
                            {
                                CreateLaserCollisionObject(Vector3.Lerp(c.connectionPoint.position, c2.connectionPoint.position, 0.5f));
                            }
                        }
                        else if (c2.distance > c.distance + 1)
                        {
                            c2.color = c.color;
                            c2.distance = c.distance + 1;

                            list.Add(c2);
                            CreateLaserObject(c, c2, c2.connectionPoint.position, c.color);
                        }
                        else if (c2.distance == c.distance + 1 && c.color != c2.color)
                        {
                            c2.color = ReducedColors.None;
                            CreateLaserObject(c, c2, c2.connectionPoint.position, c.color);
                        }
                    }

                    //Make receivers always be far away
                    if (c2.name.StartsWith("Receiver"))
                    {
                        c2.distance = 10_000;
                    }
                }
            }

            //Make black lines on unexplored connections
            foreach (Connection c in allConnections)
            {
                if (c.distance == int.MaxValue)
                {
                    DataToken[] conns = c.connections.ToArray();
                    foreach (DataToken dt in conns)
                    {
                        Connection c2 = (Connection)dt.Reference;
                        if (c2.distance == int.MaxValue)
                        {
                            if (c.id > c2.id)
                            {
                                CreateLaserObject(c, c2, c2.connectionPoint.position, ReducedColors.None);
                            }
                        }
                        else
                        {
                            CreateLaserObject(c, c2, c2.connectionPoint.position, ReducedColors.None);
                        }
                    }
                }
            }

            //Apply the new state and hide all unused objects
            ApplyUsedLasers();
            HideUnusedLaserObjects();
            HideUnusedCollisionObjects();

            foreach (Receiver r in receivers)
            {
                r._UpdateReceiverVisuals();
            }
        }

        private float GetShortestRayCastHit(RaycastHit[] hits, Connection c, Connection c2, out RaycastHit shortest)
        {
            shortest = hits[0];
            float shortestLength = float.MaxValue;
            foreach (RaycastHit hit in hits)
            {
                Laser l = hit.transform.GetComponent<Laser>();
                if (l != null && (l.from != c && l.to != c && l.from != c2 && l.to != c2))
                {
                    if (shortestLength > hit.distance)
                    {
                        shortestLength = hit.distance;
                        shortest = hit;
                    }
                }
                else
                {
                    //Everything besides lasers
                    if (hit.transform.gameObject.layer != 25)
                    {
                        //Collide with things other than the 2 connectors
                        if (c.colliderTransform != hit.transform && c2.colliderTransform != hit.transform)
                        {
                            if (shortestLength > hit.distance)
                            {
                                shortestLength = hit.distance;
                                shortest = hit;
                            }
                        }
                    }
                }
            }

            return shortestLength;
        }

        private void CreateLaserObject(Connection from, Connection to, Vector3 end, ReducedColors color)
        {
            if (nextLaserObjectIndex >= laserObjects.Count)
            {
                Debug.Log("World: Created New Laser Collider Object");
                Laser tempLas = Instantiate(laserColliderPrefab).GetComponent<Laser>();
                tempLas.transform.parent = laserColliderParent;
                laserObjects.Add(tempLas);
            }

            Laser laser = (Laser)laserObjects[nextLaserObjectIndex].Reference;
            laser._SetData(from, to, from.connectionPoint.position, end, colors[(int)color], color);
            nextLaserObjectIndex++;
        }

        private void CreateLaserCollisionObject(Vector3 position)
        {
            if (nextLaserCollisionObjectIndex >= laserCollisionObjects.Count)
            {
                Debug.Log("World: Created New Laser Collision Object");
                Transform colide = Instantiate(laserCollideObject).transform;
                colide.parent = laserColliderParent;
                laserCollisionObjects.Add(colide);
                AudioSource audioS = colide.GetComponent<AudioSource>();
                audioS.time = Random.Range(0, audioS.clip.length);
            }
            Transform t = (Transform)laserCollisionObjects[nextLaserCollisionObjectIndex].Reference;
            t.position = position;
            nextLaserCollisionObjectIndex++;
        }

        private void HideUnusedCollisionObjects()
        {
            if (nextLaserCollisionObjectIndex >= laserCollisionObjects.Count)
                return;

            for (int i = nextLaserCollisionObjectIndex; i < laserCollisionObjects.Count; i++)
            {
                Transform t = (Transform)laserCollisionObjects[i].Reference;
                t.position = new Vector3(0, -1000, 0);
            }
        }

        private void ApplyUsedLasers()
        {
            for (int i = 0; i < nextLaserObjectIndex; i++)
            {
                Laser laser = (Laser)laserObjects[i].Reference;
                laser._ApplyNewData();
            }
        }

        private void HideUnusedLaserObjects()
        {
            if (nextLaserObjectIndex >= laserObjects.Count)
                return;

            for (int i = nextLaserObjectIndex; i < laserObjects.Count; i++)
            {
                Laser laser = (Laser)laserObjects[i].Reference;
                laser._SetData(null, null, new Vector3(0, -1000, 0), new Vector3(0, -1000, 1), colors[0], ReducedColors.Red);
                laser._ApplyNewData();
            }
        }

        public void _ResetAll()
        {
            //Drop all held pickups
            VRC_Pickup pickupl = Networking.LocalPlayer.GetPickupInHand(VRC_Pickup.PickupHand.Left);
            if (pickupl != null)
                pickupl.Drop();
            VRC_Pickup pickupr = Networking.LocalPlayer.GetPickupInHand(VRC_Pickup.PickupHand.Right);
            if (pickupr != null)
                pickupr.Drop();

            //Clear connections
            Connection[] connectors = GetComponentsInChildren<Connection>();
            foreach (Connection c in connectors)
            {
                c.netHelper.Clear();
                c._Sync();
            }
            foreach (Connection c in connectors)
            {
                c._SetupPreconnections();
            }

            VRCPlayerApi localPlayer = Networking.LocalPlayer;

            //Reset Jammer connections
            Jammer[] jammers = GetComponentsInChildren<Jammer>();
            foreach (Jammer j in jammers)
            {
                Networking.SetOwner(localPlayer, j.gameObject);
                j._Reset();
            }

            //Reset positions
            PickupSnapper[] snappers = GetComponentsInChildren<PickupSnapper>();
            foreach (PickupSnapper sn in snappers)
            {
                Networking.SetOwner(localPlayer, sn.gameObject);
                sn._Reset();
            }
        }
    }

    public enum ReducedColors
    {
        Red,
        Green,
        Blue,
        None
    }
}