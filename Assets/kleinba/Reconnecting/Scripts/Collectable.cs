
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace kleinba.Talos
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class Collectable : UdonSharpBehaviour
    {
        public CollectableType type;
        [HideInInspector] public CollectableManager manager;
        public AudioSource audioSource;
        public Transform visual;
        public GameObject removeWhenCollected;

        bool isCollectable = true;

        private void _Collect()
        {
            if (isCollectable)
            {
                isCollectable = false;
                DisableInteractive = true;

                audioSource.Play();
                if (removeWhenCollected != null)
                {
                    removeWhenCollected.SetActive(false);
                }

                if (manager != null)
                {
                    if (type == CollectableType.Sigil)
                    {
                        _StartMoveSigil();
                        manager._GotSigil();
                    }
                    else
                    {
                        manager._GotStar();
                        visual.gameObject.SetActive(false);
                    }

                }
            }
        }

        public override void OnPlayerTriggerEnter(VRCPlayerApi player)
        {
            if (player.isLocal)
            {
                _Collect();
            }
        }
        public override void Interact()
        {
            _Collect();
        }

        private Transform moveTo;
        private float time = 0;
        private Vector3 startPosition;
        private Quaternion startRotation;
        private Vector3 startScale;
        private float flyHeight = 20.0f;

        public void _StartMoveSigil()
        {
            moveTo = manager._GetNextSigilTransform();
            startPosition = visual.position;
            startRotation = visual.rotation;
            startScale = visual.localScale;

            _MoveSigil();
        }

        public void _MoveSigil()
        {
            time += Time.deltaTime;

            visual.localScale = Vector3.Lerp(startScale, moveTo.localScale, time / 10.0f);

            if (time < 10.0f)
            {
                float t = easeInOutQuad(time / 10.0f);

                visual.position = Vector3.Lerp(startPosition, startPosition + Vector3.up * flyHeight, t);
                visual.Rotate(Vector3.one * Time.deltaTime * 30.0f);
            }
            else if (time < 20.0f)
            {
                float t = easeInOutQuad((time - 10.0f) / 10.0f);
                visual.position = Vector3.Lerp(startPosition, moveTo.position, t) + Vector3.up * flyHeight;
                visual.Rotate(Vector3.one * Time.deltaTime * 30.0f);

                startRotation = visual.rotation;
            }
            else if (time < 30.0f)
            {
                float t = easeInOutQuad((time - 20.0f) / 10.0f);
                visual.position = Vector3.Lerp(moveTo.position + Vector3.up * flyHeight, moveTo.position, t);
                visual.rotation = Quaternion.Slerp(startRotation, moveTo.rotation, t);
            }
            else
            {
                visual.position = moveTo.position;
                visual.rotation = moveTo.rotation;
                visual.localScale = moveTo.localScale;
                visual.GetComponent<MeshCollider>().enabled = true;
            }

            if (time < 30.0f)
            {
                SendCustomEventDelayedFrames(nameof(_MoveSigil), 1);
            }
        }

        private float easeInOutQuad(float x)
        {
            return x < 0.5f ? 2.0f * x * x : 1.0f - Mathf.Pow(-2.0f * x + 2.0f, 2.0f) / 2.0f;
        }
    }

    public enum CollectableType
    {
        Sigil,
        Star
    }
}