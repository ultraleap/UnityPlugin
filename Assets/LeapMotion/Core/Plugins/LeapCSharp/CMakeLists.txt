if(NOT CSHARP_DOTNET_FOUND AND NOT CSHARP_MONO_FOUND)
  return()
endif()

set(CSHARP_BINARY_DIRECTORY ${PROJECT_EXECUTABLE_PATH})

set(LeapCSharp_SRCS
  Arm.cs
  AssemblyInfo.cs
  Bone.cs
  CircularObjectBuffer.cs
  ClockCorrelator.cs
  Config.cs
  Connection.cs
  Controller.cs
  CopyFromLeapCExtensions.cs
  CopyFromOtherExtensions.cs
  CSharpExtensions.cs
  Device.cs
  DeviceList.cs
  DistortionData.cs
  DistortionDictionary.cs
  Events.cs
  FailedDevice.cs
  FailedDeviceList.cs
  Finger.cs
  Frame.cs
  Hand.cs
  IController.cs
  Image.cs
  ImageData.cs
  InteractionBox.cs
  LeapC.cs
  LeapTransform.cs
  LeapQuaternion.cs
  Logger.cs
  MemoryManager.cs
  MessageSeverity.cs
  ObjectPool.cs
  PooledObject.cs
  StructMarshal.cs
  TimeBracket.cs
  TransformExtensions.cs
  Vector.cs
)

configure_file(AssemblyInfo.cs.in AssemblyInfo.cs @ONLY)

set(CSHARP_PLATFORM anycpu)

if(MSVC AND "${HOME}" STREQUAL "")
  # HOME is defined on Jenkins Windows slave, but undefined for local testing
  string(REPLACE "\\" "/" HOME "$ENV{USERPROFILE}")
endif()
if(MSVC)
  set(STRONG_NAME_KEY "${HOME}/LeapMotion/Certificates/LeapSigningStrongName.snk")
  string(REGEX REPLACE "\\\\" "/" STRONG_NAME_KEY ${STRONG_NAME_KEY})
else()
  set(STRONG_NAME_KEY "${HOME}/LeapMotion/Certificates/LeapSigningPFX.snk")
endif()
if(NOT EXISTS "${STRONG_NAME_KEY}")
  set(STRONG_NAME_KEY)
endif()

foreach(version 3.5 4.0 4.5)
  if(MSVC)
    string(FIND "${CSHARP_DOTNET_REFASSEMBLY_VERSIONS}" "v${version}" FOUND_POS)
    if(${FOUND_POS} EQUAL -1)
      continue()
    endif()
  endif()
  set(ORIGINAL_FILE_NAME "LeapCSharp.NET${version}.dll")
  configure_file(LeapCSharp.rc.in LeapCSharp.NET${version}.rc @ONLY)
  csharp_add_library(LeapCSharp.NET${version} VERSION v${version} FILES ${LeapCSharp_SRCS} LeapCSharp.NET${version}.rc ${STRONG_NAME_KEY})
  add_dependencies(LeapCSharp.NET${version} LeapC)
  if(TARGET SDK)
    add_dependencies(SDK LeapCSharp.NET${version})
  endif()
  set_property(TARGET LeapCSharp.NET${version} PROPERTY FOLDER "API")
endforeach()
