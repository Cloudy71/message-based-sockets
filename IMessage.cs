using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using MessageBasedSockets.Attributes;
using MessageBasedSockets.Exceptions;

namespace MessageBasedSockets {
    /// <summary>
    /// Interface for every message structure build and used in this networking system.
    /// </summary>
    public interface IMessage {
        private static byte _id = 0;

        internal static readonly Dictionary<Type, ScannedType> MessageTypesByType = new();
        internal static readonly Dictionary<byte, ScannedType> MessageTypesByByte = new();

        internal static TypeAttributes VisibilityMask = TypeAttributes.Public;

        private static byte GetFreeMessageId() {
            while (MessageTypesByByte.ContainsKey(_id)) {
                _id++;
            }

            return _id;
        }

        internal static void RegisterMessageTypes() {
            MessageTypesByType.Clear();
            MessageTypesByByte.Clear();
            Logging.Debug("Reading message types...");
            // Select all messages and prioritize these messages with id attribute
            foreach (var messageType in AppDomain.CurrentDomain.GetAssemblies()
                                                 .SelectMany(assembly => assembly.GetTypes())
                                                 .Where(type =>
                                                            type.IsValueType &&
                                                            VisibilityMask == (type.Attributes & TypeAttributes.VisibilityMask) &&
                                                            typeof(IMessage).IsAssignableFrom(type)
                                                 )
                                                 .OrderBy(type => type.GetCustomAttribute<MessageIdAttribute>() != null ? 1 : 0)
                    ) {
                MessageIdAttribute idAttribute = messageType.GetCustomAttribute<MessageIdAttribute>();
                byte id = idAttribute?.Id ?? GetFreeMessageId();
                if (MessageTypesByByte.ContainsKey(id))
                    throw new MessageIdTakenException(id);
                TypeScanner.Scan(messageType);

                var obj = TypeScanner.ScannedTypeByType[messageType];
                obj.Value = id;
                MessageTypesByType.Add(
                    messageType,
                    obj
                );
                MessageTypesByByte.Add(
                    id,
                    obj
                );
                Logging.Debug($"Message type {messageType.Name}({id}) has been registered");
            }
        }
    }
}