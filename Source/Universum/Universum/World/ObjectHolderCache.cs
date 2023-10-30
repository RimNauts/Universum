using System.Collections.Generic;
using Verse;

namespace Universum.World {
    public class ObjectHolderCache : GameComponent {
        public static ObjectHolderCache instance;

        private readonly Dictionary<int, ObjectHolder> objectHolders = new Dictionary<int, ObjectHolder>();

        public ObjectHolderCache(Verse.Game game) : base() => instance = this;

        public static void Add(ObjectHolder objectHolder) => instance.objectHolders[objectHolder.Tile] = objectHolder;

        public static void Remove(ObjectHolder objectHolder) => instance.objectHolders.Remove(objectHolder.Tile);

        public static void Remove(int tile) => instance.objectHolders.Remove(tile);

        public static void Clear() => instance.objectHolders.Clear();

        public static bool Exists(int tile) => instance.objectHolders.ContainsKey(tile);

        public static ObjectHolder Get(int tile) => instance.objectHolders.ContainsKey(tile) ? instance.objectHolders[tile] : null;
    }
}
