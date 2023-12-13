using System.Collections;

using Unity.XR.CoreUtils;
using UnityEditor.XR.LegacyInputHelpers;
using UnityEngine;

public class XRSceneHeadOrigin : MonoBehaviour
{
    [Tooltip("If true, the cameraTransform will try to move to the sceneOrigin during startup")]
    public bool setOnStart = true;

    [Tooltip("Length of time that the cameraTransform will try to move to the sceneOrigin after startup (in Seconds)")]
    public float setOnStartLength = 2f;

    [Tooltip("Maximum distance the cameraTransform can be from the sceneOrigin during setOnStartLength after startup (in Meters)")]
    public float setOnStartDistance = 0.2f;

    [Tooltip("Should the Y rotation be set when starting up too?")]
    public bool includeRotationInStartup = false;

    [Space, Tooltip("A keyboard key to reposition the cameraTransform to the sceneOrigin")]
    public KeyCode resetKey = KeyCode.R;

    [Header("Optional Transforms")]
    [Space, Tooltip("Where the camera should move to by default. If this is not set, the Transform that this XRSceneHeadOrigin component is attached to will be used")]
    public Transform sceneOrigin;

    [Space, Tooltip("Usually an XROrigin or CameraOffset. If this is not set, a suitable Transform will be found")]
    public Transform cameraOffsetOrigin;

    [Tooltip("The main camera being used. If this is not set, a suitable Transform will be found")]
    public Transform cameraTransform;

    void Start()
    {
        if (sceneOrigin == null)
        {
            sceneOrigin = transform;
        }

        // ensure a camera transform is set
        if (cameraTransform == null)
        {
            cameraTransform = Camera.main.transform;
        }

        // Look for an XROrigin
        XROrigin xROrigin = FindAnyObjectByType<XROrigin>();

        if (xROrigin != null)
        {
            // use the XROrigin and exit out
            cameraOffsetOrigin = xROrigin.transform;
        }
        else
        {
            CameraOffset camOffset = FindAnyObjectByType<CameraOffset>();

            if (camOffset != null)
            {
                cameraOffsetOrigin = camOffset.transform;
            }
            else
            {
                if (cameraTransform.parent != null)
                {
                    cameraOffsetOrigin = cameraTransform.parent;
                }
                else
                {
                    cameraOffsetOrigin = cameraTransform;
                }
            }
        }

        if (setOnStart)
        {
            StartCoroutine(SetOriginDuringStartup());
        }
    }

    // Set the origin to this transform if the head becomes more than 20cm 
    IEnumerator SetOriginDuringStartup()
    {
        float time = 0;

        while (time < setOnStartLength)
        {
            yield return null;

            time += Time.deltaTime;

            if (Vector3.Distance(cameraTransform.position, sceneOrigin.position) > setOnStartDistance)
            {
                SetHeadOrigin(includeRotationInStartup);
            }
        }
    }

    public void SetHeadOrigin(bool includeRotation = false)
    {
        SetHeadOrigin(sceneOrigin, includeRotation);
    }

    public void SetHeadOrigin(Transform target, bool includeRotation = false)
    {

        if (includeRotation)
        {
            float rotationY = (target.transform.rotation * Quaternion.Inverse(cameraTransform.transform.rotation) * gameObject.transform.rotation).eulerAngles.y;
            cameraOffsetOrigin.transform.rotation = Quaternion.Euler(cameraOffsetOrigin.transform.eulerAngles.x, cameraOffsetOrigin.transform.eulerAngles.y + rotationY, cameraOffsetOrigin.transform.eulerAngles.z);
        }

        cameraOffsetOrigin.transform.position += target.position - cameraTransform.position;
    }
}