using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using static DungeonMonsters.Program;

namespace DungeonMonsters
{
    public class DataReader
    {
        public static Dictionary<string, Entity> EntityData = new();

        //This will save off to AppData Roaming based on user 
        public static string coreDir =
            $"{Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData)}/tstaszel/DnDcode";

        public static bool CheckDir(string dir) => Directory.Exists(dir) || File.Exists(dir);

        public static void Save()
        {
            var textInput = JsonConvert.SerializeObject(EntityData);
            if (!CheckDir(coreDir)) Directory.CreateDirectory(coreDir);
            //I could have chosen any kid of name
            using var sw = File.CreateText($"{coreDir}/MonsterData.json");
            sw.Write(textInput);
            sw.Close();
        }

        public static void Load()
        {
            if (CheckDir(coreDir) && CheckDir($"{coreDir}/MonsterData.json"))
            {
                using var sr = new StreamReader($"{coreDir}/MonsterData.json");
                EntityData = JsonConvert.DeserializeObject<Dictionary<string, Entity>>(sr.ReadToEnd());
                sr.Close();
            }
        }
    }
}