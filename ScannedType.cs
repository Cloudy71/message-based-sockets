using System;
using System.Reflection;

namespace MessageBasedSockets {
    public class ScannedType {
        public byte        Value;
        public Type        Type;
        public FieldInfo[] Fields;

        internal ScannedType() {
        }
    }
}