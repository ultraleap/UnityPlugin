using UnityEngine;
using System.Collections;

public class SpawnBalls : MonoBehaviour {
  public GameObject RedBallPrefab;
  public GameObject GreenBallPrefab;
  public float delayInterval = .15f; // seconds
  public int BallLimit = 200;

  private IEnumerator _redballCoroutine;
  private IEnumerator _greenballCoroutine;

  void Awake () {
    _redballCoroutine = addRedBallsWithDelay();
    _greenballCoroutine = addGreenBallsWithDelay();
  }

  public void StartRedBalls(){
    StopCoroutine(_redballCoroutine);
    StartCoroutine(_redballCoroutine);
  }

  public void StopRedBalls(){
    StopCoroutine(_redballCoroutine);
  }

  IEnumerator addRedBallsWithDelay(){
    while (true) {
      addBall(RedBallPrefab);
      yield return new WaitForSeconds(delayInterval);
    }
  }

  public void StartGreenBalls(){
    StopCoroutine(_greenballCoroutine);
    StartCoroutine(_greenballCoroutine);
  }

  public void StopGreenBalls(){
    StopCoroutine(_greenballCoroutine);
  }

  IEnumerator addGreenBallsWithDelay(){
    while (true) {
      addBall(GreenBallPrefab);
      yield return new WaitForSeconds(delayInterval);
    }
  }

  private void addBall (GameObject prefab) {
    if (transform.childCount > BallLimit) removeBalls(BallLimit / 10);
    GameObject go = GameObject.Instantiate(prefab);
    go.transform.parent = transform;
    Rigidbody rb = go.GetComponent<Rigidbody>();
    rb.AddForce(Random.value, Random.value, Random.value);
  }

  private void removeBalls (int count) {
    if (count > transform.childCount) count = transform.childCount;
    for (int b = 0; b < count; b++) {
      Destroy(transform.GetChild(b).gameObject);
    }
  }
}
