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
        private static string QBansBansFile = "Plugins/QBans/BansData.txt";
        private static string QBansBansExpiredExportFile = "Plugins/QBans/BansData_Expired.txt";
        private static Dictionary<CSteamID, BanDataValues> QBanData = new Dictionary<CSteamID, BanDataValues>();

        public DataStore()
        {
            Initialize();
        }

        // Initialize/load the ban data here.
        private static void Initialize()
        {
            // Check to see if we need to import data from the legacy data files.
            if (File.Exists(QBansBansExpiredExportFile) && QBan.Instance.Configuration.Instance.ExpiredBans.Count == 0)
                Legacy(QBansBansExpiredExportFile);
            if (File.Exists(QBansBansFile) && QBan.Instance.Configuration.Instance.Bans.Count == 0)
                Legacy(QBansBansFile);
            else
            {
                foreach (BanDataValues banData in QBan.Instance.Configuration.Instance.Bans)
                {
                    try
                    {
                        QBanData.Add(banData.targetSID, banData);
                    }
                    catch
                    {
                        Logger.LogWarning("Error: Duplicate record in the config file.");
                    }
                }
            }
        }

        private static void Legacy(string file)
        {
            string[] lines = File.ReadAllLines(file);
            int i = 0;
            foreach (string value in lines)
            {
                i++;
                if (value != "" && !value.StartsWith("##"))
                {
                    //use new style string splitting delimiter or old one based on what it matches.
                    String[] componentsFromSerial;
                    if (value.Contains("><"))
                        componentsFromSerial = value.Split(new String[] { "><" }, StringSplitOptions.None);
                    else
                        componentsFromSerial = value.Split(new char[] { '/' }, StringSplitOptions.None);

                    if (componentsFromSerial.Length == 9)
                    {
                        try
                        {
                            uint banDuration;
                            long banTime;
                            uint.TryParse(componentsFromSerial[7], out banDuration);
                            long.TryParse(componentsFromSerial[8], out banTime);

                            BanDataValues BanDataValue = new BanDataValues();
                            BanDataValue.targetSID = componentsFromSerial[0].StringToCSteamID();
                            BanDataValue.targetCharName = componentsFromSerial[1];
                            BanDataValue.targetSteamName = componentsFromSerial[2];

                            BanDataValue.adminSID = componentsFromSerial[3].StringToCSteamID();
                            BanDataValue.adminCharName = componentsFromSerial[4];
                            BanDataValue.adminSteamName = componentsFromSerial[5];

                            BanDataValue.reason = componentsFromSerial[6];
                            BanDataValue.duration = banDuration;
                            BanDataValue.setTime = DateTime.FromBinary(banTime);

                            if (file == QBansBansFile)
                                QBanData.Add(componentsFromSerial[0].StringToCSteamID(), BanDataValue);
                            else
                                QBan.Instance.Configuration.Instance.ExpiredBans.Add(BanDataValue);
                        }
                        catch
                        {
                            Logger.LogWarning(String.Format("Error in parsing ban record entry, line: {0} of {1}.", i, lines.Count()));
                        }
                    }
                    else
                    {
                        Logger.LogWarning(String.Format("Failed to load an entry out of the bans data file, wrong number of values, number of values returned {0} of 9.", componentsFromSerial.Length));
                    }
                }
            }
            SaveToFile();
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
            QBan.Instance.Configuration.Save(QBan.Instance.Configuration.Instance);
        }
    }
}
