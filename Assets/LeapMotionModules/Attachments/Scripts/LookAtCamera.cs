using UnityEngine;
using System.Collections;

public class LookAtCamera : MonoBehaviour {
    public GameObject Menu;
    public Vector3 Zed = Vector3.zero;
  public bool IsActive = false;
    public Quaternion swing = Quaternion.identity;
    public Quaternion twist = Quaternion.identity;
    public Vector3 axis = Vector3.zero;
    public Vector3 angles = Vector3.zero; 
  void Start(){
    Quaternion quat = Quaternion.Euler(new Vector3(45, 45, 45));
    Vector3 ax;
    float angle;
    swing_twist_decomposition(quat, Vector3.right);
    twist.ToAngleAxis(out angle, out ax);
    Debug.Log("1. " + angle);
    swing_twist_decomposition(quat, Vector3.up);
    twist.ToAngleAxis(out angle, out ax);
    Debug.Log("2. " + angle);
    swing_twist_decomposition(quat, Vector3.forward);
    twist.ToAngleAxis(out angle, out ax);
    Debug.Log("3. " + angle);
  }
  void LateUpdate () {
    Vector3 ax;
    float ang;
    Quaternion toFaceCamera = Quaternion.LookRotation(transform.position - Camera.main.transform.position);
    swing_twist_decomposition(toFaceCamera, transform.up);
    swing.ToAngleAxis(out ang, out ax);
    angles[0] = ang;
    swing_twist_decomposition(toFaceCamera, transform.right);
    swing.ToAngleAxis(out ang, out ax);
    angles[1] = ang;
    swing_twist_decomposition(toFaceCamera, transform.forward);
    swing.ToAngleAxis(out ang, out ax);
    angles[2] = ang;

    
    transform.localRotation = swing;
        Zed = transform.localEulerAngles;
        if (transform.localEulerAngles.z < 320f && transform.localEulerAngles.z > 250f)
        {
           // Menu.SetActive(true);
            IsActive = true;
        }
        else
        {
           // Menu.SetActive(false);
            IsActive = false;
        }
  }

 public void swing_twist_decomposition(Quaternion rotation,
                                       Vector3 direction//,
                                       //Quaternion swing,
                                       //Quaternion twist,
                                       //Vector3 axis
                                      )
  {
      axis = new Vector3( rotation.x, rotation.y, rotation.z ); // rotation axis
      Vector3 p = Vector3.Dot(direction, axis) * direction; //direction must be normalized
      float norm = Mathf.Sqrt(p.sqrMagnitude + rotation.w * rotation.w);
      twist.Set( p.x/norm, p.y/norm, p.z/norm, rotation.w/norm );
      swing = rotation * new Quaternion(-twist.x, -twist.y, -twist.z, twist.w);
  }
}