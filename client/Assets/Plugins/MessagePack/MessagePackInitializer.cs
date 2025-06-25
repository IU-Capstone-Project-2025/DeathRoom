using UnityEngine;
using MessagePack;
using MessagePack.Resolvers;
using System.Collections.Generic;

namespace DeathRoom.Network
{
    public static class MessagePackInitializer
    {
        private static bool isInitialized;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        public static void Initialize()
        {
            if (isInitialized) return;
            isInitialized = true;

            StaticCompositeResolver.Instance.Register(
                MessagePack.Resolvers.GeneratedResolver.Instance,
                MessagePack.Resolvers.StandardResolver.Instance
            );

            var option = MessagePackSerializerOptions.Standard.WithResolver(StaticCompositeResolver.Instance);
            MessagePackSerializer.DefaultOptions = option;
        }
    }
}