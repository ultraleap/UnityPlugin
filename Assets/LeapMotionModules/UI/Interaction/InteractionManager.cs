using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Leap.Unity.UI.Interaction {

  public class InteractionManager : MonoBehaviour {

    private HashSet<Hoverable> _hovered = new HashSet<Hoverable>();
    private HashSet<Hoverable> _notHovered = new HashSet<Hoverable>();
    private Hoverable _leftPrimary = null;
    private Hoverable _rightPrimary = null;

    

  }

}