#region Usings

using System;

#endregion

namespace DotNetNuke.Customizations.Security.TrIDEngine.Models
{
    [Serializable]
    public class Result
    {
        public ExtraInfo ExtraInfo;
        public string FileExt;
        public string FileType;
        public float Perc;
        public int Points;
    }
}
