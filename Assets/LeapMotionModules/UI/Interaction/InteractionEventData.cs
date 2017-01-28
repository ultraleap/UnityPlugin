using Leap;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InteractionEventData {

  public Hand hand;
  public bool rejectInteraction;
  public Object userData;

}

public class HoverEventData : InteractionEventData {



}

public class GrabEventData : InteractionEventData {



}

public class TouchEventData : InteractionEventData {

  

}