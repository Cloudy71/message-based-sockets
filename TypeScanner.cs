using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace MessageBasedSockets {
    internal static class TypeScanner {
        private static  byte                          _byteId           = 0;
        internal static Dictionary<Type, ScannedType> ScannedTypeByType = new();

        internal static bool IsValid(Type type) {
            return type.IsValueType && !type.IsPrimitive;
        }

        internal static bool IsTypeSupported(Type type) {
            return type == typeof(byte) || type == typeof(sbyte) ||
                   type == typeof(bool) || type == typeof(short) || type == typeof(ushort) ||
                   type == typeof(char) || type == typeof(int) || type == typeof(uint) ||
                   type == typeof(float) || type == typeof(long) || type == typeof(ulong) ||
                   type == typeof(double) || type == typeof(string) || type.IsArray ||
                   (type.IsValueType && !type.IsPrimitive);
        }

        internal static FieldInfo[] GetFields(Type type) {
            return type.GetFields(BindingFlags.Public | BindingFlags.Instance)
                       .Where(info => info.GetCustomAttribute<NonSerializedAttribute>() == null &&
                                      IsTypeSupported(info.FieldType))
                       .ToArray();
        }

        internal static void Scan(Type type) {
            if (ScannedTypeByType.ContainsKey(type))
                return;
            FieldInfo[] fields = GetFields(type);
            foreach (var fieldInfo in fields) {
                if (!IsValid(fieldInfo.FieldType))
                    continue;
                Scan(fieldInfo.FieldType);
            }

            var obj = new ScannedType {
                                          Value = _byteId++,
                                          Type = type,
                                          Fields = fields
                                      };
            ScannedTypeByType.Add(type, obj);
        }
    }
}