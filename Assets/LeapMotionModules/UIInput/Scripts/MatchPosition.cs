using UnityEngine;
using System.Collections;

public class MatchPosition : MonoBehaviour {
    public Transform toMatch;
	void LateUpdate () {
        transform.position = toMatch.position;
	}
}
