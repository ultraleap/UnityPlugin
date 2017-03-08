using Leap.Unity;
using Leap.Unity.Animation;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Leap.Unity.LeapPaint {

  public class Toolbelt : MonoBehaviour {

    public TransformTweenBehaviour openCloseAnimation;
    public FollowPlayerBehaviour followPlayerBehaviour;
    public AnimationCurve lookProductFollowCurve;

    void Update() {
      UpdateOpenClosed();
      UpdateFollowPlayer();
    }

    #region Open / Close Behavior

    public bool isOpen { get { return openCloseAnimation.tween.progress > 0.999F; } }
    public bool isClosed { get { return openCloseAnimation.tween.progress < 0.001F; } }

    private bool _ready = true;

    private bool isPlayerLookingDown { get { return Vector3.Dot(Camera.main.transform.forward, Vector3.down) > 0.5F; } }
    private bool isPlayerLookingUp { get { return Vector3.Dot(Camera.main.transform.forward, Vector3.up) > -0.2F; } }
    private void UpdateOpenClosed() {
      if (!isOpen && isPlayerLookingDown && _ready) {
        openCloseAnimation.tween.Play(Direction.Forward);
        _ready = false;
      }

      if (!isClosed && isPlayerLookingUp) {
        openCloseAnimation.tween.Play(Direction.Backward);
      }

      if (isPlayerLookingUp) {
        _ready = true;
      }
    }

    #endregion

    #region Following Player

    //private float _lerpCoeffWhileOpenLookTowards = 0.1F;
    //private float _lerpCoeffWhileOpenLookAway = 0.0F;
    private float _lerpCoeffWhileClosed = 20F;

    private void UpdateFollowPlayer() {
      float lookTowardsAmount = lookProductFollowCurve.Evaluate(
        Vector3.Dot(Camera.main.transform.forward, (this.transform.position - Camera.main.transform.position).normalized));
      float lerpCoeffWhileOpen = lookTowardsAmount;

      float followCoeffPerSec = Mathf.Lerp(_lerpCoeffWhileClosed, lerpCoeffWhileOpen, openCloseAnimation.tween.progress * openCloseAnimation.tween.progress);
      followPlayerBehaviour.posLerpCoeffPerSec = followCoeffPerSec;
      followPlayerBehaviour.rotLerpCoeffPerSec = followCoeffPerSec;
    }

    #endregion

  }

}