
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
namespace kleinba.Talos
{
    public class CollectableManager : UdonSharpBehaviour
    {
        public int collectableCount = 0;
        private int maxCollectable = 0;
        public int starCount = 0;
        private int maxStar = 0;
        public GameObject[] collectableVisual;
        public GameObject turnOnOnComplete;
        public GameObject allStars;

        private Collectable[] collectables;

        void Start()
        {
            collectables = GetComponentsInChildren<Collectable>();
            foreach (Collectable c in collectables)
            {
                c.manager = this;
                if (c.type == CollectableType.Sigil)
                {
                    maxCollectable++;
                }
                else
                {
                    maxStar++;
                }
            }

            for (int i = 0; i < collectableVisual.Length; i++)
            {
                collectableVisual[i].SetActive(false);
            }
        }

        public Transform _GetNextSigilTransform()
        {
            return collectableVisual[collectableCount].transform;
        }

        public void _GotSigil()
        {
            collectableCount++;

            _UpdateVisual();
        }

        public void _GotStar()
        {
            starCount++;

            _UpdateVisual();
        }

        public void _UpdateVisual()
        {
            turnOnOnComplete.SetActive(collectableCount == maxCollectable);
            allStars.SetActive(starCount == maxStar);

            //for (int i = 0; i < collectableVisual.Length; i++)
            //{
            //    collectableVisual[i].SetActive(i < collectableCount);
            //}
        }
    }
}