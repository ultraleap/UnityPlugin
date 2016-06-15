using UnityEngine;
using UnityEngine.UI;
using System;
using System.Linq;
using System.Collections;

namespace Leap.Unity.RealtimeGraph {

  public class RealtimeGraph : MonoBehaviour {

    public enum GraphType {
      Framerate,
      FrameDelta,
      RenderDelta,
      UpdateDelta,
      TrackingFramerate,
      TrackingDelta,
      TrackingLatency
    }

    [Serializable]
    public class GradientMaximum {
      public GraphType graphType;
      public float crossoverPoint;
      public float crossoverTolerance;
      public bool isHigherBetter;
    }

    [SerializeField]
    private GraphType _graphType;

    [SerializeField]
    private GradientMaximum[] _gradientMaximums;

    [SerializeField]
    protected int _historyLength = 128;

    [SerializeField]
    protected int _updatePeriod = 10;

    [SerializeField]
    protected float _maxSmoothingDelay = 0.1f;

    [SerializeField]
    protected Shader _graphShader;

    [SerializeField]
    protected Renderer _graphRenderer;

    [SerializeField]
    protected LeapServiceProvider _provider;

    [SerializeField]
    protected Text upperValueLabel;

    [SerializeField]
    protected Text midValueLabel;

    [Header("Gradient Settings")]
    [SerializeField]
    protected int _gradientResolution = 128;

    protected System.Diagnostics.Stopwatch _stopwatch = new System.Diagnostics.Stopwatch();
    protected long _preCullTicks, _postRenderTicks;
    protected long _endOfFrameTicks;

    protected long _updateTicks;
    protected long _frameTicks;

    protected Dequeue<float> _history;
    protected SlidingMax _slidingMax;

    protected GradientMaximum _gradientSetting;
    protected Texture2D _texture;
    protected Texture2D _gradientTexture;
    protected Color32[] _colors;
    protected SmoothedFloat _smoothedMax;

    protected virtual void OnValidate() {
      _historyLength = Mathf.Max(1, _historyLength);
      _updatePeriod = Mathf.Max(1, _updatePeriod);

      if (_texture != null) {
        UpdateTexture();
      }
    }

    protected virtual void Awake() {
      _history = new Dequeue<float>();
      _slidingMax = new SlidingMax(_historyLength);
      _smoothedMax = new SmoothedFloat();
      _smoothedMax.delay = _maxSmoothingDelay;
    }

    protected virtual void Start() {
      _texture = new Texture2D(_historyLength, 1, TextureFormat.Alpha8, false, true);
      _texture.filterMode = FilterMode.Point;
      _texture.wrapMode = TextureWrapMode.Clamp;
      _colors = new Color32[_historyLength];

      _gradientTexture = new Texture2D(_gradientResolution, 1, TextureFormat.ARGB32, false, false);
      _gradientTexture.wrapMode = TextureWrapMode.Repeat;
      _gradientTexture.filterMode = FilterMode.Bilinear;

      //_graphRenderer.material = new Material(_graphShader);
      _graphRenderer.material.SetTexture("_GraphTexture", _texture);
      _graphRenderer.material.SetTexture("_Gradient", _gradientTexture);

      SwitchGraphType(_graphType);

      StartCoroutine(endOfFrameWaiter());

      _stopwatch.Start();
    }

    public void SwitchGraphType(GraphType graphType) {
      _graphType = graphType;

      _gradientSetting = _gradientMaximums.FirstOrDefault(g => g.graphType == graphType);

      float maxGradient = _gradientSetting.crossoverPoint * 2;
      float crossoverPercent = _gradientSetting.crossoverTolerance / maxGradient;
      float startCross = 0.5f - crossoverPercent * 2;
      float endCross = 0.5f + crossoverPercent * 2;

      for (int i = 0; i < _gradientResolution; i++) {
        float percent = i / (float)_gradientResolution;
        Color color;
        if (percent < startCross) {
          color = Color.green;
        } else if (percent < endCross) {
          color = Color.Lerp(Color.green, Color.yellow, Mathf.InverseLerp(startCross, endCross, percent));
        } else {
          color = Color.Lerp(Color.red, Color.red * 0.5f, Mathf.InverseLerp(endCross, 1, percent));
        }
        _gradientTexture.SetPixel(i, 0, color);
      }
      _gradientTexture.Apply();
    }

    protected virtual void Update() {
      float value = getValue();
      if (float.IsInfinity(value) || float.IsNaN(value)) {
        return;
      }

      _history.PushFront(value);
      while (_history.Count > _historyLength) {
        _history.PopBack();
      }

      _slidingMax.AddValue(value);
      _smoothedMax.Update(_slidingMax.Max, Time.deltaTime);

      if ((Time.frameCount % _updatePeriod) == 0) {
        UpdateTexture();
      }
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
        default:
          throw new Exception("asd");
      }
    }

    private IEnumerator endOfFrameWaiter() {
      WaitForEndOfFrame waiter = new WaitForEndOfFrame();
      while (true) {
        yield return waiter;
        long ticks = _stopwatch.ElapsedTicks;
        _frameTicks = ticks - _endOfFrameTicks;
        _endOfFrameTicks = ticks;
      }
    }

    private void onPreCull() {
      if (_preCullTicks != -1) {
        _preCullTicks = _stopwatch.ElapsedTicks;
        _updateTicks = _preCullTicks - _endOfFrameTicks;
      }
    }

    private void onPostRender() {
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
      return (float)(ticks / (double)(System.Diagnostics.Stopwatch.Frequency / 1000.0));
    }

    private void UpdateTexture() {
      float max = _smoothedMax.value;

      for (int i = 0; i < _historyLength; i++) {
        float percent = Mathf.Clamp01(_history[i] / max);
        byte percentByte = (byte)(percent * 255.9999f);
        _colors[i] = new Color32(percentByte, percentByte, percentByte, percentByte);
      }

      _graphRenderer.material.SetFloat("_GradientScale", 50 * max / (_gradientSetting.crossoverPoint * 2) * (_gradientSetting.isHigherBetter ? -1 : 1));

      _texture.SetPixels32(_colors);
      _texture.Apply();

      upperValueLabel.text = Mathf.Round(max).ToString();
      midValueLabel.text = Mathf.Round(max * 0.5f).ToString();
    }
  }
}
