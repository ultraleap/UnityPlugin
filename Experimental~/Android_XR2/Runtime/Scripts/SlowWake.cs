using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

#if SVR

namespace Ultraleap
{
    public class SlowWake : MonoBehaviour
    {
        public GameObject[] toEnable; //List in chronological order (E.G. Leap, scene, etc)

        public void Awake()
        {
            Debug.Log($"SlowWake : Awake");
        }

        public void Start()
        {
            Debug.Log($"SlowWake : Start");
            for (int i = 0; i < toEnable.Length; i++) toEnable[i].SetActive(false);
            StartCoroutine(SlowWakeCoroutine());
        }

        public void OnEnable()
        {
            Debug.Log($"SlowWake : OnEnable");
        }

        public void OnDisable()
        {
            Debug.Log($"SlowWake : OnDisable");
        }

        private IEnumerator SlowWakeCoroutine()
        {
            var svrManager = SvrManager.Instance;
            Debug.Log("SlowWake : LeapC Unity hand tracking delayed initialisation, have SvrManager.Instance");
            yield return new WaitUntil(() => svrManager.Initialized == true);
            Debug.Log("SlowWake : LeapC, svrManager.Initialized == true");
            yield return new WaitForSeconds(2.0f);
            Debug.Log("SlowWake : LeapC, post 2 second sleep. About to activate Unity hand controller object");
            for (int i = 0; i < toEnable.Length; i++)
            {
                toEnable[i].SetActive(true);
                yield return new WaitForSeconds(0.1f);
            }
            Debug.Log("SlowWake : LeapC, slow wake start up sequence completed");
        }
    }
}
#endif
