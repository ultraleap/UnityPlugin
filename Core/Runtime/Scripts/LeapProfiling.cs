/******************************************************************************
 * Copyright (C) Ultraleap, Inc. 2011-2020.                                   *
 *                                                                            *
 * Use subject to the terms of the Apache License 2.0 available at            *
 * http://www.apache.org/licenses/LICENSE-2.0, or another agreement           *
 * between Ultraleap and you, your company or other organization.             *
 ******************************************************************************/

using System.Threading;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Profiling;

namespace Leap.Unity {

  /// <summary>
  /// Utility class used by the LeapServiceProvider for profiling the LeapCSharp dll
  /// </summary>
  public static class LeapProfiling {

    //Maps a block name to the sampler for it
    private static Dictionary<string, CustomSampler> _samplers = new Dictionary<string, CustomSampler>();

    //Represents a queue of samplers that need to be created.
    //Samplers can only be created on the main thread, so we need some way for the info given
    //on alternate threads to be passed to the main thread.
    private static Queue<string> _samplersToCreate = new Queue<string>();

    //We keep track of the size of the queue in a member variable
    private static int _samplersToCreateCount = 0;

    public static void Update() {
      //Read of _samplersToCreateCount is atomic
      if (_samplersToCreateCount > 0) {
        //Only if the count is nonzero do we do an expensive lock 
        lock (_samplersToCreate) {
          //First duplicate the existing dictionary
          var newDictionary = new Dictionary<string, CustomSampler>(_samplers);

          //Then construct all of the new samplers and add them to the new dictionary
          while (_samplersToCreate.Count > 0) {
            string blockName = _samplersToCreate.Dequeue();

            newDictionary[blockName] = CustomSampler.Create(blockName);
          }

          //Reset samplers to create to zero
          _samplersToCreateCount = 0;

          //Reference assignments are atomic in C#
          //All new callbacks will now reference the updated dictionary
          //Old dictionary will be collected by GC
          _samplers = newDictionary;
        }
      }
    }

    public static void BeginProfilingForThread(BeginProfilingForThreadArgs eventData) {
#if UNITY_2017_3_OR_NEWER
      //Enable unity profiling for this thread
      Profiler.BeginThreadProfiling("LeapCSharp", eventData.threadName);

      //Assume that threads are not stopping and starting frequently
      //so we can get away with less-than-optimal strategies when starting a thread.
      //in this case we use a naive queue with a lock.
      lock (_samplersToCreate) {
        foreach (var blockName in eventData.blockNames) {
          _samplersToCreate.Enqueue(blockName);
        }

        Interlocked.Add(ref _samplersToCreateCount, eventData.blockNames.Length);
      }
#else
      Debug.LogWarning("Thread Profiling is unavailable in versions of Unity below 2017.3");
#endif
    }

    public static void EndProfilingForThread(EndProfilingForThreadArgs eventData) {
#if UNITY_2017_3_OR_NEWER
      Profiler.EndThreadProfiling();
#else
      Debug.LogWarning("Thread Profiling is unavailable in versions of Unity below 2017.3");
#endif
    }

    public static void BeginProfilingBlock(BeginProfilingBlockArgs eventData) {
      //Sampler might not have been created yet because samplers can only be created
      //on the main thread.  We will simply not be able to report all blocks until
      //a sampler is available.

      //Note that the Dictionary type is thread safe for read operations
      //Dictionary is only used once and so there is no risk of the dictionary 
      //being swapped out from underneath us

      CustomSampler sampler;
      if (_samplers.TryGetValue(eventData.blockName, out sampler)) {
        sampler.Begin();
      }
    }

    public static void EndProfilingBlock(EndProfilingBlockArgs eventData) {
      CustomSampler sampler;
      if (_samplers.TryGetValue(eventData.blockName, out sampler)) {
        sampler.End();
      }
    }
  }
}
