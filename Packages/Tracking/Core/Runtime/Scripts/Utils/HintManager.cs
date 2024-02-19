using Leap;
using Leap.Unity;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public static class HintManager
{
    static List<string> currentHints = new List<string>();

    static Controller _leapController;
    static Controller LeapController
    { 
        get
        {
            if(_leapController == null) // Find any existing controller
            {
                LeapServiceProvider provider = GameObject.FindObjectOfType<LeapServiceProvider>(true);

                if(provider != null)
                {
                    _leapController = provider.GetLeapController();
                }
                else // No leapserviceprovider, we should make a new controller ourselves
                {
                    _leapController = new Controller();
                }
            }
            return _leapController;
        }
        set
        {
            _leapController = value;
        }
    }

    /// <summary>
    /// Capture the values in UltraleapSettings and set up the setting of those hints when the first device is discovered
    /// </summary>
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void StartupHints()
    {
        currentHints = UltraleapSettings.Instance.startupHints.ToList();

        LeapController.Device -= NewLeapDevice;
        LeapController.Device += NewLeapDevice;
    }

    /// <summary>
    /// Only used for initial device detection, then will send Startup Hints for the first device
    /// </summary>
    private static void NewLeapDevice(object sender, DeviceEventArgs e)
    {
        LeapController.Device -= NewLeapDevice;
        SetHints(currentHints.ToArray());
    }

    /// <summary>
    /// Remove a hint from the existing hints and then send them all to the Service for the current device of the current controller
    /// </summary>
    public static void RemoveHint(string hint)
    {
        if (currentHints.Remove(hint))
        {
            SetHints(currentHints.ToArray());
        }
        else
        {
            Debug.Log("Hand Tracking Hint: " + hint + " was not previously set. No hints were changed.");
        }
    }

    /// <summary>
    /// Add a hint to the existing hints and then send them all to the Service for the current device of the current controller
    /// </summary>
    public static void AddHint(string hint)
    {
        if (!currentHints.Contains(hint))
        {
            currentHints.Add(hint);
        }

        SetHints(currentHints.ToArray());
    }

    /// <summary>
    /// Send a specific set of hints, if this does not include previously set ones, they will be cleared
    /// </summary>
    /// <param name="hints">The hints you wish to send</param>
    /// <param name="device">An optional specific Device, otherwise the first found Device of the first Controller will be used</param>
    public static void SetHints(string[] hints, Device device = null)
    {
        LeapController.SetHints(hints, device);

        string logString = "Hand Tracking Hints have been requested:";

        foreach (string hint in hints)
        {
            logString += " " + hint + ",";
        }

        logString.Remove(logString.Length - 1);

        Debug.Log(logString);
    }
}