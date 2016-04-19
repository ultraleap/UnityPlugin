using UnityEngine;
using System.Collections;

public class LookAtCamera : MonoBehaviour {
    public GameObject Menu;
	void LateUpdate () {
        transform.rotation = Quaternion.LookRotation(transform.parent.right, Camera.main.transform.position - transform.position);

        if (transform.localEulerAngles.z < 320f && transform.localEulerAngles.z > 250f)
        {
            Menu.SetActive(true);
        }
        else
        {
            Menu.SetActive(false);
        }
	}
}
