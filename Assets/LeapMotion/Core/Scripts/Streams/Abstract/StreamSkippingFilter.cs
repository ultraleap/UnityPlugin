using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Leap.Unity {

  public abstract class StreamSkippingFilter<T> : MonoBehaviour,
                                                  IStreamReceiver<T>,
                                                  IStream<T> {

    #region Inspector

    [Header("Passthrough")]
    public bool ignoreSkipping = false;

    [Header("Skip Settings")]

    public float alwaysSkipDistance = 0.01f;
    public float maxSkipDistance = 0.02f;
    public float maxSkipAngle = 4f;
    public float maxSkipRotationAngle = 4f;

    #endregion

    #region Stream<T> Events

    public event Action OnOpen = () => { };
    public event Action<T> OnSend = (t) => { };
    public event Action OnClose = () => { };

    #endregion

    #region IStreamReceiver<T> Implementation

    private bool _hasLastOutputT = false;
    private T _lastOutputT = default(T);
    private List<T> _skippedTs = new List<T>();

    public void Open() {
      _lastOutputT = default(T);
      _hasLastOutputT = false;
      _skippedTs.Clear();

      OnOpen();
    }

    public void Receive(T t) {
      updateFilter(t);
    }

    protected abstract void ShouldSkip(T data,
                                       T lastOutputData,
                                       List<T> skippedSoFar,
                                       out bool shouldSkip,
                                       out bool shouldRememberSkip,
                                       out Maybe<T> outputOverride);

    private void updateFilter(T t) {
      if (ignoreSkipping) {
        OnSend(t);
      }
      else {
        var outputT = t;
        var skipThisPose = true;

        if (!_hasLastOutputT) {
          outputT = t;
          skipThisPose = false;
        }
        else {
          bool rememberSkipped;
          Maybe<T> outputOverride = Maybe<T>.None;
          ShouldSkip(t, _lastOutputT,
                     _skippedTs,
                     out skipThisPose,
                     out rememberSkipped,
                     out outputOverride);

          T overrideValue;
          if (outputOverride.TryGetValue(out overrideValue)) {
            outputT = overrideValue;
          }

          if (skipThisPose && rememberSkipped) {
            _skippedTs.Add(t);
          }
        }

        // Potentially output a pose.
        if (!skipThisPose) {
          _lastOutputT = outputT;

          OnSend(outputT);

          _hasLastOutputT = true;
        }
      }
    }

    public void Close() {
      // Output final poses since we know there won't be any more.
      if (_skippedTs.Count > 0) {
        var lastSkippedPose = _skippedTs[_skippedTs.Count - 1];
        OnSend(lastSkippedPose);
      }

      _lastOutputT = default(T);
      _hasLastOutputT = false;
      _skippedTs.Clear();

      OnClose();
    }

    #endregion

  }

}
