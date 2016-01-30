using Steamworks;
using System;
using System.Text.RegularExpressions;

namespace QBan
{
    public static class Extensions
    {
        public static string Truncate(this string value, int maxLength)
        {
            if (string.IsNullOrEmpty(value)) return value;
            return value.Length <= maxLength ? value : value.Substring(0, maxLength);
        }

        // Returns a Steamworks.CSteamID on out from a string, and returns true if it is a CSteamID.
        public static bool isCSteamID(this string sCSteamID, out CSteamID cSteamID)
        {
            ulong ulCSteamID;
            cSteamID = (CSteamID)0;
            if (ulong.TryParse(sCSteamID, out ulCSteamID))
            {
                if ((ulCSteamID >= 0x0110000100000000 && ulCSteamID <= 0x0170000000000000) || ulCSteamID == 0)
                {
                    cSteamID = (CSteamID)ulCSteamID;
                    return true;
                }
            }
            return false;
        }

        // Sanitize strings with binary control characters 0x00-0x1f.
        public static string Sanitze(this string value)
        {
            if (value == null)
                return null;
            return Regex.Replace(value, @"([\u0000-\u001F])+", " ");
        }
    }
}
