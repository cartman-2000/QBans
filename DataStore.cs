using Rocket.RocketAPI;
using Rocket.Logging;
using SDG;
using Steamworks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;


namespace QBan
{
    public class DataStore
    {
        private static string QBansBaseDir = String.Format("Servers/{0}/Rocket/plugins/QBans", Steam.InstanceName);
        private static string QBansBansFile = String.Format("Servers/{0}/Rocket/Plugins/QBans/BansData.txt", Steam.InstanceName);
        private static string QBansBansbackupFile = String.Format("Servers/{0}/Rocket/Plugins/QBans/BansData_bk.txt", Steam.InstanceName);
        private static string QBansBansFileHeader = "## Data file for the queued bans, format: target_sid/target_charname/target_steamname/admin_sid/admin_charname/admin_steamname/reason/duration/set_time";

        private static Dictionary<CSteamID, BanDataValues> QBanData = new Dictionary<CSteamID, BanDataValues>();

        public DataStore()
        {
            Initialize();
        }

        // Initialize/load the ban data here.
        private static void Initialize()
        {
            //create an empty file for the bans.
            if (!File.Exists(QBansBansFile))
            {
                SaveToFile();
            }

            string[] lines = System.IO.File.ReadAllLines(@QBansBansFile);
            foreach (string value in lines)
            {
                if (value != "" && !value.StartsWith("##"))
                {
                    string[] componentsFromSerial = Parser.getComponentsFromSerial(value, '/');
                    if (componentsFromSerial.Length == 9)
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

                        QBanData.Add(componentsFromSerial[0].StringToCSteamID(), BanDataValue);
                    }
                    else
                    {
                        Logger.LogWarning(String.Format("Failed to load an entry out of the bans data file, wrong number of values, number of values returned {0} of 9.", componentsFromSerial.Length));
                    }
                }
            }
        }

        // Set ban data and save out to file.
        public bool SetQBanData(CSteamID key, BanDataValues data)
        {
            try
            {
                if (QBanData.ContainsKey(key))
                {
                    QBanData.Remove(key);
                }

                QBanData.Add(key, data);
                SaveToFile();
                return true;
            }
            catch
            {
                RocketChatManager.print("Error, Unable to set ban data, wrong number of array elements.");
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
            try
            {
                return QBanData.Values.First(contents => contents.targetCharName.ToLower().Contains(playername.ToLower()) || contents.targetSteamName.ToLower().Contains(playername.ToLower()));
            }
            catch
            {
                return new BanDataValues();
            }
        }

        // Get exact match by CSteamID.
        public BanDataValues GetQBanData(Steamworks.CSteamID cSteamID)
        {
            BanDataValues result;
            if (QBanData.TryGetValue(cSteamID, out result))
            {
                return result;
            }
            else
            {
                return new BanDataValues();
            }
        }

        // Grab a list of bans for the bans command.
        public KeyValuePair<int, List<BanDataValues>> GetQBanDataList(string searchString, int count, int pagination)
        {
            // Grab a list of matches of the searchString out of the QBanData dictionary.
            var matches = QBanData.Values.Where(contents => contents.targetCharName.ToLower().Contains(searchString.ToLower()) || contents.targetSteamName.ToLower().Contains(searchString.ToLower())).ToList();
            int matchCount = matches.Count;
            int index;
            int numbeOfRecords;

            // Do the math for the pagenation so that no negative numbers are entered into matches.GetRange.
            if (matchCount - (count * pagination) <= 0)
            {
                index = 0;
                numbeOfRecords = matchCount - count * (pagination - 1);
                if (numbeOfRecords < 0)
                {
                    numbeOfRecords = 0;
                }
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
            List<CSteamID> expiredList = new List<CSteamID>();
            foreach (KeyValuePair<CSteamID, BanDataValues> pair in QBanData)
            {
                if ((int)(pair.Value.duration - (DateTime.Now - pair.Value.setTime).TotalSeconds) <= 0)
                {
                    expiredList.Add(pair.Key);
                    SteamBlacklist.unban(pair.Key);
                    SteamBlacklist.save();
                }
            }
            foreach (CSteamID key in expiredList)
            {
                QBanData.Remove(key);
            }
            if (expiredList.Count != 0)
            {
                SaveToFile();
            }
        }

        // Save to file.
        private static void SaveToFile()
        {
            //Create the folder where the data file is to be stored
            Directory.CreateDirectory(QBansBaseDir);
            //create a backup of the main data file before writing to it.
            if (File.Exists(QBansBansbackupFile))
            {
                File.Delete(QBansBansbackupFile);
            }
            if (File.Exists(QBansBansFile))
            {
                File.Copy(QBansBansFile, QBansBansbackupFile);
            }
            // Iterate through the dictionary and parse the entries out to file.
            StreamWriter file = new StreamWriter(QBansBansFile, false);
            file.WriteLine(QBansBansFileHeader);
            foreach (KeyValuePair<CSteamID, BanDataValues> pair in QBanData)
            {
                try
                {
                    file.WriteLine(pair.Value.targetSID.ToString() + "/" + pair.Value.targetCharName + "/" + pair.Value.targetSteamName + "/" + pair.Value.adminSID.ToString() + "/" + pair.Value.adminCharName + "/" + pair.Value.adminSteamName + "/" + pair.Value.reason + "/" + pair.Value.duration.ToString() + "/" + pair.Value.setTime.ToBinary().ToString());
                }
                catch (Exception ex)
                {
                    Logger.LogException(ex);
                }
            }
            file.Close();
        }
    }
}
