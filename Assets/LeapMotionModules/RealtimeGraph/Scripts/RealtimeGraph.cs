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

    [SerializeField]
    private string _defaultGraph = "Framerate";

    [SerializeField]
    protected int _historyLength = 128;

    [SerializeField]
    protected int _updatePeriod = 10;

    [SerializeField]
    protected int _samplesPerFrame = 1;

    [SerializeField]
    private float _framerateLineSpacing = 60;

    [SerializeField]
    private float _deltaLineSpacing = 10;

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

    protected Dequeue<float> _history;
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
        graph = new Graph(name, units);
        _graphs[name] = graph;
      }

      if (_currentGraphStack.Count != 0) {
        Graph currentGraph = _currentGraphStack.Peek();
        currentGraph.currentSample.totalExclusiveTicks += currTicks - currentGraph.exclusiveStart;
      }

      graph.exclusiveStart = graph.inclusiveStart = currTicks;

      _currentGraphStack.Push(graph);
    }

    public void EndSample() {
      long currTicks = _stopwatch.ElapsedTicks;

      Graph graph = _currentGraphStack.Pop();
      graph.currentSample.totalInclusiveTicks = currTicks - graph.inclusiveStart;
      graph.currentSample.totalExclusiveTicks += currTicks - graph.exclusiveStart;

      if (_currentGraphStack.Count != 0) {
        Graph nextGraph = _currentGraphStack.Peek();
        nextGraph.exclusiveStart = currTicks;
      }
    }

    protected virtual void OnValidate() {
      _historyLength = Mathf.Max(1, _historyLength);
      _updatePeriod = Mathf.Max(1, _updatePeriod);
    }

    protected virtual void Awake() {
      _history = new Dequeue<float>(_historyLength);
      _slidingMax = new SlidingMax(_historyLength);
      _graphs = new Dictionary<string, Graph>();

      _smoothedMax = new SmoothedFloat();
      _smoothedMax.delay = _maxSmoothingDelay;

      _smoothedValue = new SmoothedFloat();
      _smoothedValue.delay = _valueSmoothingDelay;

      for (int i = 0; i < _historyLength; i++) {
        _history.PushFront(0);
      }
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
      float value = getValue();
      _preCullTicks = -1;

      if (float.IsInfinity(value) || float.IsNaN(value)) {
        return;
      }

      _smoothedValue.Update(value, Time.deltaTime);

      _sampleValue += value;
      _sampleIndex++;
      if (_sampleIndex < _samplesPerFrame) {
        return;
      }

      foreach (Graph graph in _graphs.Values) {
        graph.samples.PushFront(graph.currentSample);
        graph.currentSample = new Sample();
      }

      value = _sampleValue / _sampleIndex;
      _sampleIndex = 0;
      _sampleValue = 0;

      _history.PushFront(value);
      while (_history.Count > _historyLength) {
        _history.PopBack();
      }

      _slidingMax.AddValue(value);
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

    private float ticksToMs(long ticks) {
      return (float)(ticks / (System.Diagnostics.Stopwatch.Frequency / 1000.0));
    }

    private string msToString(float ms) {
      return (Mathf.Round(ms * 10) * 0.1f).ToString();
    }

    private float getGraphSpacing() {
      //TODO
      return 0;
    }

    private void UpdateTexture() {
      float max = _smoothedMax.value * 1.5f;

      for (int i = 0; i < _historyLength; i++) {
        float percent = Mathf.Clamp01(_history[i] / max);
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
      public Dequeue<Sample> samples;

      public Sample currentSample;
      public long inclusiveStart, exclusiveStart;

      public Graph(string name, GraphUnits units) {
        this.name = name;
        this.units = units;
        samples = new Dequeue<Sample>();
      }
    }

    protected struct Sample : IComparable<Sample> {
      public long totalInclusiveTicks;
      public long totalExclusiveTicks;

      public int CompareTo(Sample other) {
        return totalExclusiveTicks.CompareTo(other.totalExclusiveTicks);
      }
    }
  }
}
