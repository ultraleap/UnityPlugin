using UnityEngine;
using System.Collections;
using Leap;

namespace Leap.Unity{
  public class HandEnableDisable : HandTransitionBehavior {
    protected override void Awake() {
      base.Awake();
      gameObject.SetActive(false);
    }

  	protected override void HandReset() {
      if (iHandModel.IsEnabled == true) {
        gameObject.SetActive(true);
      }
  	}
  
  	protected override void HandFinish () {
  		gameObject.SetActive(false);
  	}
  	
  }
}
