using Leap;
using Leap.Unity;
using System;
using System.Collections;
using UnityEngine;
using UnityEngine.XR.Management;
using UnityEngine.XR.OpenXR;
using Ultraleap.Tracking.OpenXR;


public class XRLeapProviderManager : PostProcessProvider
{
    [SerializeField] private OpenXRLeapProvider openXRLeapProvider;
    [SerializeField] private LeapXRServiceProvider leapXRServiceProvider;

    public Action<LeapProvider> OnProviderSet;

    private LeapProvider _leapProvider = null;
    public LeapProvider LeapProvider
    {
        get
        {
            return (_leapProvider == null) ? leapXRServiceProvider : _leapProvider;
        }
    }

    [Tooltip("Forces the use of the non-OpenXR provider, using LeapC hand tracking")]
    public bool forceLeapService;

    private IEnumerator Start()
    {
        while (XRGeneralSettings.Instance == null) yield return new WaitForEndOfFrame();

        if (XRGeneralSettings.Instance.Manager.activeLoader.name == "Open XR Loader" &&
            OpenXRSettings.Instance.GetFeature<HandTrackingFeature>() != null &&
            OpenXRSettings.Instance.GetFeature<HandTrackingFeature>().enabled &&
            !forceLeapService)
        {
            Debug.Log("Using OpenXR for Hand Tracking");
            _leapProvider = openXRLeapProvider;
            Destroy(leapXRServiceProvider.gameObject);
        }
        else
        {
            Debug.Log("Using LeapService for Hand Tracking");
            _leapProvider = leapXRServiceProvider;
            Destroy(openXRLeapProvider.gameObject);
        }


        inputLeapProvider = LeapProvider;
        OnProviderSet?.Invoke(inputLeapProvider);
    }

    public override void ProcessFrame(ref Frame inputFrame)
    {
    }
}
