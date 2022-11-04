using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PullUI : MonoBehaviour
{
    [SerializeField] private GameObject _sphere;
    [SerializeField] private GameObject _sphereTarget;
    [SerializeField] private LineRenderer _line;
    [SerializeField] private LineRenderer _lineTarget;

    [SerializeField] private float _pinchOffset = 0.01f;

    public void SetLength(float length, float targetLength, Vector3 direction)
    {
        _line.SetPosition(0, transform.position - new Vector3(0, _pinchOffset, 0));
        _line.SetPosition(1, transform.position + (direction * length) - new Vector3(0, _pinchOffset, 0));

        _sphere.transform.position = transform.position + (direction * length) - new Vector3(0, _pinchOffset, 0);

        _lineTarget.SetPosition(0, transform.position + (direction * length) - new Vector3(0, _pinchOffset, 0));
        _lineTarget.SetPosition(1, transform.position + (direction * targetLength) - new Vector3(0, _pinchOffset, 0));

        _sphereTarget.transform.position = transform.position + (direction * targetLength) - new Vector3(0, _pinchOffset, 0);
    }
}
