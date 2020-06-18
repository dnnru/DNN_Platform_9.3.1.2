using System;

namespace DotNetNuke.Customizations.Security.TrIDEngine.Models
{
    [Serializable]
    public class SomePattern
    {
        public byte[] Pattern;

        public int Pos;

        public int Len;

        public int Points;

        // ReSharper disable once InconsistentNaming
        public bool XM;
    }
}
