using System.Collections;
using System.Collections.Generic;
using UnityEngine;

#if UNITY_ANDROID
public class ServiceCallbacks : AndroidJavaProxy
{
    public ServiceCallbacks() :
        base("com.ultraleap.tracking.service_binder.ServiceBinder$Callbacks")
    { }
    public void onBound()
    {
        Debug.Log("ServiceCallbacks.onBound");
    }

    public void onUnbound()
    {
        Debug.Log("ServiceCallbacks.onUnbound");
        // Ensure we disconnect if the service becomes unbound to prevent
        // a TIME_WAIT state on the Service TCP socket/address
        foreach (var provider in GameObject.FindObjectsOfType<Leap.Unity.LeapServiceProvider>())
        {
            provider.destroyController();
        }
    }
}
#endif