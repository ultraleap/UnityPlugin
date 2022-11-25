using Leap.Unity;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FaceCamera : MonoBehaviour {

	[SerializeField] private bool _lockZRotation = true;

	private float _initialZRot;

	void Start () 
	{
		_initialZRot = transform.localEulerAngles.z;
	}
	
	// Update is called once per frame
	void Update () 
	{
		transform.rotation = Quaternion.LookRotation(-Camera.main.transform.position + transform.position);

		if (_lockZRotation)
		{
			transform.localEulerAngles = new Vector3(transform.localEulerAngles.x, transform.localEulerAngles.y, _initialZRot);
		}
	}
}
