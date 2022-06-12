using System;
using System.Reflection;

namespace MessageBasedSockets {
    public static class MessageWriter {
        internal static int Serialize(ref IMessage message, byte[] segment) {
            ScannedType scannedType = IMessage.MessageTypesByType[message.GetType()];
            if (scannedType == null)
                throw new ApplicationException($"Unknown message type {message.GetType().Name}");
            object[] values = new object[scannedType.Fields.Length];
            for (var i = 0; i < scannedType.Fields.Length; i++) {
                FieldInfo field = scannedType.Fields[i];
                values[i] = field.GetValue(message);
            }

            Span<byte> arr = segment;
            int start = Write(ref arr, 0, typeof(byte), scannedType.Value);
            int sizePos = start;
            start += 2;
            for (var i = 0; i < values.Length; i++) {
                start += Write(ref arr, start, scannedType.Fields[i].FieldType, values[i]);
            }

            // Message size without type byte and this ushort
            WriteUShort(ref arr, sizePos, (ushort)(start - sizePos - 2));

            return start;
        }

        internal static int Write(ref Span<byte> span, int start, Type type, object obj) {
            if (type.IsArray) {
                return WriteArray(ref span, start, (Array)obj);
            }

            if (type == typeof(byte)) {
                return WriteByte(ref span, start, (byte)obj);
            }

            if (type == typeof(sbyte)) {
                return WriteSByte(ref span, start, (sbyte)obj);
            }

            if (type == typeof(bool)) {
                return WriteByte(ref span, start, (byte)((bool)obj ? 1 : 0));
            }

            if (type == typeof(short)) {
                return WriteShort(ref span, start, (short)obj);
            }

            if (type == typeof(ushort)) {
                return WriteUShort(ref span, start, (ushort)obj);
            }

            if (type == typeof(char)) {
                return WriteChar(ref span, start, (char)obj);
            }

            if (type == typeof(int)) {
                return WriteInt(ref span, start, (int)obj);
            }

            if (type == typeof(uint)) {
                return WriteUInt(ref span, start, (uint)obj);
            }

            if (type == typeof(float)) {
                return WriteFloat(ref span, start, (float)obj);
            }

            if (type == typeof(long)) {
                return WriteLong(ref span, start, Convert.ToInt64(obj));
            }

            if (type == typeof(ulong)) {
                return WriteULong(ref span, start, (ulong)obj);
            }

            if (type == typeof(double)) {
                return WriteDouble(ref span, start, (double)obj);
            }

            if (type == typeof(string)) {
                return WriteString(ref span, start, (string)obj);
            }

            if (type.IsValueType && !type.IsPrimitive) {
                return WriteStruct(ref span, start, obj);
            }

            return 0;
        }

        private static int WriteArray(ref Span<byte> span, int start, Array v) {
            if (v == null) {
                return WriteShort(ref span, start, -1);
            }

            Type type = v.GetType().GetElementType();
            if (type == typeof(object))
                throw new ApplicationException("Array of objects is not supported serializable type");

            // object[] copy = new object[v.Length];
            // v.CopyTo(copy, 0);

            int startDefault = start;
            start += WriteShort(ref span, start, (short)v.Length);
            foreach (object o in v) {
                start += Write(ref span, start, type, o);
            }

            return start - startDefault;
        }

        private static int WriteByte(ref Span<byte> span, int start, byte v) {
            span[start] = v;
            return 1;
        }

        private static int WriteSByte(ref Span<byte> span, int start, sbyte v) {
            return WriteByte(ref span, start, unchecked((byte)v));
        }

        private static int WriteShort(ref Span<byte> span, int start, short v) {
            span[start] = (byte)((v >> 8) & 0xFF);
            span[start + 1] = (byte)(v & 0xFF);
            return 2;
        }

        private static int WriteUShort(ref Span<byte> span, int start, ushort v) {
            return WriteShort(ref span, start, unchecked((short)v));
        }

        private static int WriteChar(ref Span<byte> span, int start, char v) {
#if BYTE1_CHAR
            return WriteByte(ref span, start, Convert.ToByte(v));
#else
            return WriteShort(ref span, start, Convert.ToInt16(v));
#endif
        }

        private static int WriteInt(ref Span<byte> span, int start, int v) {
            span[start] = (byte)((v >> 24) & 0xFF);
            span[start + 1] = (byte)((v >> 16) & 0xFF);
            span[start + 2] = (byte)((v >> 8) & 0xFF);
            span[start + 3] = (byte)(v & 0xFF);
            return 4;
        }

        private static int WriteUInt(ref Span<byte> span, int start, uint v) {
            return WriteInt(ref span, start, unchecked((int)v));
        }

        private static int WriteFloat(ref Span<byte> span, int start, float v) {
            int fv = BitConverter.SingleToInt32Bits(v);
            return WriteInt(ref span, start, fv);
        }

        private static int WriteLong(ref Span<byte> span, int start, long v) {
            span[start] = (byte)((v >> 56) & 0xFF);
            span[start + 1] = (byte)((v >> 48) & 0xFF);
            span[start + 2] = (byte)((v >> 40) & 0xFF);
            span[start + 3] = (byte)((v >> 32) & 0xFF);
            span[start + 4] = (byte)((v >> 24) & 0xFF);
            span[start + 5] = (byte)((v >> 16) & 0xFF);
            span[start + 6] = (byte)((v >> 8) & 0xFF);
            span[start + 7] = (byte)(v & 0xFF);
            return 8;
        }

        private static int WriteULong(ref Span<byte> span, int start, ulong v) {
            return WriteLong(ref span, start, unchecked((long)v));
        }

        private static int WriteDouble(ref Span<byte> span, int start, double v) {
            long fv = BitConverter.DoubleToInt64Bits(v);
            return WriteLong(ref span, start, fv);
        }

        private static int WriteString(ref Span<byte> span, int start, string v) {
            if (v == null) {
                return WriteShort(ref span, start, -1);
            }

            int startDefault = start;
            start += WriteShort(ref span, start, (short)v.Length);
            var chars = v.AsSpan();
            foreach (var c in chars) {
                start += WriteChar(ref span, start, c);
            }

            return start - startDefault;
        }

        private static int WriteStruct(ref Span<byte> span, int start, object v) {
            Type type = v.GetType();
            // TypeScanner.ScannedTypeByType.TryGetValue(type, out var scannedType);
            // if (scannedType == null)
            //     throw new ApplicationException($"Unknown type {type.Name}");
            var scannedType = TypeScanner.ScannedTypeByType[type];

            int startDefault = start;
            foreach (var field in scannedType.Fields) {
                start += Write(ref span, start, field.FieldType, field.GetValue(v));
            }

            return start - startDefault;
        }
    }
}