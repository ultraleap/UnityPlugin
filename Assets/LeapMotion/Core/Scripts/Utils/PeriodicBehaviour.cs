using UnityEngine;

namespace Leap.Unity {

  public abstract class PeriodicBehaviour : MonoBehaviour {

    #region Periodicity

    private Maybe<int> _lastKnownPeriod = Maybe.None;
    private int _updatePeriod = 2;

    private int _phase;

    /// <summary>
    /// How often should this behaviour receive its PeriodicUpdate?
    /// A value of zero or lower will not receive a PeriodicUpdate.
    /// Values equal to 1 will receive a PeriodicUpdate every Unity Update().
    /// A value of 2 will receive a PeriodicUpdate every other Update().
    /// 3 will receive a PeriodicUpdate for every 3 Update()s, and etc.
    /// 
    /// The default _updatePeriod value is 2, and it can be changed at any time.
    /// Any changes will take effect on the next Update().
    /// </summary>
    public int updatePeriod {
      get {
        return _updatePeriod;
      }
      protected set {
        _updatePeriod = Mathf.Max(1, value);
      }
    }

    /// <summary>
    /// The PeriodicBehaviour will call this once for every N Updates(), where N
    /// is the current updatePeriod. (When the updatePeriod changes, the object
    /// will receive a new phase, randomly determined within the update period
    /// range.)
    /// </summary>
    public abstract void PeriodicUpdate();

    #endregion

    #region Unity Events

    private int _clock = 0;

    protected virtual void Update() {
      var didUpdatePeriodChange = updatePeriod != _lastKnownPeriod;

      if (didUpdatePeriodChange) {
        // Assign a new phase within the specified period.
        if (updatePeriod <= 1) {
          _phase = 0;
        }
        else {
          // TODO: This leaves a performance optimization up to chance. Instead,
          // static memory should track which phases are heaviest and minimize
          // their use.
          _phase = Random.Range(0, updatePeriod - 1);
        }

        _lastKnownPeriod = updatePeriod;
      }


      if (_updatePeriod == 0) return;
      else {
        if (_clock == _phase) {
          PeriodicUpdate();
        }

        // Tick the clock.
        _clock += 1;
        _clock %= _updatePeriod;
      }
    }

    #endregion

  }

}