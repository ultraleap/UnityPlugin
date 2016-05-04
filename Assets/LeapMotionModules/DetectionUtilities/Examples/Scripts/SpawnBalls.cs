using UnityEngine;
using System.Collections;

public class SpawnBalls : MonoBehaviour {
  public GameObject RedBallPrefab;
  public GameObject GreenBallPrefab;
  public float delayInterval = .15f; // seconds
  public int BallLimit = 200;

  public void StartRedBalls(){
    StartCoroutine(addRedBallsWithDelay());
  }

  public void StopRedBalls(){
    StopAllCoroutines();
     StopCoroutine(addRedBallsWithDelay());
  }

  IEnumerator addRedBallsWithDelay(){
    while (true) {
      if (transform.childCount > BallLimit) removeBalls(BallLimit / 10);
      GameObject go = GameObject.Instantiate(RedBallPrefab);
      go.transform.parent = transform;
      yield return new WaitForSeconds(delayInterval);
    }
  }

  public void StartGreenBalls(){
    StartCoroutine(addGreenBallsWithDelay());
  }

  public void StopGreenBalls(){
    StopCoroutine(addGreenBallsWithDelay());
  }

  IEnumerator addGreenBallsWithDelay(){
    while (true) {
      if (transform.childCount > BallLimit) removeBalls(BallLimit / 10);
      GameObject go = GameObject.Instantiate(GreenBallPrefab);
      go.transform.parent = transform;
      yield return new WaitForSeconds(delayInterval);
    }
  }

  private void removeBalls (int count) {
    if (count > transform.childCount) count = transform.childCount;
    for (int b = 0; b < count; b++) {
      Destroy(transform.GetChild(b).gameObject);
    }
  }
}
