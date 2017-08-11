

namespace Leap.Unity.Glint.Internal {

  public enum Status {
    Success = 0,
    NotReady = 1,
    Error_UnsupportedAPI = 2,
    Error_UnsupportedFeature = 3,
    Error_UnsupportedFormat = 4,
    Error_WrongBufferSize = 5,
    Error_NoRequest = 6,
    Error_InvalidArguments = 7,
    Error_TooManyRequests = 8,
    Error_UnableToMapMemory = 9
  };

}