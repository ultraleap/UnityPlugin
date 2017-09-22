//using System;
//using System.Collections;
//using System.Collections.Generic;
//using UnityEngine;

//namespace Leap.Unity.ZZOLD_GLINT.Internal {

//  /// <summary>
//  /// Singleton-pattern MonoBehaviour for tracking time and frames via normal Unity
//  /// events. For general use of Glint (GL Interop), refer to the Glint static class API.
//  /// </summary>
//  public class ZZOLD_GlintRequestRunner : MonoBehaviour {

//    private static List<ZZOLD_Request> _pendingRequests = new List<ZZOLD_Request>();
//    private static List<ZZOLD_Request> _readyRequests   = new List<ZZOLD_Request>();

//    private static ZZOLD_GlintRequestRunner _instance = null;
//    public static ZZOLD_GlintRequestRunner instance {
//      get {
//        if (_instance == null) {
//          GameObject obj = new GameObject("__Glint Request Runner__");
//          _instance = obj.AddComponent<ZZOLD_GlintRequestRunner>();
//        }
//        return _instance;
//      }
//    }
//    private static void ensureInstanceExists() { // sigh
//      if (_instance == null) { var _ = instance; }
//    }

//    public static void CreateRequest(Texture gpuTexture, float[] cpuData,
//                                     Action onRetrieved, int waitTimeInFrames) {
//      ensureInstanceExists();

//      var req = Pool<ZZOLD_Request>.Spawn();
//      req.status = ZZOLD_Status.Ready;
//      req.gpuTexture = gpuTexture;
//      req.cpuData = cpuData;
//      req.onRetrieved = onRetrieved;
//      req.waitTimeInFrames = waitTimeInFrames;
      
//      GlintPlugin.RequestTextureData(req.gpuTexture);

//      req.status = GlintPlugin.GetLastStatus();
//      if (req.status != ZZOLD_Status.Success_0_ReadyForRenderThreadRequest) {
//        Debug.LogError("[Glint] Aborting request for texture: " + req.gpuTexture + "; "
//                     + "expected status " + ZZOLD_Status.Success_0_ReadyForRenderThreadRequest
//                     + ", got " + req.status);
//      }
//      else {
//        _pendingRequests.Add(req);
//      }
//    }

//    private static void finalizeRequest(ZZOLD_Request req) {
//      req.Clear();
//      Pool<ZZOLD_Request>.Recycle(req);
//    }

//    private static List<ZZOLD_Request> _reqBuffer = new List<ZZOLD_Request>();
//    private static void staticUpdate() {

//      // All requests that are already pending are ready to have their data copied from
//      // native memory to managed memory.
//      foreach (var req in _readyRequests) {
//        // Retrieve, but also report how long each step took for profiling.
//        GlintPlugin.RetrieveTextureData(req.gpuTexture, ref req.cpuData,
//                                        out ZZOLD_Glint.Profiling.lastRenderMapMs,
//                                        out ZZOLD_Glint.Profiling.lastRenderCopyMs,
//                                        out ZZOLD_Glint.Profiling.lastMainCopyMs);

//        // Buffer for removal and recycle.
//        _reqBuffer.Add(req);

//        // Fire the Action!
//        req.onRetrieved();
//      }

//      // Remove any completed requests.
//      foreach (var req in _reqBuffer) {
//        finalizeRequest(req);
//      }
//      _readyRequests.Clear();
//      _reqBuffer.Clear();

//      // Decrement timers for pending requests, and fire retrieve operations, moving
//      // those requests to the ready requests.
//      foreach (var req in _pendingRequests) {
//        if (req.readyForRetrieval) {
//          // At this stage, we must IssuePluginEvent before we can actually get the
//          // data (the memory copy has to happen on the render thread).
//          GlintPlugin.RetrieveTextureData(req.gpuTexture, ref req.cpuData);
//          _reqBuffer.Add(req);
//        }
//      }

//      // Transfer buffered requests from pending to ready.
//      foreach (var req in _reqBuffer) {
//        _pendingRequests.Remove(req);
//        _readyRequests.Add(req);
//      }

//      _reqBuffer.Clear();
//    }

//    private static void staticLateUpdate() {
//      foreach (var req in _pendingRequests) {
//        req.TickFrame();
//      }
//    }

//    void Update() {
//      staticUpdate();
//    }

//    void LateUpdate() {
//      staticLateUpdate();
//    }

//  }

//}
