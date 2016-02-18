/******************************************************************************\
* Copyright (C) 2012-2016 Leap Motion, Inc. All rights reserved.               *
* Leap Motion proprietary and confidential. Not for distribution.              *
* Use subject to the terms of the Leap Motion SDK Agreement available at       *
* https://developer.leapmotion.com/sdk_agreement, or another agreement         *
* between Leap Motion and you, your company or other organization.             *
\******************************************************************************/
namespace Leap
{

    using System;
    using System.Collections.Generic;
    using System.Runtime.InteropServices;
    using LeapInternal;

    /**
   * The Config class provides access to Leap Motion system configuration information.
   *
   * @since 1.0
   */

    public class Config
    {
        private Connection _connection;
        private Dictionary<UInt32, object> _transactions = new Dictionary<UInt32, object> ();

        public Config (int connectionKey)
        {
            _connection = Connection.GetConnection (connectionKey);
            _connection.LeapConfigChange += handleConfigChange;
            _connection.LeapConfigResponse += handleConfigResponse;
        }

        private void handleConfigChange (object sender, ConfigChangeEventArgs eventArgs)
        {
            object actionDelegate;
            if (_transactions.TryGetValue (eventArgs.RequestId, out actionDelegate)) {
                Action<bool> changeAction = actionDelegate as Action<bool>;
                changeAction (eventArgs.Succeeded);
                _transactions.Remove (eventArgs.RequestId);
            }
        }

        private void handleConfigResponse (object sender, SetConfigResponseEventArgs eventArgs)
        {
            object actionDelegate = new object ();
            if (_transactions.TryGetValue (eventArgs.RequestId, out actionDelegate)) {
                switch (eventArgs.DataType) {
                case Config.ValueType.TYPE_BOOLEAN:
                    Action<bool> boolAction = actionDelegate as Action<bool>;
                    boolAction ((bool)eventArgs.Value);
                    break;
                case Config.ValueType.TYPE_FLOAT:
                    Action<float> floatAction = actionDelegate as Action<float>;
                    floatAction ((float)eventArgs.Value);
                    break;
                case Config.ValueType.TYPE_INT32:
                    Action<Int32> intAction = actionDelegate as Action<Int32>;
                    intAction ((Int32)eventArgs.Value);
                    break;
                case Config.ValueType.TYPE_STRING:
                    Action<string> stringAction = actionDelegate as Action<string>;
                    stringAction ((string)eventArgs.Value);
                    break;
                default:
                    break;
                }
                _transactions.Remove (eventArgs.RequestId);
            }
        }

        public bool Get<T> (string key, Action<T> onResult)
        {
            
            uint requestId = _connection.GetConfigValue (key);
            if (requestId > 0) {
                _transactions.Add (requestId, onResult);
                return true;
            }
            return false;
        }

        public bool Set<T> (string key, T value, Action<bool> onResult) where T : IConvertible
        {
            uint requestId = _connection.SetConfigValue<T> (key, value);

            if (requestId > 0) {
                _transactions.Add (requestId, onResult);
                return true;
            }
            return false;
        }

    /**
     * The data type for a configuration parameter.
     * @ deprecated Always returns Config.ValueType.TYPE_UNKNOWN
     */
        public Config.ValueType Type (string key)
        {
            return Config.ValueType.TYPE_UNKNOWN;
        }

     /**
     * Gets the boolean representation for the specified key.
     *
     * @ deprecated use Get<bool>(key, delegate)
     *
     * @since 1.0
     */
        public bool GetBool (string key)
        {
            return Get<bool> (key, delegate (bool value) {
            });
        }

     /** Sets the boolean representation for the specified key.
     *
     * \include Config_setBool.txt
     *
     * @returns true on success, false on failure.
     * @since 1.0
     */
        public bool SetBool (string key, bool value)
        {
            return Set<bool>(key, value, delegate(bool success){});
        }

    /**
     * Gets the 32-bit integer representation for the specified key.
     * @ deprecated use Get<Int32>(key, delegate)
     * @since 1.0
     */
        public bool GetInt32 (string key)
        {
            return Get<Int32> (key, delegate (Int32 value) {
            });
        }

        /** Sets the 32-bit integer representation for the specified key.
     *
     * \include Config_setInt32.txt
     *
     * @returns true on success, false on failure.
     * @since 1.0
     */
        public bool SetInt32 (string key, int value)
        {
            return Set<Int32>(key, value, delegate(bool success){});
        }

    /**
     * Gets the floating point representation for the specified key.
     * @ deprecated use Get<float>(key, delegate)
     * @since 1.0
     */
        public bool GetFloat (string key)
        {
            return Get<float> (key, delegate (float value) {
            });
        }
        /** Sets the floating point representation for the specified key.
     *
     * \include Config_setFloat.txt
     *
     * @returns true on success, false on failure.
     * @since 1.0
     */
        public bool SetFloat (string key, float value)
        {
            return Set<float>(key, value, delegate(bool success){});
        }

        /**
     * Gets the string representation for the specified key.
     * @ deprecated use Get<string>(key, delegate)
     * @since 1.0
     */
        public bool GetString (string key)
        {
            return Get<string> (key, delegate (string value) {
            });
        }
        /** Sets the string representation for the specified key.
     *
     * \include Config_setString.txt
     *
     * @returns true on success, false on failure.
     * @since 1.0
     */
        public bool SetString (string key, string value)
        {
            return Set<string>(key, value, delegate(bool success){});
        }

        /**
     * Saves the current state of the config.
     *
     * @deprecated no longer required, configuration changes are saved immediately.
     * @returns always returns false.
     * @since 1.0
     */
        public bool Save ()
        {
            return false;
        }

        /**
       * Enumerates the possible data types for configuration values.
       *
       * The Config::type() function returns an item from the ValueType enumeration.
       * @since 1.0
       */
        public enum ValueType
        {
            /**
         * The data type is unknown.
         * @since 1.0
         */
            TYPE_UNKNOWN = 0,
            /**
         * A boolean value.
         * @since 1.0
         */
            TYPE_BOOLEAN = 1,
            /**
         * A 32-bit integer.
         * @since 1.0
         */
            TYPE_INT32 = 2,
            /**
         * A floating-point number.
         * @since 1.0
         */
            TYPE_FLOAT = 6,
            /**
         * A string of characters.
         * @since 1.0
         */
            TYPE_STRING = 8,
        }
    }

}
