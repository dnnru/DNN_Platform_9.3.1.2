#region Usings

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Xml;
using System.Xml.XPath;
using DotNetNuke.Customizations.Security.TrIDEngine.Models;

#endregion

namespace DotNetNuke.Customizations.Security.TrIDEngine
{
    public class PatternEngine
    {
        private readonly ByteString _frontBlock;
        private string _submittedFile;

        public PatternEngine() : this(new List<FileDefPattern>(), 0)
        { }

        public PatternEngine(IEnumerable<FileDefPattern> definitions) : this(definitions, 0)
        { }

        public PatternEngine(IEnumerable<FileDefPattern> definitions, int maxFrontSize)
        {
            Definitions = new List<FileDefPattern>(definitions);
            FileFrontSize = maxFrontSize;
            _frontBlock = new ByteString();
        }

        public int FileFrontSize { get; set; }

        public List<Result> Results { get; } = new List<Result>();
        public List<FileDefPattern> Definitions { get; set; }

        public int DefInMemory => Definitions.Count;

        public string Version => "2.00";

        public void Analyze(Func<string, bool> extensionFilter = null)
        {
            bool flag = true;
            int totalCnt = 0;
            Results.Clear();
            byte[] pBlock = null;

            foreach (FileDefPattern definition in extensionFilter == null
                                                      ? Definitions
                                                      : Definitions.Where(d => !string.IsNullOrWhiteSpace(d.FileExt) && extensionFilter(d.FileExt)))
            {
                if (FileFrontSize < definition.FrontBlockSize)
                {
                    continue;
                }

                int cnt = 0;

                foreach (SomePattern pattern in definition.Patterns)
                {
                    int pos = 0;
                    for (int i = 0; i <= pattern.Len - 1; i++)
                    {
                        if (_frontBlock.Data[pattern.Pos + i] == pattern.Pattern[i])
                        {
                            pos++;
                        }
                    }

                    if (pos == pattern.Len)
                    {
                        cnt += pattern.Len * pattern.Points;
                        continue;
                    }

                    cnt = 0;
                    break;
                }

                if (cnt <= 0)
                {
                    continue;
                }

                if (definition.GlobalStrings.Count > 0)
                {
                    if (flag)
                    {
                        try
                        {
                            using (FileStream fileStream = new FileStream(_submittedFile, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                            using (BinaryReader binaryReader = new BinaryReader(fileStream))
                            {
                                if (fileStream.Length != 0)
                                {
                                    if (fileStream.Length <= 10485760)
                                    {
                                        pBlock = binaryReader.ReadBytes((int) fileStream.Length);
                                    }
                                    else
                                    {
                                        pBlock = binaryReader.ReadBytes(5242880);
                                        Array.Resize(ref pBlock, 10485762);
                                        fileStream.Seek(-5242880L, SeekOrigin.End);
                                        byte[] array = binaryReader.ReadBytes(5242880);

                                        pBlock[5242880] = 124;
                                        array.CopyTo(pBlock, 5242881);
                                    }

                                    flag = false;
                                    Ba2Upper(ref pBlock);
                                }
                            }
                        }
                        catch (Exception)
                        {
                            Results.Clear();
                        }
                    }

                    foreach (ByteString byteString in definition.GlobalStrings)
                    {
                        if (ByteArraySearchCs(pBlock, byteString.Data) != -1)
                        {
                            cnt += byteString.Data.Length * 500;
                            continue;
                        }

                        cnt = 0;
                        break;
                    }
                }

                if (cnt > 0)
                {
                    Result result = new Result();
                    result.FileType = definition.FileType;
                    result.FileExt = definition.FileExt;
                    result.ExtraInfo = definition.ExtraInfo;
                    result.ExtraInfo.FilePts = $"{cnt}/{definition.Patterns.Count}";
                    if (definition.GlobalStrings.Count > 0)
                    {
                        result.ExtraInfo.FilePts = $"{result.ExtraInfo.FilePts}/{definition.GlobalStrings.Count}";
                    }

                    result.FileType = result.FileType + " (" + cnt + "/" + definition.Patterns.Count + ")";
                    result.Points = cnt;
                    Results.Add(result);
                    totalCnt += cnt;
                }
            }

            if (Results.Count > 0)
            {
                for (int i = 0; i <= Results.Count - 1; i++)
                {
                    Results[i].Perc = Results[i].Points * 100 / (float) totalCnt;
                }
            }
        }

        public void ClearDefinitions()
        {
            Definitions.Clear();
        }

        public Result[] GetResultsData(int pResNum)
        {
            if (Results.Count == 0)
            {
                return new Result[1];
            }

            Result[] array = new Result[Results.Count];
            int cnt = 0;
            int[] pointsArray = new int[Results.Count];
            foreach (Result result in Results)
            {
                if (result.Points > 0)
                {
                    array[cnt] = result;
                    pointsArray[cnt] = result.Points;
                    cnt++;
                }
            }

            Array.Sort(pointsArray, array);
            Array.Reverse(array);
            if (pResNum > cnt)
            {
                pResNum = cnt;
            }

            Result[] resultsArray = new Result[pResNum];
            for (int i = 0; i <= pResNum - 1; i++)
            {
                resultsArray[i] = array[i];
            }

            return resultsArray;
        }

        public string[] GetResultsStrings(int pResNum)
        {
            string[] array = new string[1];
            int cnt = 0;
            array[0] = "Unknown!";
            if (Results.Count <= 0)
            {
                return array;
            }

            array = new string[Results.Count];
            int[] pointsArray = new int[Results.Count];
            for (int i = 0; i <= Results.Count - 1; i++)
            {
                Result result = Results[i];
                if (result.Points > 0)
                {
                    array[cnt] = $"{(string.IsNullOrWhiteSpace(result.FileExt) ? "" : "(." + result.FileExt + ") ")}{result.Perc,-5:##0.0}% {result.FileType}";
                    pointsArray[cnt] = result.Points;
                    cnt++;
                }
            }

            Array.Sort(pointsArray, array);
            Array.Reverse(array);
            if (pResNum > cnt)
            {
                pResNum = cnt;
            }

            string[] resultsArray = new string[pResNum];
            for (int i = 0; i <= pResNum - 1; i++)
            {
                resultsArray[i] = array[i];
            }

            return resultsArray;
        }

        public bool IsBinary()
        {
            if (string.IsNullOrWhiteSpace(_submittedFile))
            {
                return false;
            }

            for (int i = 0; i <= FileFrontSize - 1; i++)
            {
                byte b = _frontBlock.Data[i];
                if (b < 9 || b > 126)
                {
                    return true;
                }
            }

            return false;
        }

        public static TrIdEngineConfig LoadDefinitionByXml(string xml, int maxFrontSize)
        {
            TrIdEngineConfig trIdEngineConf = null;
            if (xml != null)
            {
                trIdEngineConf = new TrIdEngineConfig();
                FileDefPattern fileDefPat = new FileDefPattern {ExtraInfo = new ExtraInfo()};

                XPathDocument xPathDocument = new XPathDocument(new StringReader(xml));
                XPathNavigator nav = xPathDocument.CreateNavigator();

                fileDefPat.FileExt = nav.SelectSingleNode("//Info/Ext")?.Value;
                fileDefPat.FileType = nav.SelectSingleNode("//Info/FileType")?.Value;
                fileDefPat.ExtraInfo.FileType = fileDefPat.FileType;
                fileDefPat.ExtraInfo.FileExt = fileDefPat.FileExt;
                fileDefPat.ExtraInfo.AuthorName = nav.SelectSingleNode("//Info/User")?.Value;
                fileDefPat.ExtraInfo.AuthorEMail = nav.SelectSingleNode("//Info/E-Mail")?.Value;
                fileDefPat.ExtraInfo.AuthorHome = nav.SelectSingleNode("//Info/Home")?.Value;
                fileDefPat.ExtraInfo.Remark = nav.SelectSingleNode("//ExtraInfo/Rem")?.Value;
                fileDefPat.ExtraInfo.RelURL = nav.SelectSingleNode("//ExtraInfo/RefURL")?.Value;
                fileDefPat.ExtraInfo.FilesScanned = (int) Math.Round(double.Parse(nav.SelectSingleNode("//General/FileNum")?.Value ?? "0"));
                LoadFrontBlockNode(fileDefPat, nav.Select("//FrontBlock/Pattern"));
                LoadGlobalStringsNode(fileDefPat, nav.Select("//GlobalStrings/String"));

                /*
                XmlDocument xmlDocument = new XmlDocument();
                xmlDocument.LoadXml(xml);
                fileDefPat.FileType = xmlDocument.SelectSingleNode("//Info/FileType")?.InnerText;
                XmlNode xmlNode = xmlDocument.SelectSingleNode("//Info/Ext");
                if (xmlNode != null)
                {
                    fileDefPat.FileExt = xmlNode.InnerText;
                }

                fileDefPat.ExtraInfo.FileType = fileDefPat.FileType;
                fileDefPat.ExtraInfo.FileExt = fileDefPat.FileExt;

                xmlNode = xmlDocument.SelectSingleNode("//Info/User");
                if (xmlNode != null)
                {
                    fileDefPat.ExtraInfo.AuthorName = xmlNode.InnerText;
                }

                xmlNode = xmlDocument.SelectSingleNode("//Info/E-Mail");
                if (xmlNode != null)
                {
                    fileDefPat.ExtraInfo.AuthorEMail = xmlNode.InnerText;
                }

                xmlNode = xmlDocument.SelectSingleNode("//Info/Home");
                if (xmlNode != null)
                {
                    fileDefPat.ExtraInfo.AuthorHome = xmlNode.InnerText;
                }

                xmlNode = xmlDocument.SelectSingleNode("//ExtraInfo/Rem");
                if (xmlNode != null)
                {
                    fileDefPat.ExtraInfo.Remark = xmlNode.InnerText;
                }

                xmlNode = xmlDocument.SelectSingleNode("//ExtraInfo/RefURL");
                if (xmlNode != null)
                {
                    fileDefPat.ExtraInfo.RelURL = xmlNode.InnerText;
                }

                xmlNode = xmlDocument.SelectSingleNode("//General/FileNum");
                if (xmlNode != null)
                {
                    fileDefPat.ExtraInfo.FilesScanned = (int) Math.Round(double.Parse(xmlNode.InnerText));
                }

                XmlNodeList frontBlockNode = xmlDocument.SelectNodes("//FrontBlock/Pattern");
                if (frontBlockNode != null)
                {
                    LoadFrontBlockNode(fileDefPat, frontBlockNode);
                }

                XmlNodeList globalStringsNode = xmlDocument.SelectNodes("//GlobalStrings/String");
                if (globalStringsNode != null)
                {
                    LoadGlobalStringsNode(fileDefPat, globalStringsNode);
                }
                */
                trIdEngineConf.MaxFrontSize = Math.Max(maxFrontSize, fileDefPat.FrontBlockSize);
                trIdEngineConf.Definition = fileDefPat;
            }

            return trIdEngineConf;
        }

        public void LoadDefinitionByFilePath(string fileName)
        {
            if (File.Exists(fileName))
            {
                LoadDefinitionByXml(File.ReadAllText(fileName));
            }
        }

        public void LoadDefinitionByXml(string xml)
        {
            TrIdEngineConfig trIdEngineConf = LoadDefinitionByXml(xml, FileFrontSize);
            if (trIdEngineConf != null)
            {
                Definitions.Add(trIdEngineConf.Definition);
                FileFrontSize = Math.Max(FileFrontSize, trIdEngineConf.MaxFrontSize);
            }
        }

        public void SubmitFile(string fileName)
        {
            if (File.Exists(fileName))
            {
                try
                {
                    using (FileStream fileStream = new FileStream(fileName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                    using (BinaryReader binaryReader = new BinaryReader(fileStream))
                    {
                        if (fileStream.Length != 0)
                        {
                            if (fileStream.Length < FileFrontSize)
                            {
                                FileFrontSize = (int) fileStream.Length;
                            }

                            _frontBlock.Data = binaryReader.ReadBytes(FileFrontSize);
                        }

                        _submittedFile = fileName;
                    }
                }
                catch (Exception)
                {
                    FileFrontSize = 0;
                }
            }
            else
            {
                FileFrontSize = 0;
            }
        }

        private static void LoadFrontBlockNode(FileDefPattern pattern, XPathNodeIterator patterns)
        {
            while (patterns.MoveNext())
            {
                if (patterns.Current != null)
                {
                    SomePattern somePattern = new SomePattern {Pos = int.Parse(patterns.Current.SelectSingleNode("Pos")?.Value.Trim() ?? "0")};
                    string innerText = patterns.Current.SelectSingleNode("Bytes")?.Value.Trim();
                    if (innerText != null)
                    {
                        somePattern.Len = innerText.Length / 2;
                        somePattern.Pattern = new byte[somePattern.Len + 1];
                        for (int i = 1; i <= somePattern.Len; i++)
                        {
                            somePattern.Pattern[i - 1] = (byte) Math.Round((double) int.Parse(innerText.Substring((i - 1) * 2, 2), NumberStyles.HexNumber));
                        }
                    }

                    pattern.FrontBlockSize = somePattern.Pos + somePattern.Len;
                    somePattern.XM = false;
                    somePattern.Points = 1;
                    if (somePattern.Pos == 0)
                    {
                        somePattern.Points = 1000;
                    }

                    pattern.Patterns.Add(somePattern);
                }
            }
        }

        private static void LoadFrontBlockNode(FileDefPattern pattern, XmlNodeList list)
        {
            foreach (XmlNode node in list)
            {
                SomePattern somePattern = new SomePattern {Pos = int.Parse(node.SelectSingleNode("Pos")?.InnerText.Trim() ?? "0")};
                string innerText = node.SelectSingleNode("Bytes")?.InnerText.Trim();
                if (innerText != null)
                {
                    somePattern.Len = innerText.Length / 2;
                    somePattern.Pattern = new byte[somePattern.Len + 1];
                    for (int i = 1; i <= somePattern.Len; i++)
                    {
                        somePattern.Pattern[i - 1] = (byte) Math.Round((double) int.Parse(innerText.Substring((i - 1) * 2, 2), NumberStyles.HexNumber));
                    }
                }

                pattern.FrontBlockSize = somePattern.Pos + somePattern.Len;
                somePattern.XM = false;
                somePattern.Points = 1;
                if (somePattern.Pos == 0)
                {
                    somePattern.Points = 1000;
                }

                pattern.Patterns.Add(somePattern);
            }
        }

        private static void LoadGlobalStringsNode(FileDefPattern pattern, XPathNodeIterator globalStrings)
        {
            while (globalStrings.MoveNext())
            {
                if (globalStrings.Current != null)
                {
                    string innerText = globalStrings.Current.Value.Trim();
                    ByteString byteString = new ByteString {Data = new byte[innerText.Length]};

                    for (int j = 0; j <= innerText.Length - 1; j++)
                    {
                        byteString.Data[j] = innerText[j] > 255 ? Convert.ToByte(0) : Convert.ToByte(innerText[j]);
                        if (byteString.Data[j] == 39)
                        {
                            byteString.Data[j] = 0;
                        }
                    }

                    Ba2Upper(ref byteString.Data);
                    pattern.GlobalStrings.Add(byteString);
                }
            }
        }

        private static void LoadGlobalStringsNode(FileDefPattern pattern, XmlNodeList list)
        {
            foreach (XmlNode node in list)
            {
                string innerText = node.InnerText.Trim();
                ByteString byteString = new ByteString {Data = new byte[innerText.Length]};

                for (int j = 0; j <= innerText.Length - 1; j++)
                {
                    byteString.Data[j] = innerText[j] > 255 ? Convert.ToByte(0) : Convert.ToByte(innerText[j]);
                    if (byteString.Data[j] == 39)
                    {
                        byteString.Data[j] = 0;
                    }
                }

                Ba2Upper(ref byteString.Data);
                pattern.GlobalStrings.Add(byteString);
            }
        }

        private static void Ba2Upper(ref byte[] block)
        {
            for (int i = 0; i <= block.Length - 1; i++)
            {
                byte b = block[i];
                if (b > 96 && b < 123)
                {
                    block[i] = (byte) (b - 32);
                }
            }
        }

        private static int ByteArraySearchBm(byte[] array, byte[] pattern)
        {
            BoyerMoore bm = new BoyerMoore(pattern);
            int idx = bm.Search(array, true).Count();
            return idx > 0 ? idx : -1;
        }

        private static int ByteArraySearchCs(byte[] array, byte[] pattern)
        {
            int[] idxArray = new int[256];
            int index = 0;
            do
            {
                idxArray[index] = pattern.Length;
                index++;
            }
            while (index <= 255);

            for (index = 1; index <= pattern.Length; index++)
            {
                byte b = pattern[index - 1];
                idxArray[b] = pattern.Length - index;
            }

            int result = pattern.Length;
            index = pattern.Length;
            do
            {
                if (array[result - 1] == pattern[index - 1])
                {
                    result--;
                    index--;
                }
                else
                {
                    result = pattern.Length - index + 1 <= idxArray[array[result - 1]] ? result + idxArray[array[result - 1]] : result + pattern.Length - index + 1;
                    index = pattern.Length;
                }
            }
            while (index >= 1 && result <= array.Length);

            if (result >= array.Length)
            {
                return -1;
            }

            return result;
        }
    }
}
