/******************************************************************************
 * Copyright (C) Leap Motion, Inc. 2011-2018.                                 *
 * Leap Motion proprietary and confidential.                                  *
 *                                                                            *
 * Use subject to the terms of the Leap Motion SDK Agreement available at     *
 * https://developer.leapmotion.com/sdk_agreement, or another agreement       *
 * between Leap Motion and you, your company or other organization.           *
 ******************************************************************************/

#if UNITY_EDITOR
using UnityEditor;
#endif
using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Leap.Unity {

  /// <summary>
  /// This interface describes a generic way to update the progress of an action.
  /// </summary>
  public interface IProgressView {

    /// <summary>
    /// Clears the progress view.  Is called if the action has been completed
    /// or canceled.
    /// </summary>
    void Clear();

    /// <summary>
    /// Updates the progress view with some title text, information, and a
    /// progress percentage that ranges from 0 to 1.
    /// </summary>
    void DisplayProgress(string title, string info, float progress);
  }

#if UNITY_EDITOR
  /// <summary>
  /// An example progress view that uses the simple EditorUtility methods to 
  /// provide a developer with progress of an editor action.
  /// </summary>
  public class EditorProgressView : IProgressView {
    /// <summary>
    /// Gets a reference to a singleton instance of the EditorProgressView.
    /// This is safe because EditorProgressView is stateless.
    /// </summary>
    public static readonly EditorProgressView Single = new EditorProgressView();

    public void Clear() {
      EditorUtility.ClearProgressBar();
    }

    public void DisplayProgress(string title, string info, float progress) {
      EditorUtility.DisplayProgressBar(title, info, progress);
    }
  }
#endif

  /// <summary>
  /// This class allows you to easily give feedback of an action as
  /// it completes.
  /// 
  /// The progress bar is represented as a single 'Chunk' that is made
  /// of a certain number of sections.  The progress bar is hierarchical,
  /// and so each section can itself be another chunk.
  /// </summary>
  public class ProgressBar {
    private List<int> chunks = new List<int>();
    private List<int> progress = new List<int>();
    private List<string> titleStrings = new List<string>();
    private List<string> infoStrings = new List<string>();
    private Stopwatch stopwatch = new Stopwatch();

    private bool _forceUpdate;

    private IProgressView _view;

#if UNITY_EDITOR
    /// <summary>
    /// Constructs a new progress bar given a default EditorProgressView.
    /// You can use this constructor whenever you want to give progress 
    /// feedback to a Unity developer about an editor action that might
    /// take some time to complete.
    /// </summary>
    public ProgressBar() : this(EditorProgressView.Single) { }
#endif

    /// <summary>
    /// Constructs a new progress bar given a progress view object
    /// that will display the progress information for this progress
    /// bar.
    /// </summary>
    public ProgressBar(IProgressView view) {
      _view = view;
    }

    /// <summary>
    /// Begins a new chunk.  If this call is made from within a chunk it
    /// will generate a sub-chunk that represents a single step in the 
    /// parent chunk.
    /// 
    /// You must specify the number of sections this chunk contains.
    /// All title and info strings will be concatenated together when
    /// the progress bar is displayed.
    /// 
    /// You must specify a delegate that represents the action performed 
    /// by this chunk.  This delegate is allowed to call both Begin and
    /// StepProgress to progress through its work.
    /// </summary>
    public void Begin(int sections, string title, string info, Action action) {
      if (!stopwatch.IsRunning) {
        stopwatch.Reset();
        stopwatch.Start();
      }

      chunks.Add(sections);
      progress.Add(0);
      titleStrings.Add(title);
      infoStrings.Add(info);

      try {
        _forceUpdate = true;
        action();
      } finally {
        int lastIndex = chunks.Count - 1;
        chunks.RemoveAt(lastIndex);
        progress.RemoveAt(lastIndex);
        titleStrings.RemoveAt(lastIndex);
        infoStrings.RemoveAt(lastIndex);

        lastIndex--;
        if (lastIndex >= 0) {
          progress[lastIndex]++;
        }

        if (chunks.Count == 0) {
          _view.Clear();
          stopwatch.Stop();
        }
      }
    }

    /// <summary>
    /// Steps through one section of the current chunk.  You can provide
    /// an optional info string that will be concatenated to the current
    /// info string before progress is displayed.
    /// </summary>
    public void Step(string infoString = "") {
      progress[progress.Count - 1]++;
      if (stopwatch.ElapsedMilliseconds > 17 || _forceUpdate) {
        displayBar(infoString);
        stopwatch.Reset();
        stopwatch.Start();
      }
    }

    private void displayBar(string info = "") {
      _forceUpdate = false;

      float percent = 0.0f;
      float fraction = 1.0f;
      string titleString = "";
      string infoString = "";
      for (int i = 0; i < chunks.Count; i++) {
        float chunkSize = chunks[i];
        float chunkProgress = progress[i];

        percent += fraction * (chunkProgress / chunkSize);
        fraction /= chunkSize;

        titleString += titleStrings[i];
        infoString += infoStrings[i];
      }

      infoString += info;

      _view.DisplayProgress(titleString, infoString, percent);
    }
  }
}
