using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Reflection;

namespace RdfTool
{
    public enum Version : byte
    {
        TPP = 3,
        GZ = 1,
    };
    public enum RadioType : byte
    {
        real_time = 0,
        espionage = 1,
        optional = 2,
        game_over = 3,
        map = 4,
        mission_image = 5,
    };
    internal static class Program
    {
        private const string DefaultHashDumpFileName = "rdf_hash_dump_dictionary.txt";
        private const string fileType = "rdf";

        class RunSettings
        {
            public bool outputHashes = false;
            public string gameId = "TPP";
            public string outputPath = @"D:\Github\mgsv-lookup-strings";
        }//RunSettings
        public static bool Verbose;

        private static void Main(string[] args)
        {
            foreach (string arg in args)
                if (arg.ToLower() == "-verbose" || arg.ToLower() == "-v")
                {
                    Verbose = true;
                }

            var hashManager = new HashManager();

            // Multi-Dictionary Reading!!
            List<string> foxDictionaryNames = new List<string>
            {
                "rdf_label_dictionary.txt",
                "rdf_optionalset_dictionary.txt",
                "rdf_user_dictionary.txt",
            };
            List<string> fnvDictionaryNames = new List<string>
            {
                "rdf_dialogueevent_dictionary.txt",
                "rdf_voicetype_dictionary.txt",
                "rdf_voiceid_dictionary.txt",
                "rdf_user_dictionary.txt",
            };

            List<string> strCodeDictionaries = new List<string>();
            List<string> fnvDictionaries = new List<string>();

            foreach (var dictionaryPath in foxDictionaryNames)
                if (File.Exists(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + "/" + dictionaryPath))
                    strCodeDictionaries.Add(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + "/" + dictionaryPath);

            foreach (var dictionaryPath in fnvDictionaryNames)
                if (File.Exists(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + "/" + dictionaryPath))
                    fnvDictionaries.Add(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + "/" + dictionaryPath);

            hashManager.StrCode32LookupTable = MakeStrCode32HashLookupTableFromFiles(strCodeDictionaries);
            hashManager.Fnv1LookupTable = MakeFnv1HashLookupTableFromFiles(fnvDictionaries);

            List<string> UserStrings = new List<string>();

            //deal with args
            RunSettings runSettings = new RunSettings();

            List<string> files = new List<string>();
            int idx = 0;
            if (args[idx].ToLower() == "-outputhashes" || args[idx].ToLower() == "-o")
            {
                runSettings.outputHashes = true;
                runSettings.outputPath = args[idx += 1];
                runSettings.gameId = args[idx += 1].ToUpper();
                Console.WriteLine("Adding to file list");
                for (int i = idx += 1; i < args.Length; i++)
                {
                    AddToFiles(files, args[i], fileType);
                }
            }
            else
            {
                Console.WriteLine("Adding to file list");
                foreach (var arg in args)
                {
                    AddToFiles(files, arg, "*");
                }//foreach args
            }//args
            Console.WriteLine("Processing file list");
            foreach (var rdfPath in files)
            {
                if (File.Exists(rdfPath))
                {
                    Console.WriteLine(rdfPath);
                    // Read input file
                    string fileExtension = Path.GetExtension(rdfPath);
                    if (fileExtension.Equals(".xml", StringComparison.OrdinalIgnoreCase))
                    {
                        XmlReaderSettings xmlReaderSettings = new XmlReaderSettings
                        {
                            IgnoreWhitespace = true,
                            IgnoreComments=true,
                        };

                        using (var reader = XmlReader.Create(rdfPath, xmlReaderSettings))
                        {
                            reader.Read();
                            reader.Read();
                            Version version = (Version)Enum.Parse(typeof(Version), reader["version"]);
                            if (version == Version.GZ)
                            {
                                RadioData rdf = new RadioData();
                                rdf.ReadXml(reader);
                                WriteToBinary(rdf, Path.GetFileNameWithoutExtension(Path.GetFileNameWithoutExtension(rdfPath)) + ".rdf");
                                CollectUserStrings(rdf, hashManager, UserStrings);
                            }
                            else if (version == Version.TPP)
                            {
                                RadioData2 rdf = new RadioData2();
                                rdf.ReadXml(reader);
                                WriteToBinary(rdf, Path.GetFileNameWithoutExtension(Path.GetFileNameWithoutExtension(rdfPath)) + ".rdf");
                                CollectUserStrings(rdf, hashManager, UserStrings);
                            }
                        }
                    }
                    else if (fileExtension.Equals(".rdf", StringComparison.OrdinalIgnoreCase))
                    {
                        using (BinaryReader reader = new BinaryReader(new FileStream(rdfPath, FileMode.Open)))
                        {
                            Version version = (Version)reader.ReadByte();
                            if (version==Version.GZ)
                            {
                                RadioData rdf = new RadioData();
                                rdf.Read(reader, hashManager);
                                if (!runSettings.outputHashes)
                                {
                                    XmlWriterSettings xmlWriterSettings = new XmlWriterSettings()
                                    {
                                        Encoding = Encoding.UTF8,
                                        Indent = true
                                    };
                                    using (var writer = XmlWriter.Create(Path.GetFileNameWithoutExtension(rdfPath) + ".rdf.xml", xmlWriterSettings))
                                    {
                                        rdf.WriteXml(writer);
                                    }
                                }
                                else
                                {
                                    //Dump.DumpInfo.OutputHashes(runSettings.gameId, fileType, runSettings.outputPath, rdfPath, rdf);
                                }//if outputhashes
                            }
                            else if (version==Version.TPP)
                            {
                                RadioData2 rdf = new RadioData2();
                                rdf.Read(reader, hashManager);
                                if (!runSettings.outputHashes)
                                {
                                    XmlWriterSettings xmlWriterSettings = new XmlWriterSettings()
                                    {
                                        Encoding = Encoding.UTF8,
                                        Indent = true
                                    };
                                    using (var writer = XmlWriter.Create(Path.GetFileNameWithoutExtension(rdfPath) + ".rdf.xml", xmlWriterSettings))
                                    {
                                        rdf.WriteXml(writer);
                                    }
                                }
                                else
                                {
                                    Dump.DumpInfo.OutputHashes(runSettings.gameId, fileType, runSettings.outputPath, rdfPath, rdf);
                                }//if outputhashes
                            }
                        }
                    }
                    else
                    {
                        Console.WriteLine($"Unrecognized input type: {fileExtension}");
                    }
                }
                
            }

            // Write hash matches output
            WriteHashMatchesToFile(DefaultHashDumpFileName, hashManager);
            WriteUserStringsToFile(UserStrings);
        }//Main

        private static void AddToFiles(List<string> files, string path, string fileType)
        {
            if (File.Exists(path))
            {
                files.Add(path);
            }
            else
            {
                if (Directory.Exists(path))
                {
                    var dirFiles = Directory.GetFiles(path, $"*.{fileType}", SearchOption.AllDirectories);
                    foreach (var file in dirFiles)
                    {
                        files.Add(file);
                    }
                }
            }
        }//AddToFiles

        public static void WriteToBinary(RadioData rdf, string path)
        {
            using (BinaryWriter writer = new BinaryWriter(new FileStream(path, FileMode.Create)))
            {
                rdf.Write(writer);
            }
        }
        public static void WriteToBinary(RadioData2 rdf, string path)
        {
            using (BinaryWriter writer = new BinaryWriter(new FileStream(path, FileMode.Create)))
            {
                rdf.Write(writer);
            }
        }

        public static void WriteToXml(RadioData2 rdf, string path)
        {
            XmlWriterSettings xmlWriterSettings = new XmlWriterSettings()
            {
                Encoding = Encoding.UTF8,
                Indent = true
            };
            using (var writer = XmlWriter.Create(path, xmlWriterSettings))
            {
                rdf.WriteXml(writer);
            }
        }

        /// <summary>
        /// Opens a file containing one string per line from the input table of files, hashes each string, and adds each pair to a lookup table.
        /// </summary>
        private static Dictionary<uint, string> MakeStrCode32HashLookupTableFromFiles(List<string> paths)
        {
            ConcurrentDictionary<uint, string> table = new ConcurrentDictionary<uint, string>();

            // Read file
            List<string> stringLiterals = new List<string>();
            foreach (var dictionary in paths)
            {
                using (StreamReader file = new StreamReader(dictionary))
                {
                    // TODO multi-thread
                    string line;
                    while ((line = file.ReadLine()) != null)
                    {
                        stringLiterals.Add(line);
                    }
                }
            }

            // Hash entries
            Parallel.ForEach(stringLiterals, (string entry) =>
            {
                uint hash = HashManager.StrCode32(entry);
                table.TryAdd(hash, entry);
            });

            return new Dictionary<uint, string>(table);
        }
        private static Dictionary<uint, string> MakeFnv1HashLookupTableFromFiles(List<string> paths)
        {
            ConcurrentDictionary<uint, string> table = new ConcurrentDictionary<uint, string>();

            // Read file
            List<string> stringLiterals = new List<string>();
            foreach (var dictionary in paths)
            {
                using (StreamReader file = new StreamReader(dictionary))
                {
                    // TODO multi-thread
                    string line;
                    while ((line = file.ReadLine()) != null)
                    {
                        stringLiterals.Add(line);
                    }
                }
            }

            // Hash entries
            Parallel.ForEach(stringLiterals, (string entry) =>
            {
                uint hash = HashManager.FNV1Hash32Str(entry);
                table.TryAdd(hash, entry);
            });

            return new Dictionary<uint, string>(table);
        }

        /// <summary>
        /// Outputs all hash matched strings to a file.
        /// </summary>
        private static void WriteHashMatchesToFile(string path, HashManager hashManager)
        {
            using (StreamWriter file = new StreamWriter(path))
            {
                foreach (var entry in hashManager.UsedHashes)
                {
                    file.WriteLine(entry.Value);
                }
            }
        }
        public static void CollectUserStrings(RadioData rdf, HashManager hashManager, List<string> UserStrings)
        {
            foreach (var group in rdf.groups) // Analyze hashes
            {
                if (IsUserString(group.Name.StringLiteral, UserStrings, hashManager.StrCode32LookupTable))
                    UserStrings.Add(group.Name.StringLiteral);

                foreach (var part in group.LabelParts)
                {
                    if (IsUserString(part.Condition.StringLiteral, UserStrings, hashManager.StrCode32LookupTable))
                        UserStrings.Add(part.Condition.StringLiteral);
                }
            }
            foreach (var groupSet in rdf.groupSets) // Analyze hashes
            {
                if (IsUserString(groupSet.Name.StringLiteral, UserStrings, hashManager.StrCode32LookupTable))
                    UserStrings.Add(groupSet.Name.StringLiteral);
                foreach (var groupName in groupSet.GroupNames)
                {
                    if (IsUserString(groupName.StringLiteral, UserStrings, hashManager.Fnv1LookupTable))
                        UserStrings.Add(groupName.StringLiteral);
                }
            }
        }
        public static void CollectUserStrings(RadioData2 rdf, HashManager hashManager, List<string> UserStrings)
        {
            foreach (var dialogueEvent in rdf.dialogueEvents) // Analyze hashes
            {
                if (IsUserString(dialogueEvent.StringLiteral, UserStrings, hashManager.Fnv1LookupTable))
                    UserStrings.Add(dialogueEvent.StringLiteral);
            }
            foreach (var voiceType in rdf.charas) // Analyze hashes
            {
                if (IsUserString(voiceType.StringLiteral, UserStrings, hashManager.Fnv1LookupTable))
                    UserStrings.Add(voiceType.StringLiteral);
            }
            foreach (var label in rdf.groups) // Analyze hashes
            {
                if (IsUserString(label.Name.StringLiteral, UserStrings, hashManager.StrCode32LookupTable))
                    UserStrings.Add(label.Name.StringLiteral);
                foreach (var part in label.LabelParts)
                {
                    if (part is RadioLabelPart2 labelPart2)
                    {
                        if (IsUserString(labelPart2.Condition.StringLiteral, UserStrings, hashManager.StrCode32LookupTable))
                            UserStrings.Add(labelPart2.Condition.StringLiteral);
                    }
                    else if (part is RadioLabelGroup labelGroup)
                    {
                        if (IsUserString(labelGroup.Name.StringLiteral, UserStrings, hashManager.Fnv1LookupTable))
                            UserStrings.Add(labelGroup.Name.StringLiteral);
                    }
                }
            }
            foreach (var optionalSet in rdf.groupSets) // Analyze hashes
            {
                if (IsUserString(optionalSet.Name.StringLiteral, UserStrings, hashManager.StrCode32LookupTable))
                    UserStrings.Add(optionalSet.Name.StringLiteral);
                foreach (var optionalLabel in optionalSet.GroupNames)
                {
                    if (IsUserString(optionalLabel.StringLiteral, UserStrings, hashManager.Fnv1LookupTable))
                        UserStrings.Add(optionalLabel.StringLiteral);
                }
            }
            foreach (var variationSet in rdf.labelGroups) // Analyze hashes
            {
                if (IsUserString(variationSet.Name.StringLiteral, UserStrings, hashManager.StrCode32LookupTable))
                    UserStrings.Add(variationSet.Name.StringLiteral);
                foreach (var voiceClip in variationSet.LabelParts)
                {
                    if (IsUserString(voiceClip.Condition.StringLiteral, UserStrings, hashManager.Fnv1LookupTable))
                        UserStrings.Add(voiceClip.Condition.StringLiteral);
                }
            }
        }
        public static bool IsUserString(string userString, List<string> list, Dictionary<uint,string> dictionaryTable)
        {
            if (!dictionaryTable.ContainsValue(userString) && !list.Contains(userString))
                return true;
            else
                return false;
        }
        public static void WriteUserStringsToFile(List<string> UserStrings)
        {
            UserStrings.Sort(); //Sort alphabetically for neatness
            var UserDictionary = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + "/" + "rdf_user_dictionary.txt";
            foreach (var userString in UserStrings)
                using (StreamWriter file = new StreamWriter(UserDictionary, append: true))
                    file.WriteLine(userString); //Write them into the user dictionary
        }
    }
}
