#region Usings

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using DotNetNuke.Customizations.Security.TrIDEngine.Models;

#endregion

namespace DotNetNuke.Customizations.Security.TrIDEngine
{
    public class Engine
    {
        public Engine()
        {
            PatternEngine = new PatternEngine();
            LoadFromResources();
        }

        public Engine(string defsDir)
        {
            PatternEngine = new PatternEngine();
            LoadFromDirectory(defsDir);
        }

        public Engine(IEnumerable<FileDefPattern> definitions, int maxFrontSize)
        {
            PatternEngine = new PatternEngine(definitions, maxFrontSize);
        }

        public PatternEngine PatternEngine { get; }

        public Result[] GetExtensions(string filePath)
        {
            PatternEngine.SubmitFile(filePath);
            PatternEngine.Analyze();
            return PatternEngine.GetResultsData(int.MaxValue);
        }

        public string[] GetExtensionsStrings(string filePath)
        {
            PatternEngine.SubmitFile(filePath);
            PatternEngine.Analyze();
            return PatternEngine.GetResultsStrings(int.MaxValue);
        }

        public bool ValidateFileExtension(string filePath)
        {
            string extension = Path.GetExtension(filePath);
            if (string.IsNullOrWhiteSpace(extension))
            {
                return false;
            }

            PatternEngine.SubmitFile(filePath);
            PatternEngine.Analyze(e => !string.IsNullOrWhiteSpace(e) && e.Equals(extension.TrimStart('.'), StringComparison.InvariantCultureIgnoreCase));

            return PatternEngine.Results.Count > 0;
        }

        public string GetExtensionByFileContent(string filePath)
        {
            PatternEngine.SubmitFile(filePath);
            PatternEngine.Analyze();
            string fileExt = PatternEngine.GetResultsData(1).FirstOrDefault()?.ExtraInfo.FileExt;
            if (fileExt != null)
            {
                return "." + fileExt;
            }

            return null;
        }

        public string GetBestExtension(string filePath, string defaultExtension = "unknown")
        {
            Result[] extensions = GetExtensions(filePath);
            var anon = extensions?.GroupBy(result => result.FileExt)
                                 .Select(results => new {result = results.First(), perc = results.Sum(result => result.Perc)})
                                 .OrderByDescending(arg => arg.perc)
                                 .FirstOrDefault();

            return anon != null && anon.perc > 10f ? anon.result.FileExt : defaultExtension;
        }

        private void LoadFromResources()
        {
            Assembly resourceAssembly = typeof(Engine).Assembly;
            PatternEngine.ClearDefinitions();
            foreach (string name in resourceAssembly.GetManifestResourceNames())
            {
                if (name.EndsWith(".trid.xml", StringComparison.InvariantCultureIgnoreCase))
                {
                    try
                    {
                        using (Stream stream = resourceAssembly.GetManifestResourceStream(name))
                        {
                            using (var reader = new StreamReader(stream ?? throw new InvalidOperationException()))
                            {
                                string xml = reader.ReadToEnd();

                                PatternEngine.LoadDefinitionByXml(xml);
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e);
                    }
                }
            }
        }

        private void LoadFromDirectory(string defsDir)
        {
            if (Directory.Exists(defsDir))
            {
                PatternEngine.ClearDefinitions();
                string[] files = Directory.GetFiles(defsDir, "*.trid.xml", SearchOption.AllDirectories);
                foreach (string file in files)
                {
                    try
                    {
                        PatternEngine.LoadDefinitionByFilePath(file);
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e);
                    }
                }
            }
        }
    }
}
