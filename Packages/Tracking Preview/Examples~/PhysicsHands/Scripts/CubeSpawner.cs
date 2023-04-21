using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Leap.Unity.Interaction.PhysicsHands.Example
{
    public class CubeSpawner : MonoBehaviour
    {
        private List<GameObject> _cubes = new List<GameObject>();

        public void SpawnCube()
        {
            GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube.transform.SetParent(transform);
            cube.transform.localPosition = Vector3.zero;
            cube.transform.localScale = Vector3.one * Random.Range(0.02f, 0.1f);
            cube.transform.rotation = Quaternion.Euler(Random.Range(0, 180f), Random.Range(0, 180f), Random.Range(0, 180f));
            cube.AddComponent<Rigidbody>();
            _cubes.Add(cube);
        }

        public void DestroyCubes()
        {
            foreach (var cube in _cubes)
            {
                Destroy(cube);
            }
            _cubes.Clear();
        }
    }
}