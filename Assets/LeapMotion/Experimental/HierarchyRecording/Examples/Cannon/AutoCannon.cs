/******************************************************************************
 * Copyright (C) Leap Motion, Inc. 2011-2018.                                 *
 * Leap Motion proprietary and confidential.                                  *
 *                                                                            *
 * Use subject to the terms of the Leap Motion SDK Agreement available at     *
 * https://developer.leapmotion.com/sdk_agreement, or another agreement       *
 * between Leap Motion and you, your company or other organization.           *
 ******************************************************************************/

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AutoCannon : MonoBehaviour {

  public GameObject prefab;

  public Transform spawnParent;
  public Transform spawnLocation;
  public float spawnSpeed;
  public int spawnPeriod = 8;
  public float lifeTime = 1;

  public float rotationSpeed;
  public float rotationFrequency;

  public Queue<GameObject> pool = new Queue<GameObject>();

  void Update() {
    transform.Rotate(0, Time.deltaTime * rotationSpeed * (Mathf.PerlinNoise(rotationFrequency * Time.time, rotationFrequency * Time.time * 2) - 0.5f), 0);

    if (Time.frameCount % spawnPeriod == 0) {
      StartCoroutine(spawnCoroutine());
    }
  }

  IEnumerator spawnCoroutine() {
    GameObject obj;
    if (pool.Count > 0) {
      obj = pool.Dequeue();
    } else {
      obj = Instantiate(prefab);
      obj.transform.SetParent(spawnParent);
    }

    obj.transform.position = spawnLocation.position;
    obj.transform.rotation = spawnLocation.rotation;
    obj.GetComponent<Rigidbody>().velocity = spawnLocation.forward.normalized * spawnSpeed;
    obj.SetActive(true);

    yield return new WaitForSeconds(lifeTime);

    obj.SetActive(false);
    yield return null;
    pool.Enqueue(obj);
  }
}
