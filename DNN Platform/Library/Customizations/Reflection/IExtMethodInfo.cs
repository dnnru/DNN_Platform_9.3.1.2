#region Usings

using System;

#endregion

namespace DotNetNuke.Customizations.Reflection
{
    public interface IExtMethodInfo
    {
        string MethodName { get; }
        Type ExtendedType { get; }
    }
}
