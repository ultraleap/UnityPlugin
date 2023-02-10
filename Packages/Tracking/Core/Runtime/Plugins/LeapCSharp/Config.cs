/******************************************************************************
 * Copyright (C) Ultraleap, Inc. 2011-2023.                                   *
 *                                                                            *
 * Use subject to the terms of the Apache License 2.0 available at            *
 * http://www.apache.org/licenses/LICENSE-2.0, or another agreement           *
 * between Ultraleap and you, your company or other organization.             *
 ******************************************************************************/

namespace Leap
{

    using LeapInternal;
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// The Config class provides access to Leap Motion system configuration information.
    /// 
    /// @since 1.0
    /// </summary>
    public class Config
    {
        private Connection _connection;
        private Dictionary<UInt32, object> _transactions = new Dictionary<UInt32, object>();

        /// <summary>
        /// Creates a new Config object for setting runtime configuration settings.
        /// 
        /// Note that the Controller.Config provides a properly initialized Config object already.
        /// @since 3.0
        /// </summary>
        public Config(Connection.Key connectionKey)
        {
            _connection = Connection.GetConnection(connectionKey);
            _connection.LeapConfigChange += handleConfigChange;
            _connection.LeapConfigResponse += handleConfigResponse;
        }
        public Config(int connectionId) : this(new Connection.Key(connectionId)) { }

        private void handleConfigChange(object sender, ConfigChangeEventArgs eventArgs)
        {
            object actionDelegate;
            if (_transactions.TryGetValue(eventArgs.RequestId, out actionDelegate))
            {
                Action<bool> changeAction = actionDelegate as Action<bool>;
                changeAction(eventArgs.Succeeded);
                _transactions.Remove(eventArgs.RequestId);
            }
        }

        private void handleConfigResponse(object sender, SetConfigResponseEventArgs eventArgs)
        {
            object actionDelegate = new object();
            if (_transactions.TryGetValue(eventArgs.RequestId, out actionDelegate))
            {
                switch (eventArgs.DataType)
                {
                    case ValueType.TYPE_BOOLEAN:
                        Action<bool> boolAction = actionDelegate as Action<bool>;
                        boolAction((int)eventArgs.Value != 0);
                        break;
                    case ValueType.TYPE_FLOAT:
                        Action<float> floatAction = actionDelegate as Action<float>;
                        floatAction((float)eventArgs.Value);
                        break;
                    case ValueType.TYPE_INT32:
                        Action<Int32> intAction = actionDelegate as Action<Int32>;
                        intAction((Int32)eventArgs.Value);
                        break;
                    case ValueType.TYPE_STRING:
                        Action<string> stringAction = actionDelegate as Action<string>;
                        stringAction((string)eventArgs.Value);
                        break;
                    default:
                        break;
                }
                _transactions.Remove(eventArgs.RequestId);
            }
        }

        /// <summary>
        /// Requests a configuration value.
        /// 
        /// You must provide an action to take when the Leap service returns the config value.
        /// The Action delegate must take a parameter matching the config value type. The current
        /// value of the setting is passed to this delegate.
        /// 
        /// @since 3.0
        /// </summary>
        public bool Get<T>(string key, Action<T> onResult)
        {
            uint requestId = _connection.GetConfigValue(key);
            if (requestId > 0)
            {
                _transactions.Add(requestId, onResult);
                return true;
            }
            return false;
        }

        /// <summary>
        /// Sets a configuration value.
        /// 
        /// You must provide an action to take when the Leap service sets the config value.
        /// The Action delegate must take a boolean parameter. The service calls this delegate
        /// with the value true if the setting was changed successfully and false, otherwise.
        /// 
        /// @since 3.0
        /// </summary>
        public bool Set<T>(string key, T value, Action<bool> onResult) where T : IConvertible
        {
            uint requestId = _connection.SetConfigValue<T>(key, value);

            if (requestId > 0)
            {
                _transactions.Add(requestId, onResult);
                return true;
            }
            return false;
        }

        /// <summary>
        /// Enumerates the possible data types for configuration values.
        /// @since 1.0
        /// </summary>
        public enum ValueType
        {
            TYPE_UNKNOWN = 0,
            TYPE_BOOLEAN = 1,
            TYPE_INT32 = 2,
            TYPE_FLOAT = 6,
            TYPE_STRING = 8,
        }
    }
}