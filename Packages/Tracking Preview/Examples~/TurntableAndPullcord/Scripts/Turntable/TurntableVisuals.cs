using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TurntableVisuals : MonoBehaviour
{
    [SerializeField] private LineRenderer _circle;
    [SerializeField] private GameObject _dashesParent;
    [SerializeField] private GameObject _dashTemplate;

    [SerializeField] private int _circleSegments = 36;

    private LineRenderer[] _dashes;

    private float _lastHeight, _lastLowerHeight, _lastRadius, _lastLowerRadius;

    private void OnValidate()
    {
        if (!Application.isPlaying)
        {
            UpdateVisuals(_lastHeight, _lastLowerHeight, _lastRadius, _lastLowerRadius);
        }
    }

    /// <summary>
    /// Updates the circle and dashes visual of the turntable
    /// </summary>
    /// <param name="height">the local height of the upper edge</param>
    /// <param name="lowerHeight">the local height of the lower edge</param>
    /// <param name="radius">the upper radius</param>
    /// <param name="lowerRadius">the lower radius</param>
    public void UpdateVisuals(float height, float lowerHeight, float radius, float lowerRadius)
    {
        _lastHeight = height;
        _lastLowerHeight = lowerHeight;
        _lastRadius = radius;
        _lastLowerRadius = lowerRadius;

        _dashes = _dashesParent.GetComponentsInChildren<LineRenderer>();
        if (_dashes == null || _dashes.Length != _circleSegments)
        {
            UpdateDashes();
        }

        Vector3 dashStartPos = new Vector3(radius, height, 0f);
        Vector3 dashEndPos = new Vector3(lowerRadius, lowerHeight, 0f);

        for (int i = 0; i < _circleSegments; i++)
        {
            _dashes[i].SetPosition(0, dashStartPos + 0.1f * (dashEndPos - dashStartPos));
            _dashes[i].SetPosition(1, dashEndPos);
        }


        _circle.positionCount = _circleSegments;
        _circle.useWorldSpace = false;
        _circle.loop = true;

        float x;
        float y = height;
        float z;

        float angle = 0f;

        for (int i = 0; i < _circleSegments; i++)
        {
            x = Mathf.Sin(Mathf.Deg2Rad * angle) * radius;
            z = Mathf.Cos(Mathf.Deg2Rad * angle) * radius;

            _circle.SetPosition(i, new Vector3(x, y, z));

            _dashes[i].transform.localRotation = Quaternion.Euler(0, angle, 0);

            angle += (360f / _circleSegments);
        }
    }

    private void UpdateDashes()
    {
        // get all current dashes and destroy some if there are too many, or instantiate new ones if there are too few
        
        if (_dashes.Length > _circleSegments)
        {
            for (int i = _dashes.Length - 1; i >= _circleSegments; i--)
            {
                StartCoroutine(Destroy(_dashes[i].gameObject));
            }
        }
        else if (_dashes.Length < _circleSegments)
        {
            var tempDashes = new LineRenderer[_circleSegments];
            for (int i = 0; i < _circleSegments; i++)
            {
                if (i < _dashes.Length) tempDashes[i] = _dashes[i];
                else tempDashes[i] = Instantiate(_dashTemplate, _dashesParent.transform).GetComponent<LineRenderer>();
            }
            _dashes = tempDashes;
        }
    }

    IEnumerator Destroy(GameObject go)
    {
        yield return null;
        DestroyImmediate(go);
    }
}
