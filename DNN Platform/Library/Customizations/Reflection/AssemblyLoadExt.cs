#region Usings

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;

#endregion

namespace DotNetNuke.Customizations.Reflection
{
    public static class AssemblyLoadExt
    {
        public static Assembly LoadAssembly(this string assemblyPartialName)
        {
            Assembly[] loadedAssemblies = AppDomain.CurrentDomain.GetAssemblies();

            Assembly target = loadedAssemblies.FirstOrDefault(a => a.GetName().Name.Equals(assemblyPartialName));
            if (target != null)
            {
                return target;
            }

            foreach (Assembly assembly in loadedAssemblies)
            {
                AssemblyName refAssembly = assembly.GetReferencedAssemblies().FirstOrDefault(_ => _.Name.Equals(assemblyPartialName));
                if (refAssembly != null)
                {
                    Assembly ret = Assembly.Load(refAssembly);
                    return ret;
                }
            }

            Assembly mainAssembly = Assembly.GetExecutingAssembly();
            AssemblyName assemblyName = mainAssembly.GetReferencedAssemblies().FirstOrDefault(_ => _.Name.Equals(assemblyPartialName));

            return assemblyName != null ? Assembly.Load(assemblyName) : ForceLoadAssembly(assemblyPartialName);
        }

        private static Assembly ForceLoadAssembly(string assemblyPartialName)
        {
            Assembly loadedAssembly = null;
            Exception loadException = null;
            DirectoryInfo executeDirectoryInfo = GetExecutingDirectory();
            List<string> assemblyNames = new List<string>();

            if (assemblyPartialName.EndsWith(".dll", StringComparison.InvariantCultureIgnoreCase) ||
                assemblyPartialName.EndsWith(".exe", StringComparison.InvariantCultureIgnoreCase))
            {
                assemblyNames.Add(assemblyPartialName);
            }
            else
            {
                assemblyNames.Add($"{assemblyPartialName}.dll");
                assemblyNames.Add($"{assemblyPartialName}.exe");
            }

            foreach (string assemblyPath in assemblyNames.Select(assemblyName => FindFileInPath(assemblyName, executeDirectoryInfo.FullName))
                                                         .Where(assemblyPath => !string.IsNullOrEmpty(assemblyPath)))
            {
                try
                {
                    loadedAssembly = Assembly.LoadFile(assemblyPath);
                    loadException = null;
                    break;
                }
                catch (Exception ex)
                {
                    loadException = ex;
                }
            }

            if (loadException != null)
            {
                throw loadException;
            }

            return loadedAssembly;
        }

        private static string FindFileInPath(string filename, string path)
        {
            filename = filename.ToLower();

            foreach (string fullFile in Directory.GetFiles(path))
            {
                string file = Path.GetFileName(fullFile).ToLower();
                if (file == filename)
                {
                    return fullFile;
                }
            }

            foreach (string dir in Directory.GetDirectories(path))
            {
                string file = FindFileInPath(filename, dir);
                if (!string.IsNullOrEmpty(file))
                {
                    return file;
                }
            }

            return null;
        }

        private static DirectoryInfo GetExecutingDirectory()
        {
            string codeBase = Assembly.GetExecutingAssembly().CodeBase;
            UriBuilder uri = new UriBuilder(codeBase);
            string path = Uri.UnescapeDataString(uri.Path);
            //var path = new Uri(Assembly.GetEntryAssembly()?.GetName().CodeBase ?? string.Empty);
            return new FileInfo(path).Directory;
        }

        public static IEnumerable<MethodInfo> GetExtensionMethods(this Assembly assembly, IExtMethodInfo extMethodInfo)
        {
            return GetExtensionMethods(assembly, extMethodInfo.ExtendedType, extMethodInfo.MethodName);
        }

        public static IEnumerable<MethodInfo> GetExtensionMethods(this Assembly assembly, Type extendedType, string extensionMethodName)
        {
            IEnumerable<MethodInfo> extMethodInfos = assembly.GetTypes()
                                                             .Where(type => type.IsSealed && !type.IsGenericType && !type.IsNested)
                                                             .SelectMany(type => type.GetMethods(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic),
                                                                         (type, method) => new {type, method})
                                                             .Where(t => t.method.IsDefined(typeof(ExtensionAttribute), false))
                                                             .Where(t => t.method.Name == extensionMethodName)
                                                             .Where(t => t.method.GetParameters()[0].ParameterType == extendedType)
                                                             .Select(t => t.method);

            return extMethodInfos;
        }
    }
}
