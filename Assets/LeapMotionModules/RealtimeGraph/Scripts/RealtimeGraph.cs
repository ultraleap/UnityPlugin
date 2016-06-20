using UnityEngine;
using UnityEngine.UI;
using System;
using System.Collections;
using System.Collections.Generic;

namespace Leap.Unity.RealtimeGraph {

  public class RealtimeGraph : MonoBehaviour {

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
    protected Canvas valueCanvas;

    [SerializeField]
    protected Text valueLabel;

    [SerializeField]
    protected GameObject customSamplePrefab;

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

    protected System.Diagnostics.Stopwatch _stopwatch = new System.Diagnostics.Stopwatch();
    protected long _preCullTicks, _postRenderTicks;
    protected long _fixedUpdateTicks;
    protected long _endOfFrameTicks;

    protected long _updateTicks;
    protected long _frameTicks;

    protected SlidingMax _slidingMax;

    protected int _sampleIndex = 0;
    protected float _sampleValue = 0;
    protected int _updateCount = 0;

    protected float _lineSpacing;
    protected Texture2D _texture;
    protected Color32[] _colors;

    protected SmoothedFloat _smoothedValue;
    protected SmoothedFloat _smoothedMax;

    protected Graph _currentGraph;
    protected Dictionary<string, Graph> _graphs;
    protected Stack<Graph> _currentGraphStack = new Stack<Graph>();

    public void BeginSample(string sampleName, GraphUnits units) {
      long currTicks = _stopwatch.ElapsedTicks;

      Graph graph;
      if (!_graphs.TryGetValue(sampleName, out graph)) {
        graph = new Graph(name, units, _historyLength);
        _graphs[name] = graph;
      }

      if (_currentGraphStack.Count != 0) {
        _currentGraphStack.Peek().PauseSample(currTicks);
      }

      graph.BeginSample(currTicks);

      _currentGraphStack.Push(graph);
    }

    public void EndSample() {
      long currTicks = _stopwatch.ElapsedTicks;

      Graph graph = _currentGraphStack.Pop();
      graph.EndSample(currTicks);

      if (_currentGraphStack.Count != 0) {
        _currentGraphStack.Peek().ResumeSample(currTicks);
      }
    }

    protected virtual void OnValidate() {
      _historyLength = Mathf.Max(1, _historyLength);
      _updatePeriod = Mathf.Max(1, _updatePeriod);
    }

    protected virtual void Awake() {
      _slidingMax = new SlidingMax(_historyLength);
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
      _preCullTicks = -1;

      _sampleIndex++;
      if (_sampleIndex < _samplesPerFrame) {
        return;
      }

      foreach (Graph graph in _graphs.Values) {
        graph.RecordSample(_sampleIndex);
      }
      _sampleIndex = 0;

      float currValue;
      switch (_graphMode) {
        case GraphMode.Exclusive:
          currValue = _currentGraph.exclusive.Front;
          break;
        case GraphMode.Inclusive:
          currValue = _currentGraph.inclusive.Front;
          break;
        default:
          throw new Exception("Unexpected graph mode");
      }

      _slidingMax.AddValue(currValue);

      _smoothedMax.Update(_slidingMax.Max, Time.deltaTime);

      _updateCount++;
      if (_updateCount >= _updatePeriod) {
        UpdateTexture();
        _updateCount = 0;
      }
    }

    protected virtual void FixedUpdate() {
      if (_fixedUpdateTicks == -1) {
        _fixedUpdateTicks = _stopwatch.ElapsedTicks;
      }
    }

    private IEnumerator endOfFrameWaiter() {
      WaitForEndOfFrame waiter = new WaitForEndOfFrame();
      while (true) {
        yield return waiter;
        long ticks = _stopwatch.ElapsedTicks;
        _frameTicks = ticks - _endOfFrameTicks;
        _endOfFrameTicks = ticks;
        _fixedUpdateTicks = -1;
      }
    }

    private void onPreCull(Camera camera) {
      if (_preCullTicks == -1) {
        _preCullTicks = _stopwatch.ElapsedTicks;
        if (_fixedUpdateTicks != -1) {
          _updateTicks = _preCullTicks - _fixedUpdateTicks;
        }
      }
    }

    private void onPostRender(Camera camera) {
      _postRenderTicks = _stopwatch.ElapsedTicks;
    }

    private float getRenderMs() {
      long tickDelta = _postRenderTicks - _preCullTicks;
      return ticksToMs(tickDelta);
    }

    private float getFrameMs() {
      return ticksToMs(_frameTicks);
    }

    private float getUpdateMs() {
      return ticksToMs(_updateTicks);
    }

    protected static float ticksToMs(long ticks) {
      return (float)(ticks / (System.Diagnostics.Stopwatch.Frequency / 1000.0));
    }

    private string msToString(float ms) {
      return (Mathf.Round(ms * 10) * 0.1f).ToString();
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

      Dequeue<float> history;
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

      for (int i = 0; i < _historyLength; i++) {
        float percent = Mathf.Clamp01(history[i] / max);
        byte percentByte = (byte)(percent * 255.9999f);
        _colors[i] = new Color32(percentByte, percentByte, percentByte, percentByte);
      }

      _graphRenderer.material.SetFloat("_GraphScale", max / getGraphSpacing());

      _texture.SetPixels32(_colors);
      _texture.Apply();

      valueLabel.text = msToString(_smoothedValue.value);

      Vector3 localP = valueCanvas.transform.localPosition;
      localP.y = _smoothedValue.value / max - 0.5f;
      valueCanvas.transform.localPosition = localP;
    }

    protected class Graph {
      public string name;
      public GraphUnits units;
      public Dequeue<float> exclusive;
      public Dequeue<float> inclusive;

      private int maxHistory;

      private long accumulatedInclusiveTicks, accumulatedExclusiveTicks;
      private long inclusiveStart, exclusiveStart;

      public Graph(string name, GraphUnits units, int maxHistory) {
        this.name = name;
        this.units = units;
        this.maxHistory = maxHistory;
        exclusive = new Dequeue<float>();
        inclusive = new Dequeue<float>();
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

      public void RecordSample(int sampleCount) {
        float inclusiveMs = ticksToMs(accumulatedInclusiveTicks / sampleCount);
        float exclusiveMs = ticksToMs(accumulatedExclusiveTicks / sampleCount);

        switch (units) {
          case GraphUnits.Miliseconds:
            inclusive.PushFront(inclusiveMs);
            exclusive.PushFront(exclusiveMs);
            break;
          case GraphUnits.Framerate:
            inclusive.PushFront(1000.0f / inclusiveMs);
            exclusive.PushFront(1000.0f / exclusiveMs);
            break;
          default:
            throw new Exception("Unexpected units type");
        }

        while (inclusive.Count > maxHistory) {
          inclusive.PopBack();
        }
        while (exclusive.Count > maxHistory) {
          exclusive.PopBack();
        }
      }
    }
  }
}
