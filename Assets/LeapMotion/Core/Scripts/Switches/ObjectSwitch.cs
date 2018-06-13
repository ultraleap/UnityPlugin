using Leap.Unity.Attributes;
using Leap.Unity.Query;
using System.Collections.Generic;
using UnityEngine;

namespace Leap.Unity.Animation {

  [DisallowMultipleComponent]
  public class ObjectSwitch : MonoBehaviour, IPropertySwitch {

    #region Inspector

    [SerializeField]
    private bool _isOn;

    [Header("Attached Tween Overrides")]

    [Tooltip("If checked, you can specify the tween times for all tween-based "
           + "switches attached to this object switch.")]
    public bool overrideTweenTime = false;

    [DisableIf("overrideTweenTime", isEqualTo: false)]
    [OnEditorChange("RefreshSwitches")]
    public float tweenTime = 1f;

    #endregion

    #region Attached Switches

    protected List<IPropertySwitch> _switches = new List<IPropertySwitch>();
    public ReadonlyList<IPropertySwitch> switches {
      get { return _switches; }
    }

    protected bool _refreshed = false;

    #endregion

    #region Unity Events

    protected virtual void Reset() {
      RefreshSwitches();
    }

    protected virtual void OnValidate() {
      RefreshSwitches();
    }

    protected virtual void Start() {
      RefreshSwitches();
    }

    public virtual void RefreshSwitches() {
      GetComponents<MonoBehaviour>().Query()
                                    .Where(c => c is IPropertySwitch
                                                && !(c == this)
                                                && c.enabled)
                                    .Select(c => c as IPropertySwitch)
                                    .FillList(_switches);

      _refreshed = true;

      if (Application.isPlaying && overrideTweenTime) {
        foreach (var tweenSwitch in _switches.Query()
                                             .Where(s => s is TweenSwitch)
                                             .Cast<TweenSwitch>()) {
          tweenSwitch.tweenTime = tweenTime;
        }
      }
    }

    #endregion

    #region Switch Implementation

    public void On() {
      _isOn = true;

      if (!_refreshed) RefreshSwitches();
      
      foreach (var propertySwitch in _switches) {
        propertySwitch.On();
      }
    }

    public void OnNow() {
#if UNITY_EDITOR
      UnityEditor.Undo.RegisterFullObjectHierarchyUndo(this, "Activate ObjectSwitch");
#endif
      _isOn = true;

      foreach (var propertySwitch in _switches) {
        propertySwitch.OnNow();
      }
    }

    public bool GetIsOnOrTurningOn() {
      if (_switches.Count == 0) return _isOn;
      return _switches.Query().Any(c => c.GetIsOnOrTurningOn());
    }

    public void Off() {
      _isOn = false;

      foreach (var propertySwitch in _switches) {
        propertySwitch.Off();
      }
    }

    public void OffNow() {
#if UNITY_EDITOR
      UnityEditor.Undo.RegisterFullObjectHierarchyUndo(this, "Deactivate ObjectSwitch");
#endif
      _isOn = false;

      foreach (var propertySwitch in _switches) {
        propertySwitch.OffNow();
      }
    }

    public bool GetIsOffOrTurningOff() {
      if (_switches.Count == 0) return !_isOn;
      return _switches.Query().Any(c => c.GetIsOffOrTurningOff());
    }

    #endregion

    public void Toggle() {
      if (GetIsOffOrTurningOff()) {
        this.AutoOn();
      }
      else {
        this.AutoOff();
      }
    }

  }

}
