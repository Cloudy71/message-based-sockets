using System;
using System.Reflection;

namespace MessageBasedSockets {
    internal class ScannedType {
        public byte        Value;
        public Type        Type;
        public FieldInfo[] Fields;

        public ScannedType() {
        }
    }
}