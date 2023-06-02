using Leap;
using Leap.Unity;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using UnityEngine;
using static Leap.Bone;
using static Leap.Finger;

namespace Leap.Testing
{
    /// <summary>
    /// Use to capture and serialize a hand tracking frame (for unit testing purposes only)
    /// </summary>
    public class LeapFrameRecorder : MonoBehaviour
    {
        [SerializeField]
        private LeapProvider _leapProvider;

        [SerializeField]
        private string _fileDestination;

        private SerializableFrame _frame;

        // Update is called once per frame
        void Update()
        {
            if (Input.GetKeyDown(KeyCode.Space))
            {
                _frame = _leapProvider.CurrentFrame;
                SaveFrame();
                Debug.Log("File written to: " + _fileDestination);
            }
        }

        private void SaveFrame()
        {
            FileStream file;

            if (File.Exists(_fileDestination))
            {
                file = File.OpenWrite(_fileDestination);
            }
            else
            {
                file = File.Create(_fileDestination);
            }

            using (file)
            {
                // Use of a BinaryFormatter is acceptable here as this data should only be used for testing
                // This should be changed if someone uses this outside unit testing
                // https://learn.microsoft.com/en-us/dotnet/standard/serialization/binaryformatter-security-guide
                BinaryFormatter bf = new BinaryFormatter();
                bf.AssemblyFormat = System.Runtime.Serialization.Formatters.FormatterAssemblyStyle.Simple;
                bf.Serialize(file, _frame);
            }
        }

        /// <summary>
        /// Deserializes a hand tracking frame from a file
        /// </summary>
        /// <param name="filePath">Path to the file</param>
        /// <returns>A Frame</returns>
        public static Frame LoadFrame(string filePath)
        {
            if (!string.IsNullOrEmpty(filePath))
            {
                if (File.Exists(filePath))
                {
                    SerializableFrame frameS;

                    using (var file = File.OpenRead(filePath))
                    {
                        // Use of a BinaryFormatter is acceptable here as this data should only be used for testing
                        // This should be changed if someone uses this outside unit testing
                        // https://learn.microsoft.com/en-us/dotnet/standard/serialization/binaryformatter-security-guide
                        var bf = new BinaryFormatter();
                        frameS = (SerializableFrame) bf.Deserialize(file);
                    }

                    return frameS;
                }
                else
                {
                    Debug.Log("File not found at: " + filePath);
                }
            }

            return null;
        }
    }

    [System.Serializable]
    public class SerializableFrame
    {
        private long Id;
        private long Timestamp;
        private float CurrentFramesPerSecond;
        private List<SerializableHand> Hands;
#pragma warning disable 414
        private int DeviceID; // Not currently required, but captured for future compatibility
#pragma warning restore 414

        public SerializableFrame(long id, long timestamp, float fps, List<SerializableHand> hands)
        {
            Id = id;
            Timestamp = timestamp;
            CurrentFramesPerSecond = fps;
            Hands = hands;
            DeviceID = 1;
        }

        public static implicit operator SerializableFrame(Frame f) => new SerializableFrame(f.Id, f.Timestamp, f.CurrentFramesPerSecond, SerializableHand.ConvertHandList(f.Hands));
        public static implicit operator Frame(SerializableFrame f) => new Frame(f.Id, f.Timestamp, f.CurrentFramesPerSecond, SerializableHand.ConvertHandList(f.Hands));
    }

    [System.Serializable]
    public class SerializableHand
    {
        public long FrameId;
        public int Id;
        public float Confidence;
        public float GrabStrength;
        public readonly float PinchStrength;
        public float PinchDistance;
        public float PalmWidth;
        public bool IsLeft;
        public float TimeVisible;
        public SerializableArm Arm;
        public List<FingerS> Fingers;
        public SerializableVecotr3 PalmPosition;
        public SerializableVecotr3 StabilizedPalmPosition;
        public SerializableVecotr3 PalmVelocity;
        public SerializableVecotr3 PalmNormal;
        public SerializableQuaternion Rotation;
        public SerializableVecotr3 Direction;
        public SerializableVecotr3 WristPosition;

        public SerializableHand(long frameID,
                        int id,
                        float confidence,
                        float grabStrength,
                        float pinchStrength,
                        float pinchDistance,
                        float palmWidth,
                        bool isLeft,
                        float timeVisible,
                        Arm arm,
                        List<FingerS> fingers,
                        SerializableVecotr3 palmPosition,
                        SerializableVecotr3 stabilizedPalmPosition,
                        SerializableVecotr3 palmVelocity,
                        SerializableVecotr3 palmNormal,
                        SerializableQuaternion palmOrientation,
                        SerializableVecotr3 direction,
                        SerializableVecotr3 wristPosition)
        {
            FrameId = frameID;
            Id = id;
            Confidence = confidence;
            GrabStrength = grabStrength;
            PinchStrength = pinchStrength;
            PinchDistance = pinchDistance;
            PalmWidth = palmWidth;
            IsLeft = isLeft;
            TimeVisible = timeVisible;
            Arm = arm;
            Fingers = fingers;
            PalmPosition = palmPosition;
            StabilizedPalmPosition = stabilizedPalmPosition;
            PalmVelocity = palmVelocity;
            PalmNormal = palmNormal;
            Rotation = palmOrientation;
            Direction = direction;
            WristPosition = wristPosition;
        }

        public static implicit operator SerializableHand(Hand h) => new SerializableHand(h.FrameId, h.Id, h.Confidence, h.GrabStrength, h.PinchStrength, h.PinchDistance, h.PalmWidth, h.IsLeft, h.TimeVisible, h.Arm, FingerS.ConvertFingerList(h.Fingers), h.PalmPosition, h.StabilizedPalmPosition, h.PalmVelocity, h.PalmNormal, h.Rotation, h.Direction, h.WristPosition);
        public static implicit operator Hand(SerializableHand h) => new Hand(h.FrameId, h.Id, h.Confidence, h.GrabStrength, h.PinchStrength, h.PinchDistance, h.PalmWidth, h.IsLeft, h.TimeVisible, h.Arm, FingerS.ConvertFingerList(h.Fingers), h.PalmPosition, h.StabilizedPalmPosition, h.PalmVelocity, h.PalmNormal, h.Rotation, h.Direction, h.WristPosition);

        public static List<SerializableHand> ConvertHandList(List<Hand> SerializableHand)
        {
            var ret = new List<SerializableHand>();
            foreach (var hand in SerializableHand)
            {
                ret.Add(hand);
            }
            return ret;
        }

        public static List<Hand> ConvertHandList(List<SerializableHand> SerializableHand)
        {
            var ret = new List<Hand>();
            foreach (var hand in SerializableHand)
            {
                ret.Add(hand);
            }
            return ret;
        }
    }

    [System.Serializable]
    public class SerializableArm : SerializableBone
    {
        public SerializableArm(Vector3 elbow,
                       Vector3 wrist,
                       Vector3 center,
                       Vector3 direction,
                       float length,
                       float width,
                       Quaternion rotation)
              : base(elbow,
                     wrist,
                     center,
                     direction,
                     length,
                     width,
                     BoneType.TYPE_METACARPAL, //ignored for arms
                     rotation)
        { }

        public static implicit operator SerializableArm(Arm a) => new SerializableArm(a.PrevJoint, a.NextJoint, a.Center, a.Direction, a.Length, a.Width, a.Rotation);
        public static implicit operator Arm(SerializableArm a) => new Arm(a.PrevJoint, a.NextJoint, a.Center, a.Direction, a.Length, a.Width, a.Rotation);

    }

    [System.Serializable]
    public class FingerS
    {
        public FingerType Type;
        public SerializableBone[] bones = new SerializableBone[4];
        public int Id;
        public int HandId;
        public SerializableVecotr3 TipPosition;
        public SerializableVecotr3 Direction;
        public float Width;
        public float Length;
        public bool IsExtended;
        public float TimeVisible;

        //TODO implement serialization class for Fingers
        public FingerS(long frameId,
                         int handId,
                         int fingerId,
                         float timeVisible,
                         SerializableVecotr3 tipPosition,
                         SerializableVecotr3 direction,
                         float width,
                         float length,
                         bool isExtended,
                         FingerType type,
                         SerializableBone metacarpal,
                         SerializableBone proximal,
                         SerializableBone intermediate,
                         SerializableBone distal)
        {
            Type = type;
            bones[0] = metacarpal;
            bones[1] = proximal;
            bones[2] = intermediate;
            bones[3] = distal;
            Id = (handId * 10) + fingerId;
            HandId = handId;
            TipPosition = tipPosition;
            Direction = direction;
            Width = width;
            Length = length;
            IsExtended = isExtended;
            TimeVisible = timeVisible;
        }

        public static implicit operator FingerS(Finger f) => new FingerS(0, f.HandId, (int)f.Type, f.TimeVisible, f.TipPosition, f.Direction, f.Width, f.Length, f.IsExtended, f.Type, f.bones[0], f.bones[1], f.bones[2], f.bones[3]);
        public static implicit operator Finger(FingerS f) => new Finger(0, f.HandId, (int)f.Type, f.TimeVisible, f.TipPosition, f.Direction, f.Width, f.Length, f.IsExtended, f.Type, f.bones[0], f.bones[1], f.bones[2], f.bones[3]);

        public static List<FingerS> ConvertFingerList(List<Finger> fingers)
        {
            var ret = new List<FingerS>();
            foreach (var finger in fingers)
            {
                ret.Add(finger);
            }
            return ret;
        }

        public static List<Finger> ConvertFingerList(List<FingerS> fingers)
        {
            var ret = new List<Finger>();
            foreach (var finger in fingers)
            {
                ret.Add(finger);
            }
            return ret;
        }
    }

    [System.Serializable]
    public class SerializableBone
    {
        public SerializableVecotr3 PrevJoint;
        public SerializableVecotr3 NextJoint;
        public SerializableVecotr3 Center;
        public SerializableVecotr3 Direction;
        public SerializableQuaternion Rotation;
        public float Length;
        public float Width;
        public BoneType Type;

        public SerializableBone(SerializableVecotr3 prevJoint,
                        SerializableVecotr3 nextJoint,
                        SerializableVecotr3 center,
                        SerializableVecotr3 direction,
                        float length,
                        float width,
                        BoneType type,
                        SerializableQuaternion rotation)
        {
            PrevJoint = prevJoint;
            NextJoint = nextJoint;
            Center = center;
            Direction = direction;
            Rotation = rotation;
            Length = length;
            Width = width;
            Type = type;
        }

        public static implicit operator SerializableBone(Bone b) => new SerializableBone(b.PrevJoint, b.NextJoint, b.Center, b.Direction, b.Length, b.Width, b.Type, b.Rotation);
        public static implicit operator Bone(SerializableBone b) => new Bone(b.PrevJoint, b.NextJoint, b.Center, b.Direction, b.Length, b.Width, b.Type, b.Rotation);
    }

    [System.Serializable]
    public class SerializableVecotr3
    {
        public float x, y, z;

        public SerializableVecotr3(float x, float y, float z)
        {
            this.x = x;
            this.y = y;
            this.z = z;
        }

        public static implicit operator SerializableVecotr3(Vector3 v) => new SerializableVecotr3(v.x, v.y, v.z);
        public static implicit operator Vector3(SerializableVecotr3 v) => new Vector3(v.x, v.y, v.z);
    }

    [System.Serializable]
    public class SerializableQuaternion
    {
        public float x, y, z, w;

        public SerializableQuaternion(float x, float y, float z, float w)
        {
            this.x = x;
            this.y = y;
            this.z = z;
            this.w = w;
        }

        public static implicit operator SerializableQuaternion(Quaternion q) => new SerializableQuaternion(q.x, q.y, q.z, q.w);
        public static implicit operator Quaternion(SerializableQuaternion q) => new Quaternion(q.x, q.y, q.z, q.w);
    }
}


