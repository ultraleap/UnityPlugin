using UnityEngine;
using System.Collections;
using System;
using Leap;

namespace Leap.Unity{
  public class HandEnableDisable : HandTransitionBehavior {
    protected override void Awake() {
      base.Awake();
      gameObject.SetActive(false);
    }

  	protected override void HandReset() {
      gameObject.SetActive(true);
    }

    protected override void HandFinish () {
      StopAllCoroutines();
      try {
        StartCoroutine(changeStateNextTick(false));
      } catch (Exception e) {
        gameObject.SetActive(false);
      }
  	}

    /** Let child objects finish hierarchy modifications. */
    private IEnumerator changeStateNextTick(bool state) {
      yield return null;
      gameObject.SetActive(state);
    }
  }
}
