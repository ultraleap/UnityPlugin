using UnityEngine;

namespace Leap.Unity.Preview.InputActions
{
    public class CreateSphere : MonoBehaviour
    {
        public Transform objectToSpawn;
        public Transform spawnPoint;

        public void SpawnObject()
        {
            Instantiate(objectToSpawn, spawnPoint.position, Quaternion.identity);
        }
    }
}