using System;
namespace PocketArcade.UI
{
    // Decorative UI references can be absent in themes that do not supply those visuals.
    [AttributeUsage(AttributeTargets.Field)]
    public sealed class OptionalUIReferenceAttribute : Attribute { }
}
