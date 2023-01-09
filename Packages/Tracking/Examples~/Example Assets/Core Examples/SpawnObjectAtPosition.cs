using UnityEngine;

public class SpawnObjectAtPosition : MonoBehaviour
{
    public Transform objectToSpawn;
    public Transform spawnPoint;

    public void SpawnObject()
    {
        Instantiate(objectToSpawn, spawnPoint.position, Quaternion.identity);
    }
}