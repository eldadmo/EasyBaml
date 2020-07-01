using System;
using System.Text;

namespace EasyBamlAddin.UidManagement
{
    public class UidGenerationStrategy : IUidGenerationStrategy
    {
        //private int uidSequenceFallbackCount;
        //private const string UidFallbackSequence = "_Uid";
        private const char UidSeparator = '_';
        private const int MaxLocalizedStringUsage = 20;

        public string GenerateUid(Uid uid, Func<string, bool> uidAvailabilityChecker)
        {
            if ((uid.FrameworkElementName != null) && uidAvailabilityChecker(uid.FrameworkElementName))
            {
                return uid.FrameworkElementName;
            }

            string keyBase = GetElementLocalName(uid.ElementName);
            bool indexMandatory = true;

            if (!String.IsNullOrEmpty(uid.LocalizableString))
            {
                string key = GenerateKeyFromLocalizedString(uid.LocalizableString);
                if (!String.IsNullOrEmpty(key))
                {
                    keyBase = keyBase + UidSeparator + key;
                    indexMandatory = false;
                }
            }

            if (!indexMandatory && uidAvailabilityChecker(keyBase))
            {
                return keyBase;
            }

            keyBase += UidSeparator;
            for (long i = 0; ; i++)
            {
                string resultUid = keyBase + i;
                if (uidAvailabilityChecker(resultUid))
                {
                    return resultUid;
                }
                if (i == Int64.MaxValue)
                {
                    throw new Exception("Cannot generate Uid. This is impossible!");
                }
            }
        }

        private static string GetElementLocalName(string elementFullName)
        {
            int num = elementFullName.LastIndexOf(':');
            if (num > 0)
            {
                return elementFullName.Substring(num + 1);
            }
            return elementFullName;
        }

        private static string GenerateKeyFromLocalizedString(string s)
        {
            var sb = new StringBuilder();

            // Capitalize first letter and after a space
            bool capitalize = true;
            for (int i = 0; i < s.Length && sb.Length < MaxLocalizedStringUsage; i++)
            {
                if (sb.Length == 0 && Char.IsDigit(s, i))
                {
                    sb.Append('S');
                }
                if (Char.IsLetterOrDigit(s, i))
                {
                    if (capitalize)
                    {
                        sb.Append(Char.ToUpper(s[i]));
                        capitalize = false;
                    }
                    else
                    {
                        sb.Append(s[i]);
                    }
                }
                else if (char.IsWhiteSpace(s, i))
                {
                    capitalize = true;
                }
            }

            return sb.ToString();
        }
    }
}
