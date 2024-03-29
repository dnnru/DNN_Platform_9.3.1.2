﻿#region Usings

using System.Collections.Generic;
using System.Linq;
using DotNetNuke.Entities.Host;
using DotNetNuke.Security;

#endregion

namespace DotNetNuke.Common.Utilities
{
    public sealed class ValidationUtils
    {
        internal static string GetDecryptionKey()
        {
            var machineKey = Config.GetDecryptionkey();
            var key = $"{machineKey ?? string.Empty}{Host.GUID.Replace("-", string.Empty)}";
            return FIPSCompliant.EncryptAES(key, key, Host.GUID);
        }

        internal static string ComputeValidationCode(IList<object> parameters)
        {
            if (parameters != null && parameters.Any())
            {
                var checkString = string.Join("_",
                                              parameters.Select(p =>
                                                                {
                                                                    var list = p as IList<string>;
                                                                    if (list != null)
                                                                    {
                                                                        return list.Select(i => i.ToLowerInvariant())
                                                                                   .OrderBy(i => i)
                                                                                   .Aggregate(string.Empty, (current, extension) => current.Append(extension, ", "));
                                                                    }

                                                                    return p.ToString();
                                                                }));

                return PortalSecurity.Instance.Encrypt(GetDecryptionKey(), checkString);
            }

            return string.Empty;
        }

        internal static bool ValidationCodeMatched(IList<object> parameters, string validationCode)
        {
            return validationCode.Equals(ComputeValidationCode(parameters));
        }
    }
}
