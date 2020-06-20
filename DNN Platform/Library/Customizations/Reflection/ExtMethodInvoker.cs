#region Usings

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

#endregion

namespace DotNetNuke.Customizations.Reflection
{
    public class ExtMethodInvoker
    {
        public ExtMethodInvoker(string partialAssemblyName)
        {
            ExtensionLibAssembly = partialAssemblyName.LoadAssembly();
            if (ExtensionLibAssembly == null)
            {
                throw new TypeLoadException($"Cannot find assembly that has partial name {{{partialAssemblyName}}}");
            }
        }

        public Assembly ExtensionLibAssembly { get; }

        public void Invoke(IExtMethodInfo extMethodInfo, params object[] extMethodParams)
        {
            if (string.IsNullOrWhiteSpace(extMethodInfo.MethodName))
            {
                throw new ArgumentException($"{nameof(extMethodInfo.MethodName)} is empty");
            }

            if (extMethodParams.Length < 1)
            {
                throw new TargetParameterCountException($"Invoke Extension method {extMethodInfo.MethodName}() must provide at least the extended type parameter!");
            }

            if (extMethodInfo.ExtendedType == null)
            {
                Invoke(extMethodInfo.MethodName, extMethodParams);
                return;
            }

            IEnumerable<MethodInfo> targetExtMethods = ExtensionLibAssembly.GetExtensionMethods(extMethodInfo);
            MethodInfo[] methodInfos = targetExtMethods as MethodInfo[] ?? targetExtMethods.ToArray();

            if (!methodInfos.Any())
            {
                throw new MissingMethodException(extMethodInfo.MethodName);
            }

            methodInfos.First().Invoke(null, extMethodParams);
        }

        public void Invoke(string methodName, params object[] extMethodParams)
        {
            if (extMethodParams.Length < 1)
            {
                throw new TargetParameterCountException($"Invoke Extension method {methodName}() must provide at least the extended type parameter!");
            }

            IEnumerable<MethodInfo> targetExtMethods = ExtensionLibAssembly.GetExtensionMethods(extMethodParams.First().GetType(), methodName);
            MethodInfo[] methodInfos = targetExtMethods as MethodInfo[] ?? targetExtMethods.ToArray();
            if (!methodInfos.Any())
            {
                throw new MissingMethodException(methodName);
            }

            Exception notMatchRunEx = null;
            foreach (MethodInfo methodInfo in methodInfos.Where(x => x.GetParameters().Length == extMethodParams.Length))
            {
                try
                {
                    methodInfo.Invoke(null, extMethodParams);
                    notMatchRunEx = null;
                    break;
                }
                catch (ArgumentException ex)
                {
                    notMatchRunEx = ex;
                }
                catch (TargetInvocationException ex)
                {
                    if (ex.InnerException != null)
                    {
                        throw ex.InnerException;
                    }

                    throw;
                }
            }

            if (notMatchRunEx != null)
            {
                throw notMatchRunEx;
            }
        }

        public TReturn Invoke<TReturn>(IExtMethodInfo extMethodInfo, params object[] extMethodParams)
        {
            if (string.IsNullOrWhiteSpace(extMethodInfo.MethodName))
            {
                throw new ArgumentException($"{nameof(extMethodInfo.MethodName)} is empty");
            }

            if (extMethodParams.Length < 1)
            {
                throw new TargetParameterCountException($"Invoke Extension method {extMethodInfo.MethodName}() must provide at least the extended type parameter!");
            }

            if (extMethodInfo.ExtendedType == null)
            {
                return Invoke<TReturn>(extMethodInfo.MethodName, extMethodParams);
            }

            IEnumerable<MethodInfo> targetExtMethods = ExtensionLibAssembly.GetExtensionMethods(extMethodInfo);
            MethodInfo[] methodInfos = targetExtMethods as MethodInfo[] ?? targetExtMethods.ToArray();

            if (!methodInfos.Any())
            {
                throw new MissingMethodException(extMethodInfo.MethodName);
            }

            object invokeResult = null;
            Exception notMatchRunEx = null;

            foreach (MethodInfo methodInfo in methodInfos.Where(x => x.GetParameters().Length == extMethodParams.Length))
            {
                try
                {
                    invokeResult = methodInfo.Invoke(null, extMethodParams);
                    notMatchRunEx = null;
                    break;
                }
                catch (ArgumentException ex)
                {
                    notMatchRunEx = ex;
                }
                catch (TargetInvocationException ex)
                {
                    if (ex.InnerException != null)
                    {
                        throw ex.InnerException;
                    }

                    throw;
                }
            }

            if (notMatchRunEx != null)
            {
                throw notMatchRunEx;
            }

            if (invokeResult == null)
            {
                return default(TReturn);
            }

            return (TReturn) invokeResult;
        }

        public TReturn Invoke<TReturn>(string methodName, params object[] extMethodParams)
        {
            if (extMethodParams.Length < 1)
            {
                throw new TargetParameterCountException($"Invoke Extension method {methodName}() must provide at least the extended type parameter!");
            }

            MethodInfo targetExtMethod = ExtensionLibAssembly.GetExtensionMethods(extMethodParams.First().GetType(), methodName).FirstOrDefault();

            if (targetExtMethod == null)
            {
                throw new MissingMethodException(methodName);
            }

            object invokeResult;

            try
            {
                invokeResult = targetExtMethod.Invoke(null, extMethodParams);
            }
            catch (TargetInvocationException ex)
            {
                if (ex.InnerException != null)
                {
                    throw ex.InnerException;
                }

                throw;
            }

            if (invokeResult == null)
            {
                return default(TReturn);
            }

            return (TReturn) invokeResult;
        }
    }
}
