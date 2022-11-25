using UnityEngine;

[RequireComponent(typeof(LineRenderer))]
public class LineBetweenTransforms : MonoBehaviour {

	[SerializeField] private Transform _pointA, _pointB;
	private LineRenderer _line;

	void Start () 
	{
		_line = GetComponent<LineRenderer>();
	}

	void LateUpdate () 
	{
		if (_line.gameObject.activeInHierarchy)
		{
			_line.SetPosition(0, _pointA.position);
			_line.SetPosition(_line.positionCount - 1, _pointB.position);
		}
	}
}
