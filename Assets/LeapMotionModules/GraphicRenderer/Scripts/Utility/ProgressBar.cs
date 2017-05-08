/******************************************************************************
 * Copyright (C) Leap Motion, Inc. 2011-2017.                                 *
 * Leap Motion proprietary and  confidential.                                 *
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

namespace Leap.Unity.GraphicalRenderer {

  public interface IProgressView {
    void Clear();
    void DisplayProgress(string title, string info, float progress);
  }

#if UNITY_EDITOR
  public class EditorProgressView : IProgressView {
    public static readonly EditorProgressView Single = new EditorProgressView();

    public void Clear() {
      EditorUtility.ClearProgressBar();
    }

    public void DisplayProgress(string title, string info, float progress) {
      EditorUtility.DisplayProgressBar(title, info, progress);
    }
  }
#endif

  public class ProgressBar {
    private List<int> chunks = new List<int>();
    private List<int> progress = new List<int>();
    private List<string> titleStrings = new List<string>();
    private List<string> infoStrings = new List<string>();
    private Stopwatch stopwatch = new Stopwatch();

    private IProgressView _view;

#if UNITY_EDITOR
    public ProgressBar() : this(EditorProgressView.Single) { }
#endif

    public ProgressBar(IProgressView view) {
      _view = view;
    }

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

    public void Step(string infoString = "") {
      progress[progress.Count - 1]++;
      if (stopwatch.ElapsedMilliseconds > 17) {
        displayBar(infoString);
        stopwatch.Reset();
        stopwatch.Start();
      }
    }

    private void displayBar(string info = "") {
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
