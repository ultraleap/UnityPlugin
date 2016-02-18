using UnityEngine;
using System.Collections;
using Leap;

public class HandEnableDisable : HandTransitionBehavior {

  public override void Reset() {
    gameObject.SetActive(true);
  }

	// Use this for initialization
	public override void HandFinish () {
    gameObject.SetActive(false);
	}
	
}
