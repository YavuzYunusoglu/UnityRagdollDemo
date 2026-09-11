using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LB3D
{

    public class ShuffleDisplay : MonoBehaviour
    {
        public List<GameObject> items = new List<GameObject>();

        private int currentIndex = 0;
        public bool doAutoShuffle = false;
        public float suffleInterval = 1;     
        private float elapsed = 0;
        void Start()
        {
            Debug.LogWarning("Push space bar to cycle through character versions.");

            foreach (Transform item in transform)
            {
                items.Add(item.gameObject);
            }
            DeactivateAll();
            items[0].SetActive(true);
        }

        

        // Update is called once per frame
        void Update()
        {
            if (Input.GetKeyDown(KeyCode.Space))
            {
                Shuffle();
            }

            if (!doAutoShuffle) return;

            elapsed += Time.deltaTime;
            if (elapsed > suffleInterval) {
                Shuffle();
                elapsed = 0;
            }
        }

        public void Shuffle()
        {
            DeactivateAll();

            currentIndex++;
            if (currentIndex > items.Count - 1) {
                currentIndex = 0;
            }
            items[currentIndex].SetActive(true);
        }

        public void DeactivateAll() {
            foreach (GameObject item in items)
            {
                item.SetActive(false);
            }
        }
    }
}
