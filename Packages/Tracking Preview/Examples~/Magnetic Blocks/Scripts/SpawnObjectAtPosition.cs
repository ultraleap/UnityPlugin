using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Leap.Unity.Examples.Experiments
{
    public class SpawnObjectAtPosition : MonoBehaviour
    {
        public Transform objectToSpawn;
        public Transform spawnPoint;

        public void SpawnObject()
        {
            Instantiate(objectToSpawn, spawnPoint.position, Quaternion.identity);
        }
    }
}