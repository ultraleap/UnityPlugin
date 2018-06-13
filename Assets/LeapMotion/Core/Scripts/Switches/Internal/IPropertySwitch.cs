using UnityEngine;

namespace Leap.Unity.Animation {

  /// <summary>
  /// Interface for a class that switches between an On state and an Off state,
  /// potentially via animations and delays at runtime as well as via immediate state
  /// transitions at edit-time.
  /// </summary>
  public interface IPropertySwitch {

    /// <summary>
    /// Tells the IPropertySwitch to turn on, appear, or activate.
    /// 
    /// The switch is NOT requred to "finish" turning on immediately, so properties may
    /// not immediately reflect an activated state.
    /// 
    /// However, GetIsOnOrTurningOn() MUST return true immediately after method is called.
    /// </summary>
    void On();

    /// <summary>
    /// Returns whether the most recent On() or Off() call to this switch was On().
    /// </summary>
    bool GetIsOnOrTurningOn();

    /// <summary>
    /// Tells the IPropertySwitch to turn off, disappear, or deactivate.
    /// 
    /// The switch is NOT required to "finish" turning off immediately, so properties may
    /// not immediately reflect a deactivated state.
    /// 
    /// However, GetIsOffOrTurningOff() MUST return true immediately after this method is
    /// called.
    /// </summary>
    void Off();

    /// <summary>
    /// Returns whether the most recent On() or Off() call to this switch was Off().
    /// </summary>
    bool GetIsOffOrTurningOff();

    /// <summary>
    /// This method must result in the same effects as On(), but additionally reflect the
    /// FINISHED state of the object after an On() call, immediately.
    /// 
    /// This method must be valid to call during edit-time, and its effects must also
    /// respect Undo behaviour through the use of Undo.X() calls.
    /// </summary>
    void OnNow();

    /// <summary>
    /// This method must result in the same effects as Off(), but additionally reflect
    /// the FINISHED state of the object after an Off() call, immediately.
    /// 
    /// This method must be valid to call during edit-time, and its effects must also
    /// respect Undo behaviour through the use of Undo.X() calls.
    /// </summary>
    void OffNow();

  }

  public static class IPropertySwitchExtensions {

    public static void AutoOn(this IPropertySwitch propSwitch) {
      if (Application.isPlaying) {
        propSwitch.On();
      }
      else {
        propSwitch.OnNow();
      }
    }

    public static void AutoOff(this IPropertySwitch propSwitch) {
      if (Application.isPlaying) {
        propSwitch.Off();
      }
      else {
        propSwitch.OffNow();
      }
    }

  }

}
