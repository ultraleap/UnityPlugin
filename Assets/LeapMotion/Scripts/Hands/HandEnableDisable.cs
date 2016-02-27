using UnityEngine;
using System.Collections;
using Leap;

public class HandEnableDisable : HandTransitionBehavior {

	protected override void HandReset() {
		gameObject.SetActive(true);
	}

	// Use this for initialization
	protected override void HandFinish () {
		gameObject.SetActive(false);
	}
	
}
