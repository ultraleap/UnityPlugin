using UnityEngine;

namespace Leap.Unity
{
    static public class MainCameraProvider
    {
        static private Camera _mainCamera;
        static public Camera mainCamera
        {
            get
            {
                if (_mainCamera == null)
                {
                    var camera = Camera.main;
                    if (camera == null)
                    {
                        camera = GameObject.FindObjectOfType<Camera>();
                    }

                    _mainCamera = camera;
                }

                if (_mainCamera == null)
                {
                    Debug.LogError("Please ensure a camera exists in the scene to use the MainCameraProvider");
                }
                return _mainCamera;
            }
            set
            {
                _mainCamera = value;
            }
        }
    }
}