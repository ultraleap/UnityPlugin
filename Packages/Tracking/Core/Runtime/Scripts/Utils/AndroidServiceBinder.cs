#if UNITY_ANDROID
namespace Ultraleap
{
    using System;
    using UnityEngine;

    public static class AndroidServiceBinder
    {
        public static bool IsBound { get; private set; }

        private static AndroidJavaObject _serviceBinder;
        private static AndroidJavaClass unityPlayer;
        private static AndroidJavaObject activity;
        private static AndroidJavaObject context;
        private static ServiceCallbacks serviceCallbacks;

        public static AndroidJavaObject ServiceBinder => _serviceBinder;

        public static bool Bind()
        {
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
            bool success;
            try
            {
                _serviceBinder = null;

                //Get activity and context
                if (unityPlayer == null)
                {
                    Debug.Log("CreateAndroidBinding - Getting activity and context");
                    unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
                    activity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
                    context = activity.Call<AndroidJavaObject>("getApplicationContext");
                    serviceCallbacks = new ServiceCallbacks();
                }

                //Create a new service binding
                Debug.Log("CreateAndroidBinding - Creating a new service binder");
                _serviceBinder = new AndroidJavaObject("com.ultraleap.tracking.service_binder.ServiceBinder", context, serviceCallbacks);
                success = _serviceBinder.Call<bool>("bind");
                if (success)
                {
                    Debug.Log("CreateAndroidBinding - Binding of service binder complete");
                }
                else
                {
                    Debug.LogWarning("CreateAndroidBinding - service binder bind call failed");
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning("CreateAndroidBinding - Failed to bind service: " + e.Message);
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