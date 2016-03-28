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
  		gameObject.SetActive(true);
  	}
  
  	// Use this for initialization
  	protected override void HandFinish () {
  		gameObject.SetActive(false);
  	}
  	
  }
}
