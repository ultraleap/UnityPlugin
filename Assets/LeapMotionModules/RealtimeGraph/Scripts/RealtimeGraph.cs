using UnityEngine;
using UnityEngine.UI;
using System;
using System.Collections;
using System.Collections.Generic;

namespace Leap.Unity.RealtimeGraph {

  public class RealtimeGraph : MonoBehaviour {

    public enum GraphType {
      Framerate,
      FrameDelta,
      RenderDelta,
      UpdateDelta,
      TrackingFramerate,
      TrackingLatency
    }

    [SerializeField]
    private GraphType _graphType;

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

    protected Stack<GameObject> _customSampleLabelPool = new Stack<GameObject>();
    protected Dequeue<GameObject> _customSampleLabelActive = new Dequeue<GameObject>();

    protected Dequeue<List<CustomSample>> _customSampleBuffer;
    protected Dictionary<string, CustomSample> _completedSamples = new Dictionary<string, CustomSample>();
    protected Stack<CustomSample> _currentSamples = new Stack<CustomSample>();

    public void BeginCustomSample(string sampleName) {
      long currTicks = _stopwatch.ElapsedTicks;

      if (_currentSamples.Count != 0) {
        CustomSample currentSample = _currentSamples.Peek();
        currentSample.totalExclusiveTicks += currTicks - currentSample.exclusiveStart;
      }

      CustomSample sample;
      if (!_completedSamples.TryGetValue(sampleName, out sample)) {
        sample = CustomSample.Create(sampleName);
        sample.name = sampleName;
      }

      sample.exclusiveStart = sample.inclusiveStart = currTicks;

      _currentSamples.Push(sample);
    }

    public void EndCustomSample() {
      long currTicks = _stopwatch.ElapsedTicks;

      var sample = _currentSamples.Pop();
      sample.totalInclusiveTicks = currTicks - sample.inclusiveStart;
      sample.totalExclusiveTicks += currTicks - sample.exclusiveStart;

      if (_currentSamples.Count != 0) {
        var nextSample = _currentSamples.Peek();
        nextSample.exclusiveStart = currTicks;
      }
    }

    protected virtual void OnValidate() {
      _historyLength = Mathf.Max(1, _historyLength);
      _updatePeriod = Mathf.Max(1, _updatePeriod);

      if (_texture != null) {
        SwitchGraph(_graphType);
      }
    }

    protected virtual void Awake() {
      _history = new Dequeue<float>(_historyLength);
      _slidingMax = new SlidingMax(_historyLength);
      _customSampleBuffer = new Dequeue<List<CustomSample>>(_historyLength);

      _smoothedMax = new SmoothedFloat();
      _smoothedMax.delay = _maxSmoothingDelay;

      _smoothedValue = new SmoothedFloat();
      _smoothedValue.delay = _valueSmoothingDelay;

      for (int i = 0; i < _historyLength; i++) {
        _history.PushFront(0);
        _customSampleBuffer.PushFront(new List<CustomSample>());
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

      SwitchGraph(_graphType);

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

      List<CustomSample> samplesForFrame;
      {
        List<CustomSample> expiredSamples;
        _customSampleBuffer.PopBack(out expiredSamples);

        for (int i = 0; i < expiredSamples.Count; i++) {
          CustomSample.Recycle(expiredSamples[i]);
        }
        expiredSamples.Clear();
        samplesForFrame = expiredSamples;
      }

      List<CustomSample> newSamples = samplesForFrame;
      samplesForFrame.AddRange(_completedSamples.Values);
      for (int i = 0; i < samplesForFrame.Count; i++) {
        samplesForFrame[i].totalExclusiveTicks /= _sampleIndex;
        samplesForFrame[i].totalInclusiveTicks /= _sampleIndex;
      }

      _customSampleBuffer.PushFront(samplesForFrame);

      updateCustomSamplePanel(samplesForFrame);

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

    public void SwitchGraph(GraphType type) {
      _graphType = type;

      titleLabel.text = Enum.GetName(typeof(GraphType), _graphType);
    }

    public void SwitchGraph(string name) {
      GraphType newType = (GraphType)Enum.Parse(typeof(GraphType), name);
      SwitchGraph(newType);
    }

    public void NextGraph() {
      GraphType nextType = (GraphType)(((int)_graphType + 1) % Enum.GetNames(typeof(GraphType)).Length);
      SwitchGraph(nextType);
    }

    public void PrevGraph() {
      int count = Enum.GetNames(typeof(GraphType)).Length;
      GraphType nextType = (GraphType)(((int)_graphType - 1 + count) % count);
      SwitchGraph(nextType);
    }

    private float getValue() {
      switch (_graphType) {
        case GraphType.RenderDelta:
          return getRenderMs();
        case GraphType.FrameDelta:
          return getFrameMs();
        case GraphType.Framerate:
          return 1000.0f / getFrameMs();
        case GraphType.UpdateDelta:
          return getUpdateMs();
        case GraphType.TrackingLatency:
          return (_provider.GetLeapController().Now() - _provider.CurrentFrame.Timestamp) / 1000.0f;
        case GraphType.TrackingFramerate:
          return _provider.CurrentFrame.CurrentFramesPerSecond;
        default:
          throw new Exception("Unexpected graph type");
      }
    }

    private float getGraphSpacing() {
      switch (_graphType) {
        case GraphType.FrameDelta:
        case GraphType.RenderDelta:
        case GraphType.UpdateDelta:
        case GraphType.TrackingLatency:
          return _deltaLineSpacing;
        case GraphType.Framerate:
        case GraphType.TrackingFramerate:
          return _framerateLineSpacing;
        default:
          throw new Exception("Unexpected graph type");
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

    private void updateCustomSamplePanel(List<CustomSample> unorderedSamples) {
      unorderedSamples.Sort();

      //Pool sample objects that are not needed
      while (_customSampleLabelActive.Count > unorderedSamples.Count) {
        GameObject activeObj;
        _customSampleLabelActive.PopBack(out activeObj);

        activeObj.SetActive(false);
        _customSampleLabelPool.Push(activeObj);
      }

      //Enable sample objects that are needed
      while (_customSampleLabelActive.Count < unorderedSamples.Count) {
        GameObject sampleObj;
        if (_customSampleLabelPool.Count > 0) {
          sampleObj = _customSampleLabelPool.Pop();
          sampleObj.SetActive(true);
        } else {
          sampleObj = Instantiate(customSamplePrefab);
          sampleObj.transform.SetParent(customSamplePrefab.transform.parent);
          sampleObj.SetActive(true);
        }

        _customSampleLabelActive.PushFront(sampleObj);
      }

      //Update sample objects
      for (int i = 0; i < unorderedSamples.Count; i++) {
        GameObject sampleObj = _customSampleLabelActive[i];
        sampleObj.transform.SetSiblingIndex(i);

        Text sampleText = sampleObj.GetComponentInChildren<Text>();
        sampleText.text = unorderedSamples[i].name + "\n";
        sampleText.text += "inclusive: " + msToString(ticksToMs(unorderedSamples[i].totalInclusiveTicks)) + " ms\n";
        sampleText.text += "exclusive: " + msToString(ticksToMs(unorderedSamples[i].totalExclusiveTicks)) + " ms";
      }
    }

    protected class CustomSample : IComparable<CustomSample> {
      private static Queue<CustomSample> _pool = new Queue<CustomSample>();

      public string name;
      public long totalInclusiveTicks;
      public long totalExclusiveTicks;

      public long inclusiveStart, exclusiveStart;

      private CustomSample() { }

      public static CustomSample Create(string name) {
        CustomSample sample;
        if (_pool.Count == 0) {
          sample = new CustomSample();
        } else {
          sample = _pool.Dequeue();
        }

        //init
        sample.name = name;
        return sample;
      }

      public static void Recycle(CustomSample sample) {
        _pool.Enqueue(sample);
      }

      public int CompareTo(CustomSample other) {
        return totalExclusiveTicks.CompareTo(other.totalExclusiveTicks);
      }
    }
  }
}
