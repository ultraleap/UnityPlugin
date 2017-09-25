/******************************************************************************
 * Copyright (C) Leap Motion, Inc. 2011-2017.                                 *
 * Leap Motion proprietary and  confidential.                                 *
 *                                                                            *
 * Use subject to the terms of the Leap Motion SDK Agreement available at     *
 * https://developer.leapmotion.com/sdk_agreement, or another agreement       *
 * between Leap Motion and you, your company or other organization.           *
 ******************************************************************************/

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

namespace Leap.Unity.Recording {

  public class EventPlayableMixerBehaviour : PlayableBehaviour {

    public PlayableDirector director;
    public EventTrack eventTrack;

    private List<TimelineClip> _clips;
    private TimelineClipComparerer _clipComparer;

    private bool _firstFrameFired = false;
    private double _lastTime = 0;

    #region PlayableBehaviour Events

    public override void OnGraphStart(Playable playable) {
      base.OnGraphStart(playable);

      if (_clips == null) {
        _clips = Pool<List<TimelineClip>>.Spawn();
      }
      if (_clipComparer == null) {
        _clipComparer = Pool<TimelineClipComparerer>.Spawn();
      }

      _firstFrameFired = false;
      _lastTime = 0;
    }

    public override void OnGraphStop(Playable playable) {
      base.OnGraphStop(playable);

      if (_clips != null) {
        Pool<List<TimelineClip>>.Recycle(_clips);
        _clips = null;
      }
      if (_clipComparer != null) {
        Pool<TimelineClipComparerer>.Recycle(_clipComparer);
        _clipComparer = null;
      }

      _firstFrameFired = false;
      _lastTime = 0;
    }

    public override void PrepareFrame(Playable playable, FrameData info) {
      base.PrepareFrame(playable, info);

      refreshEvents();
    }

    public override void ProcessFrame(Playable playable, FrameData info, object playerData) {
      base.ProcessFrame(playable, info, playerData);

      // If there's an event at the beginning of the track, and we're currently at the
      // beginning of the track, we should fire it now.
      checkFireTimeZeroEvent();

      if (!_firstFrameFired) {
        _firstFrameFired = true;
      }
      else {
        double curTime = director.time;
        
        if (_lastTime <= curTime) {
          // Sweep forward from prevTime to curTime if moving forward.
          sweepFireEvents(_lastTime, curTime);
        }
        else {
          // When scrubbing backwards, sweep from the beginning of the track, potentially
          // firing events based on the eventScrubType.
          sweepFireFromBeginning(_lastTime, curTime);
        }
      }

      _lastTime = director.time;
    }

    #endregion

    #region Event Management & Firing

    /// <summary>
    /// Rebuilds the clip list by referencing the track's known clips, sorting it by
    /// start time if necessary.
    /// 
    /// Clips are usually sorted when a track is initialized, but can become unsorted if
    /// the user manually changes their order by dragging clips around.
    /// </summary>
    private void refreshEvents() {
      _clips.Clear();

      TimelineClip prevClip = null;
      bool notSorted = false;
      foreach (var clip in eventTrack.GetClips()) {
        _clips.Add(clip);

        if (prevClip != null) {
          if (clip.start < prevClip.start) {
            notSorted = true;
          }
        }
        prevClip = clip;
      }

      if (notSorted) {
        _clips.Sort(_clipComparer);
      }
    }

    /// <summary>
    /// If the director for this timeline is at the start of the timeline and there is
    /// an event set to start at the start of the timeline, this check will fire that
    /// event.
    /// 
    /// This check is necessary because robust event-firing logic usually
    /// requires a previous frame time and a current frame time, which is unavailable
    /// at the beginning of a timeline.
    /// </summary>
    private void checkFireTimeZeroEvent() {
      if (director.time == 0 && Application.isPlaying) {
        if (_clips.Count > 0) {
          var firstClip = _clips[0];
          if (firstClip.start == 0) {
            (firstClip.asset as EventClip).FireEvent();
          }
        }
      }
    }

    /// <summary>
    /// Sweeps from prevTime to curTime on the timeline, firing any events
    /// whose start times falls in between the two times.
    /// 
    /// The method should only be called if there was a valid prevTime during which
    /// the track was playing, and only if prevTime is less than curTime.
    /// </summary>
    private void sweepFireEvents(double prevTime, double curTime) {
      if (prevTime > curTime) {
        Debug.LogError("sweepFireEvents should only be called when the playhead is moving "
                     + "forward in time.");
      }

      // Find the index of the first event that might be fired.
      int firstEventIdx = _clips.Count;
      for (int i = 0; i < _clips.Count; i++) {
        var clip = _clips[i];
        if (prevTime < clip.start) {
          firstEventIdx = i;
          break;
        }
      }

      // Sweep to curTime, firing events along the way.
      for (int i = firstEventIdx; i < _clips.Count; i++) {
        var clip = _clips[i];

        if (curTime < clip.start) {
          break;
        }

        // Fire!
        var eventClip = clip.asset as EventClip;
        if (eventClip != null) {
          eventClip.FireEvent();
        }
      }
    }

    /// <summary>
    /// This method checks if the playhead scrubbed BACKWARDS across an event boundary --
    /// that is, an event's start time. For each event with a unique message that was
    /// scrubbed through, another event prior to that event (with the same unique message)
    /// may fire, depending on the scrubbed event's eventScrubType:
    /// 
    /// - Trigger: These events represent one-shots, such as sound or particle effects.
    ///     Triggers are NOT fired when scrubbing backwards, so these events are ignored.
    /// - StateChange: These events represent simple state changes. The most recent event
    ///     BEFORE the scrubbed event whose message matches the scrubbed event is fired.
    /// 
    /// This method should only be called if the playhead has scrubbed backwards.
    /// </summary>
    private void sweepFireFromBeginning(double prevTime, double curTime) {
      if (prevTime < curTime) {
        Debug.LogError("sweepFireFromBeginning should only be called if the playhead has "
                     + "scrubbed backwards in time.");
      }

      var indices = Pool<List<int>>.Spawn();
      var prevIndices = Pool<List<Maybe<int>>>.Spawn();
      try {
        getUniqueMessageScrubbedEvents(prevTime, curTime, indices, prevIndices);

        for (int i = 0; i < indices.Count; i++) {
          TimelineClip clip = _clips[indices[i]];
          TimelineClip prevClip = null;

          var maybePrevClipIdx = prevIndices[i];
          int prevClipIdx = -1;
          if (maybePrevClipIdx.hasValue) {
            prevClipIdx = maybePrevClipIdx.valueOrDefault;

            prevClip = _clips[prevClipIdx];
          }

          if (prevClip != null) {
            var eventClip = (clip.asset as EventClip);
            var prevEventClip = (prevClip.asset as EventClip);

            if (eventClip.eventScrubType == EventScrubType.StateChange) {
              // Debug.Log("Firing clip index " + prevClipIdx + " because its successor "
              //         + "is a StateChange event");

              prevEventClip.FireEvent();
            }
          }
        }
      }
      finally {
        indices.Clear();
        Pool<List<int>>.Recycle(indices);
        prevIndices.Clear();
        Pool<List<Maybe<int>>>.Recycle(prevIndices);
      }
    }

    /// <summary>
    /// Fills the provided indicesBuffer with the _latest occurring_ events with a unique
    /// message argument that were scrubbed through, and prevIdxBuffer with Some(idx) if
    /// there exists an event prior to the event at the same position in the indicesBuffer
    /// with the same message.
    /// 
    /// After calling this method, indicesBuffer and prevIdxBuffer are guaranteed to be
    /// the same length.
    /// </summary>
    private void getUniqueMessageScrubbedEvents(double time0, double time1,
                                                List<int> indicesBuffer,
                                                List<Maybe<int>> prevIdxBuffer) {
      if (indicesBuffer.Count != 0) indicesBuffer.Clear();
      if (prevIdxBuffer.Count != 0) prevIdxBuffer.Clear();
      
      if (time1 < time0) {
        Utils.Swap(ref time0, ref time1);
      }

      // mostRecentEvents will key an event message to the two latest events with that
      // event message. (index 0 latest, index 1 just-prior, or -1 if no prior event
      // exists).
      var mostRecentEvents = Pool<Dictionary<string, Pair<int>>>.Spawn();
      try {
        for (int i = 0; i < _clips.Count; i++) {
          var timelineClip = _clips[i];
          if (timelineClip.start > time1) {
            // We've hit a clip whose start time is after the latest scrub range time.
            // Since clips are sorted by start time, we've done all the work we have to.
            break;
          }

          var eventClip = timelineClip.asset as EventClip;

          var eventMessage = eventClip.message;
          Pair<int> indices;
          if (!mostRecentEvents.TryGetValue(eventMessage, out indices)) {
            mostRecentEvents[eventMessage] = new Pair<int>(i, -1);
          }
          else {
            mostRecentEvents[eventMessage] = new Pair<int>(i, indices[0]);
          }
        }

        // Convert mostRecentEvents to the output format.
        foreach (var messageIndicesPair in mostRecentEvents) {
          var indices = messageIndicesPair.Value;

          // We only need to fire previous events of events we _actually_ scrubbed through.
          if (!_clips[indices[0]].start.IsBetween(time0, time1)) continue;

          indicesBuffer.Add(indices[0]);
          prevIdxBuffer.Add((indices[1] == -1 ? Maybe<int>.None
                                              : Maybe<int>.Some(indices[1])));
        }
      }
      finally {
        mostRecentEvents.Clear();
        Pool<Dictionary<string, Pair<int>>>.Recycle(mostRecentEvents);
      }
    }

    #endregion

    #region Internal Utilities

    public struct Pair<U> {
      /// <summary>
      /// The first element in the pair.
      /// </summary>
      public U a;

      /// <summary>
      /// The second element in the pair.
      /// </summary>
      public U b;

      public U this[int idx] {
        get {
          checkIdx(idx);
          if (idx == 0) return a;
          else return b;
        }
        set {
          checkIdx(idx);
          if (idx == 0) a = value;
          else b = value;
        }
      }

      private void checkIdx(int idx) {
        if (idx > 1 || idx < 0) throw new IndexOutOfRangeException();
      }

      public Pair(U a, U b) {
        this.a = a;
        this.b = b;
      }
    }

    #endregion

  }

  public class TimelineClipComparerer : IComparer<TimelineClip> {
    public int Compare(TimelineClip x, TimelineClip y) {
      return x.start.CompareTo(y.start);
    }
  }

}
