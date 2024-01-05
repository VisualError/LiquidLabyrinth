using LiquidLabyrinth.Enums;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;
using Unity.Netcode;
using Color = UnityEngine.Color;

namespace LiquidLabyrinth.Netcode.NetworkVariables
{
    internal struct Bottle : INetworkSerializable, IEquatable<Bottle>
    {
        public Color Color;
        public BottleModes Mode;
        public string Name;
        public bool IsOpened;
        public float Fill;

        public bool Equals(Bottle other)
        {
            throw new NotImplementedException();
        }

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            if (serializer.IsReader)
            {
                var reader = serializer.GetFastBufferReader();
                reader.ReadValueSafe(out Color);
                reader.ReadValueSafe(out Mode);
                reader.ReadValueSafe(out Name);
                reader.ReadValueSafe(out IsOpened);
            }
            else
            {
                var writer = serializer.GetFastBufferWriter();
                writer.WriteValueSafe(Color);
                writer.WriteValueSafe(Mode);
                writer.WriteValueSafe(Name);
                writer.WriteValueSafe(IsOpened);
            }
        }
    }
}
