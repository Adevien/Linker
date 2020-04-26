using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Newtonsoft.Json;

namespace Linker
{
    public class StreamProcessor
    {
        private static class HashCache<T>
        {
            public static bool Initialized;
            public static ulong Id;
        }

        protected delegate void SubscribeDelegate(byte[] data, object link);

        private readonly Dictionary<ulong, SubscribeDelegate> _callbacks = new Dictionary<ulong, SubscribeDelegate>();

        public void ReadPacket(byte[] data) => ReadPacket(data, null);

        public void ReadPacket(byte[] data, object link)
        {
            try
            {
                if (data.Length > 0)
                    GetCallbackFromData(data)(data, link);
            }
            catch (Exception)
            {
                Console.WriteLine("Empty data in Processor");
            }
        }

        public void Send<T>(Portal portal, T data) where T : class, new() => portal.Send(Write(data));

        public void Send<T>(Node node, int id, T data) where T : class, new() => node.SendTo(Write<T>(data), id);

        public void SendToAllBut<T>(Node node, T data, int link) where T : class, new() => node.SendButExclude(Write<T>(data), link);

        public void SendToAll<T>(Node node, T data) where T : class, new() => node.Send(Write<T>(data));

        public byte[] Write<T>(T data) where T : class, new() => BitConverter.GetBytes(GetHash<T>()).Concat(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(data))).ToArray();

        public void Register<T>(Action<T> receive) where T : class, new() => _callbacks[GetHash<T>()] = (r, link) => receive(JsonConvert.DeserializeObject<T>(Encoding.UTF8.GetString(r.Skip(8).ToArray())));

        public void Register<T, Link>(Action<T, Link> receive) where T : class, new() => _callbacks[GetHash<T>()] = (r, link) => receive(JsonConvert.DeserializeObject<T>(Encoding.UTF8.GetString(r.Skip(8).ToArray())), (Link)link);

        public bool RemoveSubscription<T>() => _callbacks.Remove(GetHash<T>());

        protected virtual SubscribeDelegate GetCallbackFromData(byte[] reader)
        {
            if (!_callbacks.TryGetValue(BitConverter.ToUInt64(reader, 0), out SubscribeDelegate action))
            {
                Console.WriteLine("Undefined data in Processor");
            }

            return action;
        }

        protected virtual ulong GetHash<T>()
        {
            if (HashCache<T>.Initialized)
                return HashCache<T>.Id;

            ulong hash = 14695981039346656037UL;
            string typeName = typeof(T).FullName;
            for (var i = 0; i < typeName.Length; i++)
            {
                hash ^= typeName[i];
                hash *= 1099511628211UL;
            }
            HashCache<T>.Initialized = true;
            HashCache<T>.Id = hash;
            return hash;
        }
    }

}
