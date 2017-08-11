using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Leap.Unity.Glint.Internal {

  /// <summary>
  /// Singleton-pattern MonoBehaviour for tracking time and frames via normal Unity
  /// events. For general use of Glint (GL Interop), refer to the Glint static class API.
  /// </summary>
  public class GlintRequestRunner : MonoBehaviour {

    private static List<Request> _pendingRequests = new List<Request>();
    private static List<Request> _readyRequests = new List<Request>();

    private static GlintRequestRunner _instance = null;
    public static GlintRequestRunner instance {
      get {
        if (_instance == null) {
          GameObject obj = new GameObject("__Glint Request Runner__");
          _instance = obj.AddComponent<GlintRequestRunner>();
        }
        return _instance;
      }
    }
    private static void ensureInstanceExists() { // sigh
      if (_instance == null) { var unused = instance; }
    }

    public static void CreateRequest(Texture gpuTexture, float[] cpuData,
                                     Action onRetrieved, int waitTimeInFrames) {
      ensureInstanceExists();

      var req = Pool<Request>.Spawn();
      req.gpuTexture = gpuTexture;
      req.cpuData = cpuData;
      req.onRetrieved = onRetrieved;
      req.waitTimeInFrames = waitTimeInFrames;
      
      GlintPlugin.RequestTextureData(req.gpuTexture);

      _pendingRequests.Add(req);
    }


    private static void finalizeRequest(Request req) {
      req.Clear();
      Pool<Request>.Recycle(req);
    }

    private static List<Request> _reqBuffer = new List<Request>();
    private static void staticUpdate() {

      // All requests that are already pending are ready to have their data copied from
      // native memory to managed memory.
      foreach (var req in _readyRequests) {
        // Retrieve, but also report how long each step took for profiling.
        GlintPlugin.RetrieveTextureData(req.gpuTexture, ref req.cpuData,
                                        out Glint.Profiling.lastRenderMapMs,
                                        out Glint.Profiling.lastRenderCopyMs,
                                        out Glint.Profiling.lastMainCopyMs);

        // Buffer for removal and recycle.
        _reqBuffer.Add(req);

        // Fire the Action!
        req.onRetrieved();
      }

      // Remove any completed requests.
      foreach (var req in _reqBuffer) {
        finalizeRequest(req);
      }
      _readyRequests.Clear();
      _reqBuffer.Clear();

      // Decrement timers for pending requests, and fire retrieve operations, moving
      // those requests to the ready requests.
      foreach (var req in _pendingRequests) {
        if (req.readyForRetrieval) {
          // At this stage, we must IssuePluginEvent before we can actually get the
          // data (the memory copy has to happen on the render thread).
          GlintPlugin.RetrieveTextureData(req.gpuTexture, ref req.cpuData);
          _reqBuffer.Add(req);
        }
      }

      // Transfer buffered requests from pending to ready.
      foreach (var req in _reqBuffer) {
        _pendingRequests.Remove(req);
        _readyRequests.Add(req);
      }

      _reqBuffer.Clear();
    }

    private static void staticLateUpdate() {
      foreach (var req in _pendingRequests) {
        req.TickFrame();
      }
    }

    void Update() {
      staticUpdate();
    }

    void LateUpdate() {
      staticLateUpdate();
    }

  }

}
