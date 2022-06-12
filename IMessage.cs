using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace MessageBasedSockets {
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
                // FieldInfo[] fields = TypeScanner.GetFields(messageType);
                // foreach (var fieldInfo in fields) {
                //     if (!TypeScanner.IsValid(fieldInfo.FieldType))
                //         continue;
                //     TypeScanner.Scan(fieldInfo.FieldType);
                // }
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

        // internal static int GetObjectSize(object obj) {
        //     Type type = obj.GetType();
        //
        //     if (type.IsArray) {
        //         
        //     }
        //     if (type == typeof(byte) || type == typeof(sbyte) ||
        //         type == typeof(bool))
        //         return 1;
        //     if (type == typeof(short) || type == typeof(ushort) ||
        //         type == typeof(char))
        //         return 2;
        //     if (type == typeof(int) || type == typeof(uint) ||
        //         type == typeof(float))
        //         return 4;
        //     if (type == typeof(long) || type == typeof(ulong) ||
        //         type == typeof(double))
        //         return 8;
        //     if (type == typeof(string))
        //         return 3 + ((string)obj).Length * 2; // String length
        //
        //     return 0;
        // }
    }
}