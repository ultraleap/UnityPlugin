using UnityEngine;
using System.Collections;


namespace InteractionEngine {

  public class InteractionTransform : InteractionObject {

    public override void HandleGrabMove(object eventObj) {
      base.HandleGrabMove(eventObj);
      /*
      transform.position = eventObj.object.position;
      transform.rotation = eventObj.object.rotation;
      */
    }

  }
}
