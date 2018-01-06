using System;
using System.Threading;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Profiling;

namespace Leap.Unity {

  [RequireComponent(typeof(LeapServiceProvider))]
  public class LeapProfiling : MonoBehaviour {

    private Dictionary<string, CustomSampler> _samplers = new Dictionary<string, CustomSampler>();

    private int _samplersToCreateCount = 0;
    private Queue<string> _samplersToCreate = new Queue<string>();

    private bool _isEnabled;
    private int _activeThreads = 0;

    private void OnEnable() {
      var provider = GetComponent<LeapServiceProvider>();
      if (provider == null) {
        Debug.LogError("LeapProfiling component must be placed next to a LeapServiceProvider.");
        enabled = false;
        return;
      }

      var controller = provider.GetLeapController();
      if (controller == null) {
        Debug.LogError("Could not get a reference to the leap controller, profiling will not be performed.");
        enabled = false;
        return;
      }

      controller.BeginProfilingForThread += beginProfilingForThread;
      controller.EndProfilingForThread += endProfilingForThread;
      controller.BeginProfilingBlock += beginProfilingBlock;
      controller.EndProfilingBlock += endProfilingBlock;

      _isEnabled = true;
    }

    private void OnDisable() {
      var provider = GetComponent<LeapServiceProvider>();
      if (provider == null) {
        return;
      }

      var controller = provider.GetLeapController();
      if (controller == null) {
        return;
      }

      controller.BeginProfilingForThread -= beginProfilingForThread;
      controller.EndProfilingForThread -= endProfilingForThread;
      controller.BeginProfilingBlock -= beginProfilingBlock;
      controller.EndProfilingBlock -= endProfilingBlock;

      _isEnabled = false;
    }

    private void Update() {
      //Read of _samplersToCreateCount is atomic
      if (_samplersToCreateCount > 0) {
        lock (_samplersToCreate) {
          //First duplicate the dictionary
          var newDictionary = new Dictionary<string, CustomSampler>(_samplers);

          while (_samplersToCreate.Count > 0) {
            string blockName = _samplersToCreate.Dequeue();

            newDictionary[blockName] = CustomSampler.Create(blockName);
          }

          //Reset samplers to create to zero
          _samplersToCreateCount = 0;

          //Reference assignments are atomic in C#
          //All new callbacks will now reference the updated dictionary
          _samplers = newDictionary;
        }
      }
    }

    private void beginProfilingForThread(BeginProfilingForThreadArgs eventData) {
      Debug.Log("BEGIN:" + Thread.CurrentThread.ManagedThreadId);

      lock (_samplersToCreate) {
        foreach (var blockName in eventData.blockNames) {
          _samplersToCreate.Enqueue(blockName);
        }

        Interlocked.Add(ref _samplersToCreateCount, eventData.blockNames.Length);
      }
    }

    private void endProfilingForThread(EndProfilingForThreadArgs eventData) {
    }

    private void beginProfilingBlock(BeginProfilingBlockArgs eventData) {
      Profiler.BeginThreadProfiling("LeapCSharp", "Worker Thread");

      //Sampler might not have been created yet because samplers can only be created
      //on the main thread.
      CustomSampler sampler;
      if (_samplers.TryGetValue(eventData.blockName, out sampler)) {
        sampler.Begin();
      }
    }

    private void endProfilingBlock(EndProfilingBlockArgs eventData) {
      CustomSampler sampler;
      if (_samplers.TryGetValue(eventData.blockName, out sampler)) {
        sampler.End();
      }
    }
  }
}
