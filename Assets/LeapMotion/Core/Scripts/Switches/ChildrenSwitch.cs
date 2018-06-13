using Leap.Unity.Query;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Leap.Unity.Animation {

  public class ChildrenSwitch : ObjectSwitch {

    public override void RefreshSwitches() {
      GetComponentsInChildren<MonoBehaviour>(true).Query()
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

  }

}
