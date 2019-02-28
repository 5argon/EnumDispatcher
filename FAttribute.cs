namespace E7.EnumDispatcher
{
    /// <summary>
    /// F is short for Flag. Each action belongs to one category, but can be attached with multiple flags.
    /// Use `[F(___, ___, ...)]` to define flags on your actions. Put it before your enum's value name.
    /// </summary>
    [System.AttributeUsage(System.AttributeTargets.Field, Inherited = false, AllowMultiple = false)]
    public class FAttribute : System.Attribute
    {
        internal readonly string[] flags;
        public FAttribute(params string[] flags)
        {
            this.flags = flags;
        }
    }
}
