#if UNITY_ANDROID
namespace Leap
{
    using System;
    using UnityEngine;

    public static class AndroidServiceBinder
    {
        public static bool IsBound{ get; private set; }

        static AndroidJavaObject _serviceBinder;
        static AndroidJavaClass unityPlayer;
        static AndroidJavaObject activity;
        static AndroidJavaObject context;
        static ServiceCallbacks serviceCallbacks;

        public static AndroidJavaObject ServiceBinder => _serviceBinder;

        public static bool Bind()
        {
            Debug.Log("Attempting to bind to tracking service using the AndroidServiceBinder");
            bool isBound = _serviceBinder?.Call<bool>("isBound") ?? false;

            if (!isBound)
            {
                isBound = TryBind();

                if (isBound)
                {
                    Application.quitting -= OnApplicationQuitting;
                    Application.quitting += OnApplicationQuitting;
                }
            }

            IsBound = isBound;
            return isBound;
        }

        private static void OnApplicationQuitting()
        {
            Application.quitting -= OnApplicationQuitting;
            Unbind();
        }

        private static bool TryBind()
        {
            bool success = false;
            try
            {
                _serviceBinder = null;

                // Get activity and context
                if (unityPlayer == null)
                {
                    unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
                    activity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
                    context = activity.Call<AndroidJavaObject>("getApplicationContext");
                    serviceCallbacks = new ServiceCallbacks();
                }

                // Create a new service binding
                _serviceBinder = new AndroidJavaObject("com.ultraleap.tracking.service_binder.ServiceBinder", context, serviceCallbacks);

                // Check if there is a service installed before trying to bind to it. If this returns false, there is either nothing to bind to, or the service might be being used in direct mode (part of the running process)
                if (_serviceBinder.Call<bool>("isServiceInstalled", new object[] {context}))
                {
                    success = _serviceBinder.Call<bool>("bind");
                    Debug.Log($"AndroidServiceBinder.TryBind - calling bind returned {success}.");
                }
                else
                {
                    Debug.Log("AndroidServiceBinder.TryBind - System does not have an installed service to bind to. LeapC might be being used in direct mode.");
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning("AndroidServiceBinder.TryBind - Failed to bind service, an exception was caught: " + e.Message);
                _serviceBinder = null;
                success = false;
            }

            return success;
        }

        private static void Unbind()
        {
            if (_serviceBinder != null)
            {
                Debug.Log("UnbindAndroidBinding - Unbinding of service binder complete");
                _serviceBinder.Call("unbind");
                IsBound = false;
            }
        }
    }
}
#endif