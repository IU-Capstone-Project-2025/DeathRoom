using System;
using System.Collections.Generic;
using MessagePack;

namespace DeathRoom.Common.network
{
    public static class PacketProcessor
    {
        private static readonly Dictionary<Type, PacketType> PacketTypes = new()
        {
            { typeof(PlayerMovePacket), PacketType.PlayerMove },
            { typeof(WorldStatePacket), PacketType.WorldState },
            { typeof(LoginPacket), PacketType.Login },
            { typeof(PlayerShootPacket), PacketType.PlayerShoot },
            // дополним другими пакетами
        };

        public static byte[] Pack(IPacket packet)
        {
            if (!PacketTypes.TryGetValue(packet.GetType(), out var type))
            {
                throw new Exception($"Unknown packet type: {packet.GetType()}");
            }

            var packetBytes = MessagePackSerializer.Serialize(packet);
            var finalPacket = new byte[packetBytes.Length + 1];
            finalPacket[0] = (byte)type;
            Buffer.BlockCopy(packetBytes, 0, finalPacket, 1, packetBytes.Length);
            return finalPacket;
        }

        public static (PacketType, IPacket) Unpack(byte[] data)
        {
            var type = (PacketType)data[0];
            
            var packetBytes = new byte[data.Length - 1];
            Buffer.BlockCopy(data, 1, packetBytes, 0, packetBytes.Length);
            
            var packet = MessagePackSerializer.Deserialize<IPacket>(packetBytes);

            return (type, packet);
        }
    }
} 