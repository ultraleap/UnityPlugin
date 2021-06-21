// Implementation comes from several online sources. 
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Assets.Plugins.UnityModules.Assets.Plugins.LeapMotion.Core.Scripts.Utils
{
    using UnityEngine;
    using System.Collections;
    using System.Collections.Generic;

    /// <summary>
    /// Mono singleton Class. Extend this class to make singleton component.
    /// Example: 
    /// <code>
    /// public class Foo : MonoSingleton<Foo>
    /// </code>. To get the instance of Foo class, use <code>Foo.instance</code>
    /// Override <code>Init()</code> method instead of using <code>Awake()</code>
    /// from this class.
    /// </summary>
    public abstract class MonoSingleton<T> : MonoBehaviour where T : MonoSingleton<T> {
      
      private static T m_Instance = null;

      public static T Instance {
        get {
          // Instance requiered for the first time, we look for it
          if (m_Instance == null) {
            m_Instance = GameObject.FindObjectOfType(typeof(T)) as T;

            // Object not found, we create a temporary one
            if (m_Instance == null) {
              Debug.LogWarning("No instance of " + typeof(T).ToString() + ", a temporary one is created.");

              isTemporaryInstance = true;
              m_Instance = new GameObject("Temp Instance of " + typeof(T).ToString(), typeof(T)).GetComponent<T>();

              // Problem during the creation, this should not happen
              if (m_Instance == null) {
                Debug.LogError("Problem during the creation of " + typeof(T).ToString());
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

    // If no other monobehaviour request the instance in an awake function
    // executing before this one, no need to search the object.
    private void Awake() {
      if (m_Instance == null) {
        m_Instance = this as T;
      } else if (m_Instance != this) {
        Debug.LogError("Another instance of " + GetType() + " is already exist! Destroying self...");
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
    /// This function is called when the instance is used the first time
    /// Put all the initializations you need here, as you would do in Awake
    /// </summary>
    public virtual void Init() { }

    /// Make sure the instance isn't referenced anymore when the user quits, just in case.
    private void OnApplicationQuit() {
     m_Instance = null;
    }
  }
}
