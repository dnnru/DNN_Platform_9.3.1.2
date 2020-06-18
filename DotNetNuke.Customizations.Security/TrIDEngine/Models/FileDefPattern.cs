#region Usings

using System;
using System.Collections.Generic;

#endregion

namespace DotNetNuke.Customizations.Security.TrIDEngine.Models
{
    [Serializable]
    public class FileDefPattern
    {
        public readonly List<ByteString> GlobalStrings = new List<ByteString>();
        public readonly List<SomePattern> Patterns = new List<SomePattern>();
        public ExtraInfo ExtraInfo;
        public string FileExt;
        public string FileType;
        public int FrontBlockSize;
    }
}
