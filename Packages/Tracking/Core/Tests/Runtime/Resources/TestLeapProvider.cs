using Leap;
using Leap.Unity;
using System;
using System.IO;
using UnityEngine;

namespace Leap.Testing
{
    /// <summary>
    /// A leap provider that returns hand tracking data for the referenced serialized data, usually used for testing 
    /// </summary>
    public class TestLeapProvider : PostProcessProvider
    {
        private string _frameFileRepository;
        private string _frameFileName;
        private bool _dataSourceIsDirty;
        private Frame _frameData;

        /// <summary>
        /// Path to the location for the serialized frame data
        /// </summary>
        public string FrameFileRepository
        {
            get
            {
                return _frameFileRepository;
            }

            set
            {
                if (_frameFileRepository != value)
                {
                    _dataSourceIsDirty = true;
                    _frameFileRepository = value;
                }
            }
        }

        /// <summary>
        /// Name of the serialized frame data to use
        /// </summary>
        public string FrameFileName
        {
            get
            {
                return _frameFileName;
            }

            set
            {
                if (_frameFileName != value)
                {
                    _dataSourceIsDirty = true;
                    _frameFileName = value;
                }
            }
        }

        private string FrameFileFullPath
        {
            get
            {
                if (!String.IsNullOrEmpty(FrameFileRepository) && !String.IsNullOrEmpty(FrameFileName))
                {
                    return Path.GetFullPath(Path.Combine(FrameFileRepository + FrameFileName));
                }

                return String.Empty;
            }
        }
        public override void ProcessFrame(ref Frame inputFrame)
        {
            if (Application.isPlaying)
            {
                if (_dataSourceIsDirty)
                {
                    _frameData = LoadFrame(FrameFileFullPath);

                    if (_frameData != null)
                    {
                        inputFrame.CopyFrom(_frameData);
                        _dataSourceIsDirty = false;
                    }
                }
                else
                {
                    if (_frameData != null)
                    {
                        inputFrame.CopyFrom(_frameData);
                    }
                }
            }
        }

        private Frame LoadFrame(string filePath)
        {
            try
            {
                return LeapFrameRecorder.LoadFrame(filePath);
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
                return null;
            }
        }
    }
}