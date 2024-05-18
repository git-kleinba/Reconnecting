
using System;
using UdonSharp;
using UnityEngine;
using VRC.SDK3.Data;
using VRC.SDKBase;
using VRC.Udon;

namespace kleinba.Talos
{
    //[UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
    public class Connection : UdonSharpBehaviour
    {
        [HideInInspector] public int id;
        [HideInInspector] public LaserEvaluator manager;

        public Transform colliderTransform;

        //Stores int's
        [HideInInspector] public DataList netHelper = new DataList();
        [UdonSynced] public int[] netConnections;

        //Stores Connections
        [HideInInspector] public DataList connections = new DataList();
        [HideInInspector] public int distance;
        [HideInInspector] public ReducedColors color;
        public Transform connectionPoint;

        [SerializeField]
        private Connection[] preConnections;

        //For non synced connectors
        public bool alwaysDoPreconnections;

        public virtual void Start()
        {
            if (Networking.LocalPlayer.isMaster || alwaysDoPreconnections)
            {
                _SetupPreconnections();
            }
        }

        public void _AddConnection(Connection newConnection)
        {
            if (!netHelper.Contains(newConnection.id))
            {
                netHelper.Add(newConnection.id);
                _Sync();
            }
        }

        public void _RemoveConnection(Connection toRemove)
        {
            netHelper.Remove(toRemove.id);
            _Sync();
        }

        public void _ClearAllConnections()
        {
            DataToken[] tokens = connections.ToArray();
            foreach (DataToken t in tokens)
            {
                Connection c = (Connection)t.Reference;
                c._RemoveConnection(this);
            }

            netHelper.Clear();
            _Sync();
        }

        public void _SetupPreconnections()
        {
            foreach (Connection c in preConnections)
            {
                _AddConnection(c);
                c._AddConnection(this);
            }
        }

        public void _Sync()
        {
            Networking.SetOwner(Networking.LocalPlayer, gameObject);

            netConnections = new int[netHelper.Count];
            for (int i = 0; i < netHelper.Count; i++)
            {
                netConnections[i] = netHelper[i].Int;
            }

            RequestSerialization();
            OnDeserialization();
        }

        public override void OnDeserialization()
        {
            connections.Clear();
            netHelper.Clear();
            Connection[] cons = manager.allConnections;
            foreach (int i in netConnections)
            {
                connections.Add(cons[i]);
                netHelper.Add(i);
            }
        }
    }
}
