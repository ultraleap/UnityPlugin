using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Leap.Unity.Examples {
  
  /// <summary>
  /// This script keeps a GameObject in front of the ship, off to the side a bit. The
  /// skybox cannot provide a frame of reference for the linear motion of the ship, so
  /// the spawned object provides one instead.
  /// 
  /// This script assumes the ship is moving along the world forward axis.
  /// </summary>
  [AddComponentMenu("")]
  public class LinearReferenceSpawner : MonoBehaviour {

    public Spaceship spaceship;
    public GameObject toSpawn;

    private GameObject _spawnedObj;

    void Update() {
      bool justSpawned = false;
      if (_spawnedObj == null) {
        _spawnedObj = GameObject.Instantiate(toSpawn);
        justSpawned = true;
      }

      if (justSpawned
          || (_spawnedObj.transform.position - spaceship.transform.position).z < -1F) {
        Vector3 spawnPos = spaceship.transform.position;
        spawnPos += spaceship.velocity;
        spawnPos += Vector3.left * 1.33F;
        _spawnedObj.transform.position = spawnPos;
      }
    }

  }

}
