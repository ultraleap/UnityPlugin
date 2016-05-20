using UnityEngine;
using UnityEngine.VR;
using System.Collections;

public class ResetOnLoad : MonoBehaviour {
    public Transform HeadTrackingNode;
    private Vector3 offset;
    void Awake() {
        offset = transform.position - HeadTrackingNode.transform.position;
    }

	void Start () {
        transform.position = new Vector3(transform.position.x, HeadTrackingNode.transform.position.y + offset.y, transform.position.z);
        InputTracking.Recenter();
    }
}
