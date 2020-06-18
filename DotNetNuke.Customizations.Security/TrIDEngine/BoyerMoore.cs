#region Usings

using System;
using System.Collections.Generic;

#endregion

namespace DotNetNuke.Customizations.Security.TrIDEngine
{
    public sealed class BoyerMoore
    {
        private readonly int[] _charTable;
        private readonly byte[] _needle;
        private readonly int[] _offsetTable;

        public BoyerMoore(byte[] needle)
        {
            _needle = needle;
            _charTable = MakeByteTable(needle);
            _offsetTable = MakeOffsetTable(needle);
        }

        public IEnumerable<int> Search(byte[] haystack, bool onlyFirst = false)
        {
            if (_needle.Length == 0)
            {
                yield break;
            }

            bool found = false;

            for (int i = _needle.Length - 1; i < haystack.Length;)
            {
                int j;

                for (j = _needle.Length - 1; _needle[j] == haystack[i]; --i, --j)
                {
                    if (j != 0)
                    {
                        continue;
                    }

                    yield return i;
                    found = true;
                    i += _needle.Length - 1;
                    break;
                }

                i += Math.Max(_offsetTable[_needle.Length - 1 - j], _charTable[haystack[i]]);
                if (onlyFirst && found)
                {
                    break;
                }
            }
        }

        private static int[] MakeByteTable(byte[] needle)
        {
            const int ALPHABET_SIZE = 256;
            int[] table = new int[ALPHABET_SIZE];

            for (int i = 0; i < table.Length; ++i)
            {
                table[i] = needle.Length;
            }

            for (int i = 0; i < needle.Length - 1; ++i)
            {
                table[needle[i]] = needle.Length - 1 - i;
            }

            return table;
        }

        private static int[] MakeOffsetTable(byte[] needle)
        {
            int[] table = new int[needle.Length];
            int lastPrefixPosition = needle.Length;

            for (int i = needle.Length - 1; i >= 0; --i)
            {
                if (IsPrefix(needle, i + 1))
                {
                    lastPrefixPosition = i + 1;
                }

                table[needle.Length - 1 - i] = lastPrefixPosition - i + needle.Length - 1;
            }

            for (int i = 0; i < needle.Length - 1; ++i)
            {
                int slen = SuffixLength(needle, i);
                table[slen] = needle.Length - 1 - i + slen;
            }

            return table;
        }

        private static bool IsPrefix(byte[] needle, int p)
        {
            for (int i = p, j = 0; i < needle.Length; ++i, ++j)
            {
                if (needle[i] != needle[j])
                {
                    return false;
                }
            }

            return true;
        }

        private static int SuffixLength(byte[] needle, int p)
        {
            int len = 0;

            for (int i = p, j = needle.Length - 1; i >= 0 && needle[i] == needle[j]; --i, --j)
            {
                ++len;
            }

            return len;
        }
    }
}
