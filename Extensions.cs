using Steamworks;
using System;

namespace QBan
{
    public static class Extensions
    {
        public static string Truncate(this string value, int maxLength)
        {
            if (string.IsNullOrEmpty(value)) return value;
            return value.Length <= maxLength ? value : value.Substring(0, maxLength);
        }

        // Returns a Steamworks.CSteamID from a string, throws a FormatException if the string isn't a valid unsigned number, or isn't in the valid range.
        public static CSteamID StringToCSteamID(this string sCSteamID)
        {
            ulong ulCSteamID;
            if (ulong.TryParse(sCSteamID, out ulCSteamID))
            {
                if ((ulCSteamID >= 0x0110000100000000 && ulCSteamID <= 0x0170000000000000) || ulCSteamID == 0)
                {
                    return (CSteamID)ulCSteamID;
                }
                throw new FormatException(String.Format("Unable to convert {0} to a CSteamID, not in the valid range.", sCSteamID));
            }
            throw new FormatException(String.Format("Unable to convert {0} to a CSteamID, not a valid unsigned number.", sCSteamID));
        }
    }
}
