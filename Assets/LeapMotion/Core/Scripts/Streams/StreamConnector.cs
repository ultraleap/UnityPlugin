using Leap.Unity.Attributes;
using Leap.Unity.Infix;
using Leap.Unity.RuntimeGizmos;
using UnityEngine;

namespace Leap.Unity.Streams {

  public abstract class StreamConnector : MonoBehaviour {

    /// <summary>
    /// Gets the data type flowing through the input IStream and into the connectors's
    /// "output" type, an IStreamReceiver.
    /// </summary>
    public abstract System.Type streamDataType { get; }

    /// <summary>
    /// Gets the bounded generic type for the input IStream, whose generic type argument
    /// MUST match streamDataType.
    /// </summary>
    public abstract System.Type streamType { get; }

    /// <summary>
    /// Gets the bounded generic type for the connector's output type IStreamReceiver,
    /// whose generic type argument MUST match streamDataType.
    /// </summary>
    public abstract System.Type streamReceiverType { get; }
  }

  [ExecuteInEditMode]
  public abstract class StreamConnector<StreamType> : StreamConnector,
                                                      IRuntimeGizmoComponent,
                                                      ISerializationCallbackReceiver {

    [SerializeField]
    // Sadly, this doesn't compile (name isn't constant), so we'll have to
    // use a Custom Editor instead
    //[ImplementsInterface(typeof(IStream<StreamType>).AssemblyQualifiedName)]
    protected MonoBehaviour _stream;
    public IStream<StreamType> stream {
      get { return _stream as IStream<StreamType>; }
    }

    [SerializeField]
    // Sadly, this doesn't compile (name isn't constant), so we'll have to
    // use a Custom Editor instead
    //[ImplementsInterface(typeof(IStreamReceiver<StreamReceiverType>)
    //                       .AssemblyQualifiedName)]
    protected MonoBehaviour _receiver;
    public IStreamReceiver<StreamType> receiver {
      get { return _receiver as IStreamReceiver<StreamType>; }
    }

    public void OnBeforeSerialize() {

    }

    public void OnAfterDeserialize() {
      // GO GO GO!
      initCallbacks();
    }

    #region StreamConnector

    /// <summary>
    /// Gets the Type corresponding to the IStream and IStreamReceiver type arguments
    /// that this Connector connects.
    /// </summary>
    public override System.Type streamDataType {
      get { return typeof(StreamType); }
    }

    /// <summary>
    /// Gets the bounded generic type for the input IStream, whose generic type argument
    /// matches streamDataType.
    /// </summary>
    public override System.Type streamType {
      get { return typeof(IStream<StreamType>); }
    }

    /// <summary>
    /// Gets the bounded generic type for the connector's output type IStreamReceiver,
    /// whose generic type argument matches streamDataType.
    /// </summary>
    public override System.Type streamReceiverType {
      get { return typeof(IStreamReceiver<StreamType>); }
    }

    #endregion

    public bool drawWire = true;

    [OnEditorChange("ResetConnection")]
    public bool debugWire = false;

    protected void ResetConnection() {
      OnDisable();
      OnEnable();
    }

    private void Awake() {
      initCallbacks();
    }

    private void OnEnable() {
      initCallbacks();
    }

    private void initCallbacks() {
      if (stream != null && receiver != null) {

        stream.OnOpen -= receiver.Open;
        stream.OnOpen += receiver.Open;

        stream.OnClose -= receiver.Close;
        stream.OnClose += receiver.Close;

        stream.OnSend -= receiver.Receive;
        stream.OnSend += receiver.Receive;

        #if UNITY_EDITOR
        if (debugWire) {
          stream.OnOpen -= debugOnOpen;
          stream.OnOpen += debugOnOpen;

          stream.OnClose -= debugOnClose;
          stream.OnClose += debugOnClose;

          stream.OnSend -= debugOnSend;
          stream.OnSend += debugOnSend;
        }
        #endif
      }
    }

    private void debugOnOpen() {
      Debug.Log("Wire " + this.name + " received OnOpen.");
    }
    private void debugOnSend(StreamType data) {
      Debug.Log("Wire " + this.name + " received OnSend: " + data.ToString());
    }
    private void debugOnClose() {
      Debug.Log("Wire " + this.name + " received OnClose.");
    }

    private void OnDisable() {
      if (stream != null && receiver != null) {
        stream.OnOpen -= receiver.Open;

        stream.OnClose -= receiver.Close;

        stream.OnSend -= receiver.Receive;
      }

      #if UNITY_EDITOR
      if (stream != null) {
        stream.OnOpen -= debugOnOpen;
        stream.OnClose -= debugOnClose;
        stream.OnSend -= debugOnSend;
      }
      #endif
    }
    
    /// <summary> Debug wire flash period in seconds. </summary>
    private static float DEBUG_WIRE_FLASH_PERIOD = 0.3f;

    public void OnDrawRuntimeGizmos(RuntimeGizmoDrawer drawer) {
      if (!this.enabled || !this.gameObject.activeInHierarchy) return;

      if (_stream != null && _receiver != null) {
        // No wire to render if the stream and receiver components are on the same object.
        if (_stream.gameObject == _receiver.gameObject) return;

        float time = 0f;
        if (Application.isPlaying) {
          time = Time.time;
        }
        var flashAmount = (Mathf.Sin((time % DEBUG_WIRE_FLASH_PERIOD) * Mathf.PI * 2)
                         + 1) / 2f;

        var a = _stream.transform.position;
        var b = _receiver.transform.position;

        if (drawWire) {
          drawer.color = LeapColor.white.WithAlpha(0.3f);
          drawer.DrawLine(a, b);
        }

        if (debugWire) {
          drawer.color = LeapColor.magenta.Lerp(LeapColor.white, 0.4f)
                                  .WithAlpha(flashAmount);
          drawer.DrawDashedLine(a, b, segmentsPerMeter: 128);
        }
      }
    }
  }

}
