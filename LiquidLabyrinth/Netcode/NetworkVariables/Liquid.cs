using LiquidLabyrinth.Enums;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;
using Unity.Collections;
using Unity.Netcode;
using Color = UnityEngine.Color;

namespace LiquidLabyrinth.Netcode.NetworkVariables
{
    internal struct Liquid : INetworkSerializable, IEquatable<Liquid>
    {
        public Color Color;
        public float Fill;
        public FixedString128Bytes Name;
        public FixedString128Bytes LiquidID;

        public bool Equals(Liquid other)
        {
            throw new NotImplementedException();
        }

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            if (serializer.IsReader)
            {
                var reader = serializer.GetFastBufferReader();
                reader.ReadValueSafe(out Color);
                reader.ReadValueSafe(out Name);
                reader.ReadValueSafe(out LiquidID);
                reader.ReadValueSafe(out Fill);
            }
            else
            {
                var writer = serializer.GetFastBufferWriter();
                writer.WriteValueSafe(Color);
                writer.WriteValueSafe(Name);
                writer.WriteValueSafe(LiquidID);
                writer.WriteValueSafe(Fill);
            }
        }
    }
}
