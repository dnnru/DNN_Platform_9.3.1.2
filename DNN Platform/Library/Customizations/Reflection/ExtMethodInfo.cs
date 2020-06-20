#region Usings

using System;

#endregion

namespace DotNetNuke.Customizations.Reflection
{
    public class ExtMethodInfo : IExtMethodInfo
    {
        public string MethodName { get; set; }
        public Type ExtendedType { get; set; } = null;
    }
}
