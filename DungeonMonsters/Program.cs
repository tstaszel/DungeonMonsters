using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Json;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using TextCopy;
using static System.ConsoleColor;

namespace DungeonMonsters
{
    public class Program
    {
        public enum AlignmentType
        {
            Lawful,
            Neutral,
            Chaotic,
            Unaligned
        }

        public enum AlignmentExtent
        {
            Good,
            Neutral,
            Evil,
            Unaligned
        }

        static void Main()
        {
            DataReader.Load();
            string input = "";
            Console.WriteLine("Options: input, list, search, exit. ");
            Console.WriteLine("Input will need to have some data saved inorder for it to work.");
            do
            {
                
                input = Console.ReadLine();
                var args = input.ToLower().Split(" ");
                if (!args.Any()) continue;
                switch (args[0])
                {
                    case "input":
                        var entity = CreateEntity(ClipboardService.GetText());
                        DataReader.EntityData.Add($"{entity.Name}: {entity.Type}", entity);
                        PrintEntity(entity);
                        break;
                    case "list":
                        DataReader.EntityData.Keys.ToList().ForEach(Console.WriteLine);
                        break;
                    case "search":
                        Console.WriteLine("Search by monster type or its name. ex search fiend");
                        var enemies = DataReader.EntityData.Keys.ToArray().Where(s =>
                        {
                            if (args.Length > 2 && (args[1].Contains("type") || (args[1].Contains("name"))))
                                return s.ToLower().Split(": ")[args[1].Contains("type") ? 1 : 0].Contains(args[2]);
                            return args.Length != 2 || s.ToLower().Contains(args[1]);
                        });
                        switch (enemies.Count())
                        {
                            case 0:
                                Console.WriteLine(
                                    $"There is no match '{string.Join(" ", args[(args[1].Contains("type") || args[1].Contains("name") ? 2 : 1)..])}'");
                                break;
                            case 1:
                                PrintEntity(DataReader.EntityData[enemies.First()]);
                                break;
                            default:
                                enemies.ToList().ForEach(Console.WriteLine);
                                break;
                        }
                        break;
                }
            } while (input != "exit");

            DataReader.Save();
        }

        public static void ColorWrite(string text)
        {
            //Regex will see [#blue] 
            //this will be case sensitive 
            Console.ForegroundColor = White;
            List<(string, ConsoleColor)> stringCache = new()
                {(text.Replace("\r", "").Replace("[#w]", "[#white]"), White)};
            while (stringCache.Any(sc => Regex.IsMatch(sc.Item1, @"\[#(.+?)\]")))
            {
                var outliers = stringCache.Where(sc => Regex.IsMatch(sc.Item1, @"\[#(.+?)\]")).ToArray();
                for (int i = 0; i < outliers.Length; i++)
                {
                    var (split, color) = outliers[i];
                    var startIndex = stringCache.IndexOf((split, color));
                    stringCache.Remove((split, color));
                    var splitColorRaw = Regex.Match(split, @"\[#(.+?)\]").Groups[1].Value;
                    var splitColor = (ConsoleColor) Enum.Parse(typeof(ConsoleColor), splitColorRaw, true);
                    var continueSplit = split.Split($"[#{splitColorRaw}]");
                    stringCache.Insert(startIndex, (continueSplit[0], color));
                    stringCache.InsertRange(startIndex + 1, continueSplit[1..].Select(s => (s, splitColor)));
                }
            }

            foreach (var (subtext, color) in stringCache)
            {
                Console.ForegroundColor = color;
                Console.Write(subtext);
            }
        }


        public record Entity(
            string Name,
            string Type,
            (AlignmentType, AlignmentExtent) Alignment,
            int ArmorClass,
            (int, string) HitPoints,
            int Speed,
            (int, int)[] Stats,
            List<(string, int)> SavingThrows,
            List<(string, int)> Skills,
            List<(string, int)> Senses,
            string ConditionImmunities,
            string Languages,
            (string, int) Challenge,
            string Abilities,
            string Actions,
            string Vulnerabilities = "N/A",
            string Immunities = "N/A",
            int Swim = 0,
            int Burrow = 0,
            int Climb = 0,
            int Fly = 0);

        public static Entity CreateEntity(string text)
        {
            var split = text.Replace("\r", "").Replace("−", "-").Replace("\n\n", "\n").Split("\n");

            var name = split[0];

            var typeAlign = split[1].Split(", ");
            var type = typeAlign[0];
            var align = (AlignmentType.Unaligned, AlignmentExtent.Unaligned);
            if (!typeAlign[1].Contains("unaligned"))
            {
                var alignsplit = typeAlign[1].Split(" ");
                align = ((AlignmentType) Enum.Parse(typeof(AlignmentType), alignsplit[0], true),
                    (AlignmentExtent) Enum.Parse(typeof(AlignmentExtent), alignsplit[1], true));
            }

            var armor = int.Parse(Regex.Match(split[2], @"^Armor Class (\d+)").Groups[1].Value);

            var hpRaw = Regex.Match(split[3], @"^Hit Points (\d+) \((.+?)\)").Groups;

            var hp = (int.Parse(hpRaw[1].Value), hpRaw[2].Value);

            //ALL Of the Speeds pull
            var speed = int.Parse(Regex.Match(split[4], @"Speed (\d+)").Groups[1].Value);
            var climb = 0;
            var fly = 0;
            var swim = 0;
            var burrow = 0;
            if (split[4].Contains("climb")) climb = int.Parse(Regex.Match(split[4], @"climb (\d+)").Groups[1].Value);
            if (split[4].Contains("fly")) fly = int.Parse(Regex.Match(split[4], @"fly (\d+)").Groups[1].Value);
            if (split[4].Contains("swim")) swim = int.Parse(Regex.Match(split[4], @"swim (\d+)").Groups[1].Value);
            if (split[4].Contains("burrow")) burrow = int.Parse(Regex.Match(split[4], @"burrow (\d+)").Groups[1].Value);

            var stats = new (int, int)[6];

            for (var i = 0; i < stats.Length; i++)
            {
                var tempRaw = Regex.Match(split[i * 2 + 6], @"(\d+)\(([+-]?\d+)\)").Groups;
                stats[i] = (int.Parse(tempRaw[1].Value), int.Parse(tempRaw[2].Value));
                //This will not save what the numbers mean, but the list always follows the same order
            }

            var addedIndex = 17; //this is the 17th item in the array
            List<(string, int)> skills = new();
            List<(string, int)> senses = new();
            List<(string, int)> savingthrows = new();


            void rawData(List<(string, int)> listName, string objects)
            {
                if (split[addedIndex].Contains(objects))
                {
                    foreach (var s in split[addedIndex].Replace(objects, "").Split(", "))
                    {
                        var rawData = Regex.Match(s, @"(.+) ([+-]?\d+)").Groups;
                        listName.Add((rawData[1].Value, int.Parse(rawData[2].Value)));
                    }

                    addedIndex++;
                }
            }

            rawData(savingthrows, "Saving Throws ");
            rawData(skills, "Skills ");

            //We search for the word, and then remove it if there are Vulnerabilities, if not use N/A
            var damageVul = split[addedIndex].Contains("Damage Vulnerabilities ")
                ? split[addedIndex++].Replace("Damage Vulnerabilities ", "")
                : "N/A";

            //Damage Immunities
            var damageImmune = split[addedIndex].Contains("Damage Immunities ")
                ? split[addedIndex++].Replace("Damage Immunities ", "")
                : "N/A";

            //Condition Immunities 
            var cndtImmune = split[addedIndex].Contains("Condition Immunities ")
                ? split[addedIndex++].Replace("Condition Immunities ", "")
                : "N/A";

            //Senses Pull
            rawData(senses, "Senses ");


            // This is just checking to see if the addedIndex is pulling languages yet
            // if not we have an issue
            List<string> sensesQueue = new();
            while (!split[addedIndex].Contains("Languages")) sensesQueue.Add(split[addedIndex++]);


            //Languages pull
            var languages = split[addedIndex].Contains("Languages ")
                ? split[addedIndex++].Replace("Languages ", "")
                : "N/A";
            if (languages == "—") languages = "N/A";

            // Pulling any data from senseQueue into the senses List
            if (sensesQueue.Any())
            {
                senses.AddRange(string.Join("", sensesQueue).Replace("\n", "").Split(", ")
                    .Select(s => Regex.Match(s, @"(.+) ([+-]?\d+)").Groups)
                    .Select(groups => (groups[1].Value, int.Parse(groups[2].Value))));
            }

            //Challenge pull
            var challenge = ("", 0);
            if (split[addedIndex].Contains("Challenge "))
            {
                var chalRaw = Regex.Match(split[addedIndex]
                    .Replace(",", ""), @"Challenge (.+) \((\d+) XP\)").Groups;
                challenge = (chalRaw[1].Value, int.Parse(chalRaw[2].Value));
                addedIndex++;
            }

            //Actions pull
            var actionsIndex = split.ToList().IndexOf("Actions");
            var abilities = new[] {"N/A"};
            try
            {
                abilities = split[addedIndex..(actionsIndex - 1)];
            }
            catch
            {
                // ignore the catch
            }

            var actions = split[(actionsIndex + 1)..];


            return new Entity(name, type, align, armor, hp, speed, stats, savingthrows, skills, senses, cndtImmune,
                languages,
                challenge, String.Join(" ", abilities),
                String.Join(" ", actions), damageVul, damageImmune, swim, burrow, climb, fly);
        } //End of Public Entity

        public static void PrintEntity(Entity entity)
        {
            var alignColors = new[] {Green, White, Red};

            var posNegColors = new[] {Red, Green};
            StringBuilder sb = new StringBuilder($@"[#darkmagenta]Entity Name[#w]:
[#darkred]{entity.Name}[#w]
[#darkmagenta]Entity Type[#w]: 
[#yellow]{entity.Type}[#w]
");

            //This is to filter out items in a list
            void ExpandList(IEnumerable<(string, int)> list, string name)
            {
                sb.Append($"[#darkmagenta]{name}[#w]: \n");
                var valueTuples = list as (string, int)[] ?? list.ToArray();

                if (!valueTuples.Any()) sb.Append($"[#yellow]N/A \n");

                else
                    foreach (var objects in valueTuples)
                        sb.Append(
                            $"{objects.Item1} ([#{posNegColors[objects.Item2 >= 0 ? 1 : 0]}]{objects.Item2}[#w]) \n");
            }

            sb.Append($"[#darkmagenta]Entity Alignment[#w]: \n");
            if (entity.Alignment.Item1 != AlignmentType.Unaligned)
                sb.Append($"[#{alignColors[(int) entity.Alignment.Item1]}]{entity.Alignment.Item1}" +
                          $" [#{alignColors[(int) entity.Alignment.Item2]}]{entity.Alignment.Item2}[#w] \n");
            else sb.Append($"[#gray]{entity.Alignment.Item1} \n");

            //Coloring Armor Class
            sb.Append($"[#darkmagenta]Armor Class[#w]: \n" +
                      $"{entity.ArmorClass} \n");

            //Coloring Hit Points
            sb.Append($"[#darkmagenta]Hit Points[#w]: \n" +
                      $"[#darkgreen]{entity.HitPoints.Item1}[#w] " +
                      $"([#darkgreen]{entity.HitPoints.Item2}[#w]) \n");

            //Coloring Speed and Movement
            sb.Append($"[#darkmagenta]Movement Speeds[#w]: \n{entity.Speed}ft");
            if (entity.Climb != 0) sb.Append($" | {entity.Climb}ft");
            if (entity.Fly != 0) sb.Append($" | {entity.Fly}ft");
            if (entity.Swim != 0) sb.Append($" | {entity.Swim}ft");
            if (entity.Burrow != 0) sb.Append($" | {entity.Burrow}ft");
            sb.Append("\n");


            //Coloring Stats
            sb.Append($"[#darkmagenta]Stats[#w]: \n");
            //Dictionary changes location from nth to string, changing key from int to something else
            //Lets us skip like 1, 4, 6, 9 
            Dictionary<string, ConsoleColor> statsColor = new()
                {{"Str", Red}, {"Dex", Green}, {"Con", Yellow}, {"Int", Blue}, {"Wis", Cyan}, {"Cha", Magenta}};
            for (int i = 0; i < statsColor.Count; i++)
            {
                var stat = statsColor.Keys.ToArray()[i];
                sb.Append($"[#{statsColor[stat]}]{stat}[#w]:" + $" {entity.Stats[i].Item1} " +
                          $"([#{posNegColors[entity.Stats[i].Item2 >= 0 ? 1 : 0]}]{entity.Stats[i].Item2}[#w]) \n");
            }


            //Expand Saving Throws List + Color
            sb.Append($"[#darkmagenta]Saving Throws[#w]: \n");
            if (!entity.SavingThrows.Any()) sb.Append("[#yellow]N/A \n");
            else
            {
                foreach (var objects in entity.SavingThrows)
                {
                    var stat = objects.Item1;
                    sb.Append($"[#{statsColor[stat]}]{objects.Item1}[#w]: " +
                              $"([#{posNegColors[objects.Item2 >= 0 ? 1 : 0]}]{objects.Item2}[#w]) \n");
                }
            }

            //Expand Skills List
            ExpandList(entity.Skills, "Skills");

            //Coloring Vulnerabilities
            sb.Append($"[#darkmagenta]Vulnerabilities[#w]: \n");
            sb.Append(entity.Vulnerabilities.Contains("N/A")
                ? $"[#yellow]{entity.Vulnerabilities}\n"
                : $"{entity.Vulnerabilities}\n");

            //Coloring Immunities
            sb.Append($"[#darkmagenta]Immunities[#w]: \n");
            sb.Append(entity.Immunities.Contains("N/A") ? $"[#yellow]{entity.Immunities}\n" : $"{entity.Immunities}\n");

            //Coloring Condition Immunities
            sb.Append($"[#darkmagenta]Condition Immunities[#w]:  \n");
            sb.Append(entity.ConditionImmunities.Contains("N/A")
                ? $"[#yellow]{entity.ConditionImmunities} \n"
                : $"{entity.ConditionImmunities} \n");

            //Coloring Senses
            ExpandList(entity.Senses, "Senses");

            //Coloring Languages
            sb.Append($"[#darkmagenta]Languages[#w]: \n");
            sb.Append(entity.Languages.Contains("N/A") ? $"[#yellow]{entity.Languages} \n" : $"{entity.Languages}\n");

            //Coloring Challenge
            sb.Append($"[#darkmagenta]Challenge[#w]: \n" +
                      $"[#darkred]{entity.Challenge.Item1}[#w]" +
                      $"([#darkred]{entity.Challenge.Item2}[#w]) \n");

            //Coloring Abilities
            sb.Append($"[#darkmagenta]Abilities[#w]: \n{entity.Abilities} \n");

            //Coloring Actions
            sb.Append($"[#darkmagenta]Actions[#w]: \n{entity.Actions.Replace(". ", ". \n")}");

            ColorWrite(sb.ToString());
        }
    }
}