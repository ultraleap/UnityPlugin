// clang-format off

#ifndef _LEAP_SERVER_INTERFACE_H
#define _LEAP_SERVER_INTERFACE_H

#ifndef LEAP_SERVER_EXPORT
#ifdef _MSC_VER
    #define LEAP_SERVER_EXPORT __declspec(dllimport)
#else
    #define LEAP_SERVER_EXPORT
#endif
#endif

#ifdef _MSC_VER
  #define LEAP_SERVER_CALL __stdcall
#else
  #define LEAP_SERVER_CALL
#endif

#ifdef __cplusplus
extern "C"
{
#endif

struct LeapServer_Handle;
typedef struct LeapServer_Config_
{
  const char* ip_address;  // Must be null-terminated. Ownership remains with the caller
  unsigned int port;

  const char* user_accessible_path;  // Must be null-terminated. Ownership remains with the caller.
} LeapServer_Config;

LEAP_SERVER_EXPORT void LEAP_SERVER_CALL LeapServerCreate(struct LeapServer_Handle** handle, const LeapServer_Config* config);
LEAP_SERVER_EXPORT void LEAP_SERVER_CALL LeapServerStart(struct LeapServer_Handle* handle);
LEAP_SERVER_EXPORT void LEAP_SERVER_CALL LeapServerStop(struct LeapServer_Handle* handle);
LEAP_SERVER_EXPORT void LEAP_SERVER_CALL LeapServerDestroy(struct LeapServer_Handle* handle);

#ifdef __cplusplus
}
#endif

#endif

// clang-format on
