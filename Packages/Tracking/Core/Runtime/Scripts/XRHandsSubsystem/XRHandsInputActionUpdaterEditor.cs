using Leap;
using Leap.InputActions;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Hands;

/// <summary>
/// Enables settings related to the XRHandsInputActionUpdater to be viewed in the Inspector and modified
/// </summary>
public class XRHandsInputActionUpdaterEditor : MonoBehaviour
{
    protected void Update()
    {
        // Force our property logic to be used
        this.WristShoulderBlendAmount = wristShoulderBlendAmount;
        this.PinchWristOffset = pinchWristOffset;
        this.NeckOffset = neckOffset;   
    }

    protected void Start()
    {
        TransformHelper = new GameObject("WristShoulderFarFieldRay_TransformHelper").transform;
        TransformHelper.SetParent(transform);
    }

    /// <summary>
    /// The wrist shoulder lerp amount is only used when the rayOrigin is wristShoulderLerp. 
    /// It specifies how much the wrist vs the shoulder is used as a ray origin.
    /// At 0, only the wrist position and rotation are taken into account.
    /// At 1, only the shoulder position and rotation are taken into account.
    /// For a more responsive far field ray, blend towards the wrist. For a more stable far field ray,
    /// blend towards the shoulder. Keep the value central for a blend between the two.
    /// </summary>
    [Tooltip("Our far field ray is a direction from a wristShoulder blend position, through a stable pinch position.\n" +
        "WristShoulderBlendAmount determines the wristShoulder blend position.\n" +
        " - At 0, only an wrist position is taken into account.\n" +
        " - At 1, only the shoulder position is taken into account.\n" +
        " - For a more responsive far field ray, blend towards the wrist.\n" +
        " - For a more stable far field ray, blend towards the shoulder.\n" +
        " - Keep the value central for a blend between the two.")]
    [Range(0f, 1)]
    [SerializeField]
    private float wristShoulderBlendAmount = 0.532f;
    
    public float WristShoulderBlendAmount
    {
        get
        {
            return wristShoulderBlendAmount;
        }

        set
        {
            if (XRHandsInputActionUpdater.wristShoulderBlendAmount != value ||
                WristShoulderBlendAmount != value)
            {
                wristShoulderBlendAmount = value;
                XRHandsInputActionUpdater.wristShoulderBlendAmount = value;
            }
        } 
    }

    [SerializeField]
    private Vector3 pinchWristOffset = new Vector3(0.0425f, 0.0652f, 0.0f);

    public Vector3 PinchWristOffset
    {
        get
        {
            return pinchWristOffset;    
        }

        set
        {
            if (pinchWristOffset != value ||
                XRHandsInputActionUpdater.pinchWristOffset != value)
            {
                pinchWristOffset = value;
                XRHandsInputActionUpdater.pinchWristOffset = value;
            }
        }
    }

    [SerializeField]
    [Range(0f, -1.0f)]
    private float neckOffset = -0.1f;

    public float NeckOffset
    {
        get
        {
            return neckOffset;
        }

        set
        {
            if (neckOffset != value ||
                XRHandsInputActionUpdater.neckOffset != value)
            {
                neckOffset = value;
                XRHandsInputActionUpdater.neckOffset = value;
            }
        }
    }

    private Transform transformHelper;
    public Transform TransformHelper
    {
        get
        {
            return XRHandsInputActionUpdater.transformHelper;
        }

        set
        {
            XRHandsInputActionUpdater.transformHelper = value;
        }
    }
}
