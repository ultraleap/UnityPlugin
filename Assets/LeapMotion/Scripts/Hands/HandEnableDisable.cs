using UnityEngine;
using System.Collections;
using Leap;

namespace Leap.Unity{
  /**Simple implementation of HandTransitionBehavior the turn IHandModel GameObjects on and off   */
  public class HandEnableDisable : HandTransitionBehavior {

    /** Called when a HandRepresentation's Leap Hand ends tracking */
    protected override void HandReset() {
  		gameObject.SetActive(true);
  	}
  
  	/** Called when a HandRepresentation's Leap Hand ends tracking */
  	protected override void HandFinish () {
  		gameObject.SetActive(false);
  	}
  }
}
