using UnityEditor;
using UnityEngine;

namespace Leap.Unity.Animation {

  [CustomEditor(typeof(TweenSwitch), editorForChildClasses: true)]
  public class TweenSwitchEditor : SwitchEditorBase<TweenSwitch> { }

}
