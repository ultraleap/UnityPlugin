using UnityEngine;
using System.Collections;

public class LookAtCamera : MonoBehaviour {
	void LateUpdate () {
        transform.rotation = Quaternion.LookRotation(transform.parent.right, Camera.main.transform.position - transform.position);
	}
}
