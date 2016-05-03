using UnityEngine;
using System.Collections;

public class SpawnBalls : MonoBehaviour {
  public GameObject RedBallPrefab;
  public GameObject GreenBallPrefab;
  public float delayInterval = .05f; // seconds

  public void StartRedBalls(){
    StartCoroutine(addRedBallsWithDelay());
  }

  public void StopRedBalls(){
     StartCoroutine(addRedBallsWithDelay());
  }

  IEnumerator addRedBallsWithDelay(){
    GameObject go = GameObject.Instantiate(RedBallPrefab);
    go.transform.parent = transform;

    yield return  new WaitForSeconds(delayInterval);
  }

  public void StartGreenBalls(){
    StartCoroutine(addGreenBallsWithDelay());
  }

  public void StopGreenBalls(){
     StartCoroutine(addGreenBallsWithDelay());
  }

  IEnumerator addGreenBallsWithDelay(){
    GameObject go = GameObject.Instantiate(GreenBallPrefab);
    go.transform.parent = transform;
    yield return  new WaitForSeconds(delayInterval);
  }
}
