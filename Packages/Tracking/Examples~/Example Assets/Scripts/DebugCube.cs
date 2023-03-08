using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Leap.Unity;

namespace Leap.Unity
{

    public class DebugCube : MonoBehaviour
    {
        // Start is called before the first frame update
        void Start()
        {
            this.GetComponent<MeshRenderer>().material.color = Color.red;
        }
    
        public void OnPoseDetected()
        {
            this.GetComponent<MeshRenderer>().material.color = Color.green;
        }
        public void OnPoseLost()
        {
            this.GetComponent<MeshRenderer>().material.color = Color.red;
        }
    }
}
