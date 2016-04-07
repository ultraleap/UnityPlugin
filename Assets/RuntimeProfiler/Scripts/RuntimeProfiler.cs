using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using Leap.Unity;

public class RuntimeProfiler : MonoBehaviour {

  public static KeyCode toggleProfiler = KeyCode.P;
  public const float DIST_BETWEEN_RENDER_AND_UPDATE = 0.003f;

  [Tooltip("All frame times greater than this value will not result in larger bars.  In Miliseconds!")]
  [SerializeField]
  private float _frameTimeCap = 30.0f;

  [Tooltip("When the frame time is less than idealMaxFrameTime, color will come from this gradient.")]
  [SerializeField]
  private Gradient _colorGradient;

  [SerializeField]
  private Text _smoothedRenderTimeText;

  [SerializeField]
  private Text _smoothedUpdateTimeText;

  [SerializeField]
  private Text _smoothedSpikeText;

  [SerializeField]
  private Text _smoothedFrameTimeText;

  [Tooltip("Amount of smoothing to use for the smoothed times.")]
  [SerializeField]
  private float _smoothingDelay = 0.1f;

  [Tooltip("In Miliseconds!")]
  [SerializeField]
  private float _spikeThreshold = 17.0f;

  [Tooltip("In Seconds!")]
  [SerializeField]
  private float _spikeSmoothingDelay = 5.0f;

  [Tooltip("How many frames should this profiler visualize.")]
  [SerializeField]
  private int _frameResolution = 128;

  [SerializeField]
  private Renderer _profilerRenderer;

  private System.Diagnostics.Stopwatch _stopwatch = new System.Diagnostics.Stopwatch();

  private string[] _cachedStrings;

  private int _textureOffset;
  private Texture2D _profilerTexture;
  private Renderer _renderer;

  private Texture2D _gradientTexture;

  private bool _hasCompletedFrame = false;

  private long _prevEofTick = 0;
  private long _preRenderTick = 0;

  private long _totalRenderTick = 0;
  private long _totalFrameTick = 0;

  private long _updateStartTick = -1;
  private long _updateEndTick = -1;
  private long _totalUpdateTick = -1;

  private SmoothedFloat _smoothedFrameTime = new SmoothedFloat();
  private SmoothedFloat _smoothedRenderTime = new SmoothedFloat();
  private SmoothedFloat _smoothedUpdateTime = new SmoothedFloat();

  private long _lastSpikeTick = 0;
  private SmoothedFloat _smoothedSpikeRate = new SmoothedFloat();

  void OnValidate() {
    _frameTimeCap = Mathf.Max(1, _frameTimeCap);
    _smoothingDelay = Mathf.Max(0, _smoothingDelay);
    _spikeThreshold = Mathf.Max(0, _spikeThreshold);
    _spikeSmoothingDelay = Mathf.Max(0, _spikeSmoothingDelay);
    _frameResolution = Mathf.Max(1, _frameResolution);
  }

  void Awake() {
    _smoothedFrameTime.delay = _smoothingDelay;
    _smoothedRenderTime.delay = _smoothingDelay;
    _smoothedUpdateTime.delay = _smoothingDelay;
    _smoothedSpikeRate.delay = _spikeSmoothingDelay;

    _profilerTexture = new Texture2D(_frameResolution, 1, TextureFormat.ARGB32, false, true);
    _profilerTexture.filterMode = FilterMode.Point;
    _profilerTexture.wrapMode = TextureWrapMode.Repeat;

    Color32[] black = new Color32[_frameResolution];
    for (int i = 0; i < _frameResolution; i++) {
      black[i] = Color.black;
    }

    _profilerTexture.SetPixels32(black);
    _profilerTexture.Apply();

    _profilerRenderer.material.mainTexture = _profilerTexture;

    _gradientTexture = new Texture2D(256, 1, TextureFormat.ARGB32, false, true);
    _gradientTexture.filterMode = FilterMode.Bilinear;
    _gradientTexture.wrapMode = TextureWrapMode.Clamp;

    Color32[] colors = new Color32[256];
    for (int i = 0; i < 256; i++) {
      colors[i] = _colorGradient.Evaluate(i / 256.0f);
    }

    _gradientTexture.SetPixels32(colors);
    _gradientTexture.Apply();

    _profilerRenderer.material.SetTexture("_Ramp", _gradientTexture);

    _cachedStrings = new string[500];
    for (int i = 0; i < 500; i++) {
      _cachedStrings[i] = (i / 10.0f).ToString();
    }
  }

  void OnEnable() {
    _stopwatch.Reset();
    _stopwatch.Start();
    StartCoroutine(endOfFrameCoroutine());

    Camera.onPreCull += onPreCull;
    Camera.onPostRender += onPostRender;

    _hasCompletedFrame = false;

    _smoothedFrameTime.reset = true;
    _smoothedRenderTime.reset = true;
    _smoothedUpdateTime.reset = true;
  }

  void OnDisable() {
    _stopwatch.Stop();
    StopAllCoroutines();

    Camera.onPreCull -= onPreCull;
    Camera.onPostRender -= onPostRender;
  }

  void FixedUpdate() {
    if (_updateStartTick == -1) {
      _updateStartTick = _stopwatch.ElapsedTicks;
    }
  }

  void Update() {
    if (_hasCompletedFrame) {
      float totalRenderMilis = _totalRenderTick / (float)System.Diagnostics.Stopwatch.Frequency * 1000.0f;
      float totalUpdateMilis = _totalUpdateTick / (float)System.Diagnostics.Stopwatch.Frequency * 1000.0f;
      float totalFrameMilis = _totalFrameTick / (float)System.Diagnostics.Stopwatch.Frequency * 1000.0f;

      bool isSpike = totalFrameMilis > _spikeThreshold;
      if (isSpike) {
        _lastSpikeTick = _stopwatch.ElapsedTicks;
      }
      long ticksSinceSpike = (_stopwatch.ElapsedTicks - _lastSpikeTick);
      float secondsSinceSpike = ticksSinceSpike / (float)System.Diagnostics.Stopwatch.Frequency;

      float renderPercent = totalRenderMilis / _frameTimeCap;
      float updatePercent = totalUpdateMilis / _frameTimeCap;
      float framePercent = totalFrameMilis / _frameTimeCap;

      _profilerTexture.SetPixel(_textureOffset, 0, new Color(renderPercent, updatePercent, framePercent));
      _profilerTexture.Apply();

      _textureOffset = (_textureOffset + 1) % _frameResolution;
      _profilerRenderer.material.SetFloat("_Offset", _textureOffset / (float)_frameResolution);

      setSmoothedText(_smoothedFrameTimeText, _smoothedFrameTime, totalFrameMilis);
      setSmoothedText(_smoothedRenderTimeText, _smoothedRenderTime, totalRenderMilis);
      setSmoothedText(_smoothedUpdateTimeText, _smoothedUpdateTime, totalUpdateMilis);
      setSmoothedText(_smoothedSpikeText, _smoothedSpikeRate, secondsSinceSpike);
    }

    _totalRenderTick = 0;
    _totalFrameTick = 0;
  }

  private void onPreCull(Camera camera) {
    _preRenderTick = _stopwatch.ElapsedTicks;

    if (_updateEndTick == -1 && _updateStartTick != -1) {
      _updateEndTick = _preRenderTick;
    }
  }

  private void onPostRender(Camera camera) {
    _totalRenderTick += _stopwatch.ElapsedTicks - _preRenderTick;
  }

  private IEnumerator endOfFrameCoroutine() {
    var eof = new WaitForEndOfFrame();
    while (true) {
      yield return eof;
      long eofTick = _stopwatch.ElapsedTicks;

      if (_hasCompletedFrame) {
        _totalFrameTick = eofTick - _prevEofTick;
      }

      if (_updateEndTick != -1 && _updateStartTick != -1) {
        _totalUpdateTick = _updateEndTick - _updateStartTick;
        _updateStartTick = -1;
        _updateEndTick = -1;
      }

      _hasCompletedFrame = true;
      _prevEofTick = eofTick;
    }
  }

  private void setSmoothedText(Text text, SmoothedFloat smoothedTime, float time) {
    smoothedTime.Update(time, Time.deltaTime);
    if (text != null) {
      int index = Mathf.Clamp(Mathf.RoundToInt(smoothedTime.value * 10), 0, _cachedStrings.Length - 1);
      text.text = _cachedStrings[index];
    }
  }
}
