using UnityEngine;
using System.Collections;

public class SpawnBalls : MonoBehaviour {
  public GameObject BallPrefab;
  public float delayInterval = .15f; // seconds
  public int BallLimit = 200;

  private IEnumerator _spawnCoroutine;

  void Awake () {
    _spawnCoroutine = AddBallWithDelay(BallPrefab);
  }

  public void StartBalls(){
    StopCoroutine(_spawnCoroutine);
    StartCoroutine(_spawnCoroutine);
  }

  public void StopBalls(){
    StopCoroutine(_spawnCoroutine);
  }

  private IEnumerator AddBallWithDelay (GameObject prefab) {
    while (true) {
      addBall(prefab);
      yield return new WaitForSeconds(delayInterval);
    }
  }

  private void addBall (GameObject prefab) {
    if (transform.childCount > BallLimit) removeBalls(BallLimit / 10);
    GameObject go = GameObject.Instantiate(prefab);
    go.transform.parent = transform;
    go.transform.localPosition = Vector3.zero;
    Rigidbody rb = go.GetComponent<Rigidbody>();
    rb.AddForce(Random.value * 3, -Random.value * 13, Random.value * 3, ForceMode.Impulse);
  }

  private void removeBalls (int count) {
    if (count > transform.childCount) count = transform.childCount;
    for (int b = 0; b < count; b++) {
      Destroy(transform.GetChild(b).gameObject);
    }
  }
}
