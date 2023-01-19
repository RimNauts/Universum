namespace Universum {
    [RimWorld.DefOf]
    public static class DefOf {
        public static ObjectsDef Objects;

        static DefOf() => RimWorld.DefOfHelper.EnsureInitializedInCtor(typeof(DefOf));
    }
}
