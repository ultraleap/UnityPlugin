using UnityEngine;
using System.Collections;

public class LookAtCamera : MonoBehaviour {
    public GameObject Menu;
    public Vector3 Zed = Vector3.zero;
  public bool IsActive = false;

  void LateUpdate () {
        transform.rotation = Quaternion.LookRotation(transform.parent.right, Camera.main.transform.position - transform.position);
        Zed = transform.localEulerAngles;
        if (transform.localEulerAngles.z < 320f && transform.localEulerAngles.z > 250f)
        {
            Menu.SetActive(true);
            IsActive = true;
        }
        else
        {
            Menu.SetActive(false);
            IsActive = false;
        }
  }
}