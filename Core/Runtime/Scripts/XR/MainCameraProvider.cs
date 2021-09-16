using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Leap.Unity
{
    /// <summary>
    /// Singleton class to provide access to the main camera. Based on public singleton implementations
    /// Only required because the XR2 Unity plugin does not support Camera.main
    /// </summary>
    public class MainCameraProvider : MonoBehaviour {

#region Singleton 
      private static MainCameraProvider m_Instance = null;

      public static MainCameraProvider Instance {
        get {
            if (m_Instance == null) {

              m_Instance = GameObject.FindObjectOfType(typeof(MainCameraProvider)) as MainCameraProvider;

              // Object not found, we create a temporary one
              if (m_Instance == null) {
                isTemporaryInstance = true;
                m_Instance = new GameObject("MainCameraProvider").GetComponent<MainCameraProvider>();

                if (m_Instance == null) {
                  Debug.LogError("Problem creating MainCameraProvider Instance");
                }
              }

              if (!_isInitialized) {
                _isInitialized = true;
                m_Instance.Init();
              }
            }
          return m_Instance;
        }
    }

    public static bool isTemporaryInstance { private set; get; }

    private static bool _isInitialized;

    // If no other monobehaviour has requested the instance in an awake function
    // executing before this one, there is no need to search the object.
    private void Awake() {

      if (m_Instance == null) {
            m_Instance = this as MainCameraProvider;
        }
        else if (m_Instance != this) {
          Debug.LogError("Another instance of MainCameraProvider already exists. Destroying self.");
          DestroyImmediate(this);
          return;
        }

        if (!_isInitialized) {
          DontDestroyOnLoad(gameObject);
          _isInitialized = true;
          m_Instance.Init();
        }
    }

    /// <summary>
    /// This function is called when the instance is used for the first time
    /// Put all the initializations you need here, as you would do in Awake
    /// </summary>
    public virtual void Init() { }

    private void OnApplicationQuit() {
      m_Instance = null;
    }

#endregion

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
