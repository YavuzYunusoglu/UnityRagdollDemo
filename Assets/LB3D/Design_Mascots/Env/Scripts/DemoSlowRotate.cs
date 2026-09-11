using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace LB3D
{
    public class DemoSlowRotate : MonoBehaviour
    {

        public float speed = 1;

        bool doRotate = false;
        public GameObject subject;

        private void Start()
        {
            print("Press [Spacebar] to start turntable...");
        }

        void Update()
        {
            if (Input.GetKeyDown(KeyCode.Space))
            {
                doRotate = !doRotate;
            }

            if (doRotate)
                subject.transform.Rotate(Vector3.up * Time.deltaTime * speed);
        }
    }
}