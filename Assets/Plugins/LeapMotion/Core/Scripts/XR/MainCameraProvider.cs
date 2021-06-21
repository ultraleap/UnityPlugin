using Assets.Plugins.UnityModules.Assets.Plugins.LeapMotion.Core.Scripts.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Assets.Plugins.UnityModules.Assets.Plugins.LeapMotion.Core.Scripts.XR
{
    /// <summary>
    /// Singleton class to provide access to the main camera.
    /// Only required because the XR2 Unity plugin does not support Camera.main
    /// </summary>
    public class MainCameraProvider : MonoSingleton<MainCameraProvider> {

      [SerializeField]
      [HideInInspector]
      private Camera _mainCamera;
      public Camera mainCamera {
         get {
           if (_mainCamera == null) {
             return Camera.main;
           } else {
             return _mainCamera;
           }
         }

         set { _mainCamera = value; }
        }
    }
}
