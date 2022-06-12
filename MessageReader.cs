using System;
using System.Reflection;
using MessageBasedSockets.Exceptions;

namespace MessageBasedSockets {
    public static class MessageReader {
        internal static IMessage Deserialize(byte[] segment, int offset, int size, out int readLen) {
            readLen = 0;
            if (size <= 0)
                return default;
            if (size == 1) {
                throw new IncompleteMessageException(offset, size, -1);
            }

            var arraySegment = new ArraySegment<byte>(segment, offset, size);
            byte type = arraySegment[0];
            ScannedType scannedType = IMessage.MessageTypesByByte[type];
            if (scannedType == null)
                throw new ApplicationException($"Unknown message type b{type}");
            IMessage message = (IMessage)Activator.CreateInstance(scannedType.Type);
            int start = 1;
            ushort messageSize = ReadUShort(ref arraySegment, start, out int len);
            if (size < messageSize) {
                // Validation for incomplete message.
                // This exception must be caught and the buffer content outside start - end must be cleared and shifted
                throw new IncompleteMessageException(offset, size, messageSize);
            }

            arraySegment = new ArraySegment<byte>(segment, offset + 3, messageSize);
            start = 0;
            for (var i = 0; i < scannedType.Fields.Length; i++) {
                FieldInfo field = scannedType.Fields[i];
                object obj = Read(ref arraySegment, start, field.FieldType, out len);
                field.SetValue(message, obj);
                start += len;
            }

            readLen = start + 3;
            return message;
        }

        internal static object Read(ref ArraySegment<byte> seg, int start, Type type, out int len) {
            len = 0;
            if (type.IsArray) {
                return ReadArray(ref seg, start, type.GetElementType(), out len);
            }

            if (type == typeof(byte)) {
                return ReadByte(ref seg, start, out len);
            }

            if (type == typeof(sbyte)) {
                return ReadSByte(ref seg, start, out len);
            }

            if (type == typeof(bool)) {
                return ReadByte(ref seg, start, out len) == 1;
            }

            if (type == typeof(short)) {
                return ReadShort(ref seg, start, out len);
            }

            if (type == typeof(ushort)) {
                return ReadUShort(ref seg, start, out len);
            }

            if (type == typeof(char)) {
                return ReadChar(ref seg, start, out len);
            }

            if (type == typeof(int)) {
                return ReadInt(ref seg, start, out len);
            }

            if (type == typeof(uint)) {
                return ReadUInt(ref seg, start, out len);
            }

            if (type == typeof(float)) {
                return ReadFloat(ref seg, start, out len);
            }

            if (type == typeof(long)) {
                return ReadLong(ref seg, start, out len);
            }

            if (type == typeof(ulong)) {
                return ReadULong(ref seg, start, out len);
            }

            if (type == typeof(double)) {
                return ReadDouble(ref seg, start, out len);
            }

            if (type == typeof(string)) {
                return ReadString(ref seg, start, out len);
            }

            if (type.IsValueType && !type.IsPrimitive) {
                return ReadStruct(ref seg, start, type, out len);
            }

            return null;
        }

        private static Array ReadArray(ref ArraySegment<byte> span, int start, Type type, out int len) {
            len = 0;
            var length = ReadShort(ref span, start, out int lenAdd);
            start += lenAdd;
            len += lenAdd;
            if (length == -1) {
                return null;
            }

            Array arr = Array.CreateInstance(type, length);
            for (int i = 0; i < length; i++) {
                arr.SetValue(Read(ref span, start, type, out lenAdd), i);
                start += lenAdd;
                len += lenAdd;
            }

            return arr;
        }

        private static byte ReadByte(ref ArraySegment<byte> span, int start, out int len) {
            len = 1;
            return span[start];
        }

        private static sbyte ReadSByte(ref ArraySegment<byte> span, int start, out int len) {
            return unchecked((sbyte)ReadByte(ref span, start, out len));
        }

        private static short ReadShort(ref ArraySegment<byte> span, int start, out int len) {
            len = 2;
            return (short)((span[start] << 8) + span[start + 1]);
        }

        private static ushort ReadUShort(ref ArraySegment<byte> span, int start, out int len) {
            return unchecked((ushort)ReadShort(ref span, start, out len));
        }

        private static char ReadChar(ref ArraySegment<byte> span, int start, out int len) {
#if BYTE1_CHAR
            return Convert.ToChar(ReadByte(ref span, start, out len));
#else
            return Convert.ToChar(ReadShort(ref span, start, out len));
#endif
        }

        private static int ReadInt(ref ArraySegment<byte> span, int start, out int len) {
            len = 4;
            return (span[start] << 24) + (span[start + 1] << 16) + (span[start + 2] << 8) + span[start + 3];
        }

        private static uint ReadUInt(ref ArraySegment<byte> span, int start, out int len) {
            return unchecked((uint)ReadInt(ref span, start, out len));
        }

        private static float ReadFloat(ref ArraySegment<byte> span, int start, out int len) {
            return BitConverter.Int32BitsToSingle(ReadInt(ref span, start, out len));
        }

        private static long ReadLong(ref ArraySegment<byte> span, int start, out int len) {
            len = 8;
            return ((long)span[start] << 56) + ((long)span[start + 1] << 48) + ((long)span[start + 2] << 40) + ((long)span[start + 3] << 32) +
                   ((long)span[start + 4] << 24) + ((long)span[start + 5] << 16) + ((long)span[start + 6] << 8) + span[start + 7];
        }

        private static ulong ReadULong(ref ArraySegment<byte> span, int start, out int len) {
            return unchecked((ulong)ReadLong(ref span, start, out len));
        }

        private static double ReadDouble(ref ArraySegment<byte> span, int start, out int len) {
            return BitConverter.Int64BitsToDouble(ReadLong(ref span, start, out len));
        }

        private static string ReadString(ref ArraySegment<byte> span, int start, out int len) {
            len = 0;
            // byte validString = ReadByte(ref span, start, out len);
            // if (validString == 0) {
            //     return null;
            // }
            // else if (validString != 1) {
            //     throw new ApplicationException("Integrity fail");
            // }

            // start += len;
            var length = ReadShort(ref span, start, out int lenAdd);
            len += lenAdd;
            start += lenAdd;
            if (length == -1) {
                return null;
            }

            Span<char> chars = stackalloc char[length];
            for (var i = 0; i < length; i++) {
                chars[i] = ReadChar(ref span, start, out lenAdd);
                len += lenAdd;
                start += lenAdd;
            }

            return chars.ToString();
        }

        private static object ReadStruct(ref ArraySegment<byte> span, int start, Type type, out int len) {
            len = 0;
            var scannedType = TypeScanner.ScannedTypeByType[type];
            object obj = Activator.CreateInstance(type);
            foreach (var field in scannedType.Fields) {
                field.SetValue(obj, Read(ref span, start, field.FieldType, out int lenAdd));
                start += lenAdd;
                len += lenAdd;
            }

            return obj;
        }
    }
}