using Leap.Unity;
using Leap.Unity.Attributes;
using System.Collections.Generic;
using UnityEngine;

public class SingleCameraJointConfidences : MonoBehaviour
{

    List<JointOcclusion> jointOcclusions;

    /// <summary>
    /// A list of providers that are used for aggregation
    /// </summary>
    [Tooltip("Scene provider for confidence visualization")]
    [EditTimeOnly]
    public LeapProvider[] providers;

    /// <summary>
    /// A list of providers that are used for aggregation
    /// </summary>
    [Tooltip("Capsule hands used to visualize confidence")]
    [EditTimeOnly]
    public CapsuleHand[] visualCapsuleHands;


    private Camera jointOcclusionCamera;

    /// <summary>
    /// 
    /// </summary>
    [Tooltip("An optional visualizer for showing the joint confidence image")]
    public ConfidenceFromPoseVisualizer confidenceFromPoseVisualizer;

    private void Update()
    {
        SetupJointOcclusion();

        if (jointOcclusionCamera != null && confidenceFromPoseVisualizer!= null)
        {
            confidenceFromPoseVisualizer.UpdateTexture(jointOcclusionCamera.targetTexture);
        }
    }

    private void FixedUpdate()
    {
        SetupJointOcclusion();
    }

    /// <summary>
    /// create joint occlusion gameobjects if they are not there yet and update the position of all joint occlusion gameobjects that are attached to a xr service provider
    /// </summary>
    void SetupJointOcclusion()
    {
        if (jointOcclusions == null)
        {
            jointOcclusions = new List<JointOcclusion>();

            foreach (LeapProvider provider in providers)
            {
                JointOcclusion jointOcclusion = provider.gameObject.GetComponentInChildren<JointOcclusion>();

                if (jointOcclusion == null)
                {
                    jointOcclusion = GameObject.Instantiate(Resources.Load<GameObject>("JointOcclusionPrefab"), provider.transform).GetComponent<JointOcclusion>();

                    foreach (CapsuleHand jointOcclusionHand in visualCapsuleHands)
                    {
                        jointOcclusionHand.leapProvider = provider;

                        VisualiseSingleCameraJointConfidence visualizer = (VisualiseSingleCameraJointConfidence)jointOcclusionHand.gameObject.AddComponent<VisualiseSingleCameraJointConfidence>();
                        visualizer.occlusionProvider = jointOcclusion;
                        visualizer.serviceProvider = provider;
                        visualizer.hand = jointOcclusionHand;
                    } 
                }

                jointOcclusionCamera = jointOcclusion.GetComponent<Camera>();
        
                jointOcclusions.Add(jointOcclusion);
            }

            foreach (JointOcclusion jointOcclusion in jointOcclusions)
            {
                jointOcclusion.Setup();
            }
        }

        // if any providers are xr providers, update their jointOcclusions position and rotation
        for (int i = 0; i < jointOcclusions.Count; i++)
        {
            LeapXRServiceProvider xrProvider = providers[i] as LeapXRServiceProvider;
            if (xrProvider != null)
            {
                Transform deviceOrigin = GetDeviceOrigin(providers[i]);

                jointOcclusions[i].transform.SetPose(deviceOrigin.GetPose());
                jointOcclusions[i].transform.Rotate(new Vector3(-90, 0, 180));
            }
        }
    }

    /// <summary>
    /// returns the transform of the device origin of the device corresponding to the given provider.
    /// If it is a desktop or screentop provider, this is simply provider.transform.
    /// If it is an XR provider, the main camera's transform is taken into account as well as the manual head offset values of the device
    /// </summary>
    public static Transform GetDeviceOrigin(LeapProvider provider)
    {
        Transform deviceOrigin = provider.transform;

        LeapXRServiceProvider xrProvider = provider as LeapXRServiceProvider;
        if (xrProvider != null)
        {
            deviceOrigin = xrProvider.mainCamera.transform;

            // xrProvider.deviceOrigin is set if camera follows a transform
            if (xrProvider.deviceOffsetMode == LeapXRServiceProvider.DeviceOffsetMode.Transform && xrProvider.deviceOrigin != null)
            {
                deviceOrigin = xrProvider.deviceOrigin;
            }
            else if (xrProvider.deviceOffsetMode != LeapXRServiceProvider.DeviceOffsetMode.Transform)
            {
                deviceOrigin.Translate(new Vector3(0, xrProvider.deviceOffsetYAxis, xrProvider.deviceOffsetZAxis));
                deviceOrigin.Rotate(new Vector3(-90 - xrProvider.deviceTiltXAxis, 180, 0));
            }
        }

        return deviceOrigin;
    }
}