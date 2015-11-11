using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using SDG.Unturned;
using Steamworks;
using Rocket.Core.Logging;

namespace QBan
{
    public class DataStore
    {
        private static Dictionary<CSteamID, BanDataValues> QBanData = new Dictionary<CSteamID, BanDataValues>();

        public DataStore()
        {
            Initialize();
        }

        // Initialize/load the ban data here.
        private static void Initialize()
        {
            DateTime unsetDateTime = new DateTime(0);
            foreach (BanDataValues banData in QBan.Instance.Configuration.Instance.Bans)
            {
                try
                {
                    // Sanity checks for loaded data.
                    if (banData.targetCharName == null)
                        banData.targetCharName = "";
                    if (banData.targetSteamName == null)
                        banData.targetSteamName = "";
                    if (banData.adminCharName == null)
                        banData.adminCharName = "";
                    if (banData.adminSteamName == null)
                        banData.adminSteamName = "";
                    if (banData.reason == null)
                        banData.reason = "Banned.";
                    if (banData.targetSID == (CSteamID)0)
                    {
                        Logger.LogWarning("Loading: Bad target Steam ID in record, skipping.");
                        continue;
                    }
                    if (banData.setTime == unsetDateTime)
                    {
                        Logger.LogWarning("Loading: Bad set time in record, skipping.");
                        continue;
                    }
                    // Now add the record to the dictionary.
                    QBanData.Add(banData.targetSID, banData);
                }
                catch
                {
                    Logger.LogWarning("Error: Duplicate record in the config file.");
                }
            }
        }

        public void Unload()
        {
            QBanData.Clear();
        }

        // Set ban data and save out to file.
        public bool SetQBanData(CSteamID key, BanDataValues data)
        {
            try
            {
                if (QBanData.ContainsKey(key))
                    QBanData.Remove(key);

                QBanData.Add(key, data);
                SaveToFile();
                return true;
            }
            catch
            {
                Logger.LogWarning("Error, Unable to set ban data.");
                return false;
            }
        }

        // Remove ban data and save out to file.
        public bool RemoveQBanData(CSteamID key)
        {
            if (QBanData.ContainsKey(key))
            {
                QBanData.Remove(key);
                SaveToFile();
                return true;
            }
            return false;
        }

        // Search by playername.
        public BanDataValues GetQBanData(string playername)
        {
            return QBanData.Values.FirstOrDefault(contents => contents.targetCharName.ToLower().Contains(playername.ToLower()) || contents.targetSteamName.ToLower().Contains(playername.ToLower()));
        }

        // Get exact match by CSteamID.
        public BanDataValues GetQBanData(CSteamID cSteamID)
        {
            BanDataValues result;
            if (QBanData.TryGetValue(cSteamID, out result))
                return result;
            return null;
        }

        // Search for IP bans by CSteamID.
        public BanDataValues GetIPQBanData(uint ip)
        {
            return QBanData.Values.FirstOrDefault(contents => contents.uIP == ip && contents.isIPBan);
        }

        // Grab a list of bans for the bans command.
        public KeyValuePair<int, List<BanDataValues>> GetQBanDataList(string searchString, int count, int pagination)
        {
            // Grab a list of matches of the searchString out of the QBanData dictionary.
            List<BanDataValues> matches = new List<BanDataValues>();
            if (searchString == String.Empty)
                matches = QBanData.Values.OrderBy(o => o.setTime).ToList();
            else
                matches = QBanData.Values.Where(contents => contents.targetCharName.ToLower().Contains(searchString.ToLower()) || contents.targetSteamName.ToLower().Contains(searchString.ToLower())).OrderBy(o => o.setTime).ToList();

            int matchCount = matches.Count;
            int index;
            int numbeOfRecords;

            // Do the math for the pagenation so that no negative numbers are entered into matches.GetRange.
            if (matchCount - (count * pagination) <= 0)
            {
                index = 0;
                numbeOfRecords = matchCount - count * (pagination - 1);
                if (numbeOfRecords < 0)
                    numbeOfRecords = 0;
            }
            else
            {
                index = matchCount - count * pagination;
                numbeOfRecords = count;
            }
            // Return index posistion and the list.
            return new KeyValuePair<int, List<BanDataValues>>(index + 1, new List<BanDataValues>(matches.GetRange(index, numbeOfRecords)));
        }

        // Check for expired bans in the ban data, remove expired.
        public void CheckExpiredBanData()
        {
            List<BanDataValues> expiredList = QBanData.Values.Where(contents => (contents.duration - (DateTime.Now - contents.setTime).TotalSeconds) <= 0).ToList();
            foreach (BanDataValues banData in expiredList)
            {
                Logger.Log(String.Format("Ban for player: {0}[{1}]({2}), has expired.",banData.targetCharName, banData.targetSteamName, banData.targetSID));
                QBanData.Remove(banData.targetSID);
                SteamBlacklist.unban(banData.targetSID);
                if (QBan.Instance.Configuration.Instance.EnableExpiredExport && expiredList.Count != 0)
                    QBan.Instance.Configuration.Instance.ExpiredBans.Add(banData);
            }
            if (expiredList.Count != 0)
            {
                SaveToFile();
                SteamBlacklist.save();
            }
        }

        // Save to file.
        private static void SaveToFile()
        {
            // Dump the data in the QBanData dictionary to the configuration and save.
            QBan.Instance.Configuration.Instance.Bans = QBanData.Values.ToList();
            QBan.Instance.Configuration.Save();
        }
    }
}
