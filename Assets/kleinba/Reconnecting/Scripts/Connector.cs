using System;
using UdonSharp;
using UnityEngine;
using VRC.SDK3.Data;
using VRC.SDKBase;
using VRC.Udon;

namespace kleinba.Talos
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.NoVariableSync)]
    public class Connector : UdonSharpBehaviour
    {
        public Connection connection;

        private DataList savedConnections = new DataList();
        [SerializeField] LineRenderer previewLine;
        private bool isheld;

        private float lastUseDown;

        private Vector3 startPosition;
        private Quaternion startRotation;

        public void Start()
        {
            startPosition = transform.position;
            startRotation = transform.rotation;

            previewLine.positionCount = 2;
        }

        public override void OnPickupUseDown()
        {
            lastUseDown = Time.timeSinceLevelLoad + 1.0f - Time.deltaTime;
            SendCustomEventDelayedSeconds(nameof(_CheckHeldOneSec), 1.0f);

            //Do raycast to find objects
            Ray r = new Ray(connection.connectionPoint.position, connection.connectionPoint.forward);
            if (Physics.SphereCast(r, 0.05f, out RaycastHit hit, Mathf.Infinity, (1 << 0) | 1 << 13))
            {
                if (!Utilities.IsValid(hit) || !Utilities.IsValid(hit.transform))
                    return;

                Connection other = hit.transform.GetComponentInChildren<Connection>();
                if (other == null)
                {
                    return;
                }

                Debug.Log("Found Connection");

                if (savedConnections.Contains(other))
                {
                    _RemoveFromSavedConnections(other);
                }
                else
                {
                    _AddToSavedConnections(other);
                }
            }
        }

        public void _CheckHeldOneSec()
        {
            if (lastUseDown <= Time.timeSinceLevelLoad)
            {
                Debug.Log("Clearing cause button held down");
                savedConnections.Clear();
            }
        }

        public override void OnPickupUseUp()
        {
            lastUseDown = Mathf.Infinity;
        }

        public override void OnPickup()
        {
            savedConnections.Clear();

            savedConnections = connection.connections.ShallowClone();

            connection._ClearAllConnections();

            previewLine.gameObject.SetActive(true);
            isheld = true;

            _WhileHeld();
        }

        public override void OnDrop()
        {
            previewLine.gameObject.SetActive(false);
            isheld = false;

            for(int i = 0; i < savedConnections.Count; i++)
            {
                Connection c = (Connection)savedConnections[i].Reference;

                c._AddConnection(connection);
                connection._AddConnection(c);
            }
        }

        public void _WhileHeld()
        {
            if (isheld)
            {
                previewLine.positionCount = 2 + 2 * savedConnections.Count;

                previewLine.SetPosition(0, connection.connectionPoint.position);
                Ray ray = new Ray(connection.connectionPoint.position, connection.connectionPoint.forward);

                if (Physics.SphereCast(ray, 0.05f, out RaycastHit hit, 50.0f, (1 << 0) | (1 << 13) | (1 << 4)))
                {
                    previewLine.SetPosition(1, hit.point);
                }
                else
                {
                    previewLine.SetPosition(1, ray.GetPoint(50.0f));
                }

                for (int i = 0; i < savedConnections.Count; i++)
                {
                    previewLine.SetPosition(2 + 2 * i, connection.connectionPoint.position);
                    previewLine.SetPosition(3 + 2 * i, ((Connection)savedConnections[i].Reference).connectionPoint.position);
                }

                SendCustomEventDelayedFrames(nameof(_WhileHeld), 1);
            }
        }

        public void _AddToSavedConnections(Connection newConnection)
        {
            if (savedConnections.Contains(newConnection))
                return;

            savedConnections.Add(newConnection);
        }

        public void _RemoveFromSavedConnections(Connection toRemove)
        {
            savedConnections.Remove(toRemove);
        }
    }
}