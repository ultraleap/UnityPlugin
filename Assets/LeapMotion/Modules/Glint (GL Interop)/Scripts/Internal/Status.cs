

namespace Leap.Unity.Glint.Internal {
  
  /// <summary>
  /// This enum should match the Status enum defined in the Glint DLL
  /// for meaningful success/error messages.
  /// </summary>
  public enum Status {
    Ready                                    = -1, // Note: Unity-side-only Status
                                                   // for just-created Requests.

    Success_0_ReadyForRenderThreadRequest    = 0,
    Success_1_ReadyForRenderThreadMapAndCopy = 1,
    Success_2_ReadyForMainThreadRetrieval    = 2,
    Success_3_CopySuccessful                 = 3,

    Error_UnsupportedAPI                     = 4,
    Error_UnsupportedFeature                 = 5,
    Error_UnsupportedFormat                  = 6,
    Error_WrongBufferSize                    = 7,
    Error_NoRequest                          = 8,
    Error_InvalidArguments                   = 9,
    Error_TooManyRequests                    = 10,
    Error_UnableToMapMemory                  = 11,
    Error_RequestAlreadyPendingForTexture    = 12,
  };

}