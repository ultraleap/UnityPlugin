/******************************************************************************
 * Copyright (C) Leap Motion, Inc. 2011-2017.                                 *
 * Leap Motion proprietary and  confidential.                                 *
 *                                                                            *
 * Use subject to the terms of the Leap Motion SDK Agreement available at     *
 * https://developer.leapmotion.com/sdk_agreement, or another agreement       *
 * between Leap Motion and you, your company or other organization.           *
 ******************************************************************************/

using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Profiling;
using System;
using System.Collections;
using System.Collections.Generic;
using Leap.Unity.Query;

namespace Leap.Unity.Graphing {

  public class RealtimeGraph : MonoBehaviour {

    private static RealtimeGraph _cachedInstance = null;
    public static RealtimeGraph Instance {
      get {
        if (_cachedInstance == null) {
          _cachedInstance = FindObjectOfType<RealtimeGraph>();
        }
        return _cachedInstance;
      }
    }

    public struct GraphKey {
      public string name;
      public GraphUnits units;
      public long tick;

      public GraphKey(string name, GraphUnits units, long tick = 0) {
        this.name = name;
        this.units = units;
        this.tick = tick;
      }
    }

    public enum GraphUnits {
      Miliseconds,
      Framerate
    }

    public enum GraphMode {
      Inclusive,
      Exclusive
    }

    [SerializeField]
    protected string _defaultGraph = "Framerate";

    [SerializeField]
    protected string[] _unitySamplerNames = new string[0];

    [SerializeField]
    protected GraphMode _graphMode = GraphMode.Exclusive;

    [SerializeField]
    protected int _historyLength = 128;

    [SerializeField]
    protected int _updatePeriod = 10;

    [SerializeField]
    protected int _samplesPerFrame = 1;

    [SerializeField]
    protected float _framerateLineSpacing = 60;

    [SerializeField]
    protected float _deltaLineSpacing = 10;

    [SerializeField]
    protected float _maxSmoothingDelay = 0.1f;

    [SerializeField]
    protected float _valueSmoothingDelay = 1;

    [Header("References")]
    [SerializeField]
    protected LeapServiceProvider _provider;

    [SerializeField]
    protected Renderer _graphRenderer;

    [SerializeField]
    protected Text titleLabel;

    [SerializeField]
    protected TextMesh valueMesh;

    [SerializeField]
    protected GameObject customGraphPrefab;

    public float UpdatePeriodFloat {
      set {
        _updatePeriod = Mathf.RoundToInt(Mathf.Lerp(1, 10, value));
      }
    }

    public float BatchSizeFloat {
      set {
        _samplesPerFrame = Mathf.RoundToInt(Mathf.Lerp(1, 10, value));
      }
    }

    public float GraphModeFloat {
      set {
        _graphMode = value > 0.5f ? GraphMode.Inclusive : GraphMode.Exclusive;
      }
    }

    protected System.Diagnostics.Stopwatch _stopwatch = new System.Diagnostics.Stopwatch();

    protected int _sampleIndex = 0;
    protected int _updateCount = 0;

    protected bool _paused = false;

    protected Texture2D _texture;
    protected Color32[] _colors;

    protected SmoothedFloat _smoothedValue;
    protected SmoothedFloat _smoothedMax;

    protected Graph _currentGraph;
    protected Dictionary<string, Graph> _graphs;
    protected Stack<Graph> _currentGraphStack = new Stack<Graph>();

    protected Deque<GraphKey> _keyBuffer = new Deque<GraphKey>();

    protected List<Recorder> _unityRecorders = new List<Recorder>();

    //Custom sample timers
    protected long _preCullTicks, _renderTicks, _fixedTicks = -1;

    public void BeginSample(GraphKey key) {
      key.tick = _stopwatch.ElapsedTicks;
      _keyBuffer.PushFront(key);
    }

    public void BeginSample(string sampleName, GraphUnits units) {
      GraphKey key = new GraphKey(sampleName, units, _stopwatch.ElapsedTicks);
      _keyBuffer.PushFront(key);
    }

    public void EndSample() {
      _keyBuffer.PushFront(new GraphKey(null, GraphUnits.Miliseconds, _stopwatch.ElapsedTicks));
    }

    public void AddSample(string sampleName, GraphUnits units, long ticks) {
      Graph graph = getGraph(sampleName, units);
      graph.AddSample(ticks);
    }

    public void AddSample(string sampleName, GraphUnits units, float ms) {
      Graph graph = getGraph(sampleName, units);
      graph.AddSample((long)(ms * System.Diagnostics.Stopwatch.Frequency * 0.001f));
    }

    public void SwtichGraph(string graphName) {
      _currentGraph = _graphs[graphName];
      titleLabel.text = graphName;
    }

    public void TogglePaused() {
      _paused = !_paused;
    }

    protected virtual void OnValidate() {
      _historyLength = Mathf.Max(1, _historyLength);
      _updatePeriod = Mathf.Max(1, _updatePeriod);
    }

    protected virtual void Awake() {
      _graphs = new Dictionary<string, Graph>();

      _smoothedMax = new SmoothedFloat();
      _smoothedMax.delay = _maxSmoothingDelay;

      _smoothedValue = new SmoothedFloat();
      _smoothedValue.delay = _valueSmoothingDelay;
    }

    protected virtual void Start() {
      _provider = _provider ?? FindObjectOfType<LeapServiceProvider>();

      _texture = new Texture2D(_historyLength, 1, TextureFormat.Alpha8, false, true);
      _texture.filterMode = FilterMode.Point;
      _texture.wrapMode = TextureWrapMode.Clamp;
      _colors = new Color32[_historyLength];

      _graphRenderer.material.SetTexture("_GraphTexture", _texture);

      _unitySamplerNames.Query().Select(n => {
        getGraph(n, GraphUnits.Miliseconds);
        return Recorder.Get(n);
      }).FillList(_unityRecorders);

      _stopwatch.Start();
    }

    protected virtual void OnEnable() {
      Camera.onPreCull += onPreCull;
      Camera.onPostRender += onPostRender;

      StartCoroutine(endOfFrameWaiter());
    }

    protected virtual void OnDisable() {
      Camera.onPreCull -= onPreCull;
      Camera.onPostRender -= onPostRender;
    }

    protected virtual void Update() {
      if (_fixedTicks != -1) {
        AddSample("Physics Delta", GraphUnits.Miliseconds, _stopwatch.ElapsedTicks - _fixedTicks);
        _fixedTicks = -1;
      }

      _preCullTicks = -1;

      if (_provider != null) {
        AddSample("Tracking Framerate", GraphUnits.Framerate, 1000.0f / _provider.CurrentFrame.CurrentFramesPerSecond);
      }

      for (int i = 0; i < _unityRecorders.Count; i++) {
        var recorder = _unityRecorders[i];
        AddSample(_unitySamplerNames[i], GraphUnits.Miliseconds, recorder.elapsedNanoseconds / 1000000.0f);
      }

      if (_currentGraph == null) {
        return;
      }

      _sampleIndex++;
      if (_sampleIndex < _samplesPerFrame) {
        return;
      }

      replayKeys();

      for (var it = _graphs.GetEnumerator(); it.MoveNext();) {
        var graph = it.Current.Value;
        if (_paused) {
          graph.ClearSample();
        } else {
          graph.RecordSample(_sampleIndex);
        }
      }
      _sampleIndex = 0;

      float currValue, currMax;
      switch (_graphMode) {
        case GraphMode.Exclusive:
          currValue = _currentGraph.exclusive.Back;
          currMax = _currentGraph.exclusiveMax.Max;
          break;
        case GraphMode.Inclusive:
          currValue = _currentGraph.inclusive.Back;
          currMax = _currentGraph.inclusiveMax.Max;
          break;
        default:
          throw new Exception("Unexpected graph mode");
      }

      _smoothedValue.Update(currValue, Time.deltaTime);

      _smoothedMax.Update(currMax, Time.deltaTime);

      _updateCount++;
      if (_updateCount >= _updatePeriod) {
        UpdateTexture();
        _updateCount = 0;
      }
    }

    protected virtual void FixedUpdate() {
      if (_fixedTicks == -1) {
        _fixedTicks = _stopwatch.ElapsedTicks;
      }
    }

    private IEnumerator endOfFrameWaiter() {
      WaitForEndOfFrame waiter = new WaitForEndOfFrame();
      long endOfFrameTicks = _stopwatch.ElapsedTicks;
      while (true) {
        yield return waiter;

        long newTicks = _stopwatch.ElapsedTicks;
        AddSample("Frame Delta", GraphUnits.Miliseconds, newTicks - endOfFrameTicks);
        AddSample("Framerate", GraphUnits.Framerate, newTicks - endOfFrameTicks);
        endOfFrameTicks = newTicks;

        AddSample("Render Delta", GraphUnits.Miliseconds, _renderTicks);

        float gpuTime;
#if UNITY_5
        gpuTime = UnityEngine.VR.VRStats.gpuTimeLastFrame;
#else
        UnityEngine.XR.XRStats.TryGetGPUTimeLastFrame(out gpuTime);
#endif

        AddSample("GPU Time", GraphUnits.Miliseconds, gpuTime);

        if (_provider != null) {
          AddSample("Tracking Latency", GraphUnits.Miliseconds, (_provider.GetLeapController().Now() - _provider.CurrentFrame.Timestamp) * 0.001f);
        }
      }
    }

    private void onPreCull(Camera camera) {
      if (_preCullTicks == -1) {
        _preCullTicks = _stopwatch.ElapsedTicks;
      }
    }

    private void onPostRender(Camera camera) {
      _renderTicks = _stopwatch.ElapsedTicks - _preCullTicks;
    }

    protected static float ticksToMs(long ticks) {
      return (float)(ticks / (System.Diagnostics.Stopwatch.Frequency / 1000.0));
    }

    private string msToString(float ms) {
      return (Mathf.Round(ms * 10) * 0.1f).ToString();
    }

    private long msToTicks(float ms) {
      return (long)(ms * System.Diagnostics.Stopwatch.Frequency * 1000);
    }

    private float getGraphSpacing() {
      switch (_currentGraph.units) {
        case GraphUnits.Framerate:
          return _framerateLineSpacing;
        case GraphUnits.Miliseconds:
          return _deltaLineSpacing;
        default:
          throw new Exception("Unexpected graph units");
      }
    }

    private void UpdateTexture() {
      float max = _smoothedMax.value * 1.5f;

      Deque<float> history;
      switch (_graphMode) {
        case GraphMode.Exclusive:
          history = _currentGraph.exclusive;
          break;
        case GraphMode.Inclusive:
          history = _currentGraph.inclusive;
          break;
        default:
          throw new Exception("Unexpected graph mode");
      }

      for (int i = 0; i < history.Count; i++) {
        float percent = Mathf.Clamp01(history[i] / max);
        byte percentByte = (byte)(percent * 255.9999f);
        _colors[i] = new Color32(percentByte, percentByte, percentByte, percentByte);
      }

      _graphRenderer.material.SetFloat("_GraphScale", max / getGraphSpacing());

      _texture.SetPixels32(_colors);
      _texture.Apply();

      valueMesh.text = msToString(_smoothedValue.value);

      Vector3 localP = valueMesh.transform.localPosition;
      localP.y = _smoothedValue.value / max - 0.5f;
      valueMesh.transform.localPosition = localP;
    }

    protected Graph getGraph(string name, GraphUnits units) {
      Graph graph;
      if (!_graphs.TryGetValue(name, out graph)) {
        graph = new Graph(name, units, _historyLength);
        _graphs[name] = graph;

        GameObject buttonObj = Instantiate(customGraphPrefab);
        buttonObj.transform.SetParent(customGraphPrefab.transform.parent, false);

        Text buttonText = buttonObj.GetComponentInChildren<Text>();
        buttonText.text = name.Replace(' ', '\n');

        Button button = buttonObj.GetComponentInChildren<Button>();
        addCallback(button, name);

        buttonObj.SetActive(true);

        if (_currentGraph == null && name == _defaultGraph) {
          SwtichGraph(name);
        }
      }
      return graph;
    }

    protected void addCallback(Button button, string name) {
      button.onClick.AddListener(() => SwtichGraph(name));
    }

    protected void replayKeys() {
      GraphKey key;
      while (_keyBuffer.Count > 0) {
        _keyBuffer.PopBack(out key);

        if (key.name != null) {
          Graph graph = getGraph(key.name, key.units);

          if (_currentGraphStack.Count != 0) {
            _currentGraphStack.Peek().PauseSample(key.tick);
          }

          graph.BeginSample(key.tick);

          _currentGraphStack.Push(graph);
        } else {
          Graph graph = _currentGraphStack.Pop();
          graph.EndSample(key.tick);

          if (_currentGraphStack.Count != 0) {
            _currentGraphStack.Peek().ResumeSample(key.tick);
          }
        }
      }
    }

    protected class Graph {
      public string name;
      public GraphUnits units;
      public Deque<float> exclusive;
      public Deque<float> inclusive;
      public SlidingMax exclusiveMax, inclusiveMax;

      private int maxHistory;

      private long accumulatedInclusiveTicks, accumulatedExclusiveTicks;
      private long inclusiveStart, exclusiveStart;

      public Graph(string name, GraphUnits units, int maxHistory) {
        this.name = name;
        this.units = units;
        this.maxHistory = maxHistory;
        exclusive = new Deque<float>(maxHistory);
        inclusive = new Deque<float>(maxHistory);
        exclusiveMax = new SlidingMax(maxHistory);
        inclusiveMax = new SlidingMax(maxHistory);
      }

      public void BeginSample(long currTicks) {
        inclusiveStart = exclusiveStart = currTicks;
      }

      public void PauseSample(long currTicks) {
        accumulatedInclusiveTicks += currTicks - inclusiveStart;
      }

      public void ResumeSample(long currTicks) {
        inclusiveStart = currTicks;
      }

      public void EndSample(long currTicks) {
        accumulatedInclusiveTicks += currTicks - inclusiveStart;
        accumulatedExclusiveTicks += currTicks - exclusiveStart;
      }

      public void AddSample(long ticks) {
        accumulatedInclusiveTicks += ticks;
        accumulatedExclusiveTicks += ticks;
      }

      public void ClearSample() {
        accumulatedExclusiveTicks = accumulatedInclusiveTicks = 0;
      }

      public void RecordSample(int sampleCount) {
        float inclusiveMs = ticksToMs(accumulatedInclusiveTicks / sampleCount);
        float exclusiveMs = ticksToMs(accumulatedExclusiveTicks / sampleCount);
        ClearSample();

        switch (units) {
          case GraphUnits.Miliseconds:
            inclusive.PushBack(inclusiveMs);
            exclusive.PushBack(exclusiveMs);
            inclusiveMax.AddValue(inclusiveMs);
            exclusiveMax.AddValue(exclusiveMs);
            break;
          case GraphUnits.Framerate:
            inclusive.PushBack(1000.0f / inclusiveMs);
            exclusive.PushBack(1000.0f / exclusiveMs);
            inclusiveMax.AddValue(1000.0f / inclusiveMs);
            exclusiveMax.AddValue(1000.0f / exclusiveMs);
            break;
          default:
            throw new Exception("Unexpected units type");
        }

        while (inclusive.Count > maxHistory) {
          inclusive.PopFront();
        }
        while (exclusive.Count > maxHistory) {
          exclusive.PopFront();
        }
      }
    }
  }
}
