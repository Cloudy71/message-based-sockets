using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace MessageBasedSockets {
    /// <summary>
    /// Interface for every message structure build and used in this networking system.
    /// </summary>
    public interface IMessage {
        internal static readonly Dictionary<Type, ScannedType> MessageTypesByType = new();
        internal static readonly Dictionary<byte, ScannedType> MessageTypesByByte = new();

        internal static void RegisterMessageTypes() {
            MessageTypesByType.Clear();
            MessageTypesByByte.Clear();
            Logging.Debug("Reading message types...");
            byte val = 0;
            foreach (var messageType in AppDomain.CurrentDomain.GetAssemblies()
                                                 .SelectMany(assembly => assembly.GetTypes())
                                                 .Where(type => type.IsValueType && typeof(IMessage).IsAssignableFrom(type))) {
                TypeScanner.Scan(messageType);

                var obj = TypeScanner.ScannedTypeByType[messageType];
                obj.Value = val;
                MessageTypesByType.Add(
                    messageType,
                    obj
                );
                MessageTypesByByte.Add(
                    val++,
                    obj
                );
                Logging.Debug($"Message type {messageType.Name} has been registered");
            }
        }
    }
}