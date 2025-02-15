using RdfTool;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Dump
{
    static class DumpInfo
    {
        //tex even though this is specific to the tool, isolating it a bit to its own file to make merging/adapting to other tools easier
        public static void OutputHashes(string gameId, string fileType, string outputPath, string filePath, RdfTool.RadioData2 file)
        {
            //SYNC mgsv-lookup-strings/rdf/rdf_hash_types.json
            //tex you might be thinking, why dont you just read the json? it's a chicken and egg, well not really, you figure out the names and hash types from the tool parameters first then use that to create the json
            var hashSets = new Dictionary<string, HashSet<string>>();
            var hashTypeNames = new Dictionary<string, string>();
            AddHashType("DialogueEvent", "FNV1Hash32", ref hashSets, ref hashTypeNames);
            AddHashType("Chara", "FNV1Hash32", ref hashSets, ref hashTypeNames);
            AddHashType("GroupName", "StrCode32", ref hashSets, ref hashTypeNames);
            AddHashType("Condition", "FNV1Hash32", ref hashSets, ref hashTypeNames);
            AddHashType("GroupSetName", "StrCode32", ref hashSets, ref hashTypeNames);
            AddHashType("LabelGroupName", "StrCode32", ref hashSets, ref hashTypeNames);//DEBUGNOW caplag Though there's still the problem of not knowing whether variation set names are FNV132 or StrCode32.

            foreach (var hash in file.dialogueEvents)
            {
                hashSets["DialogueEvent"].Add(hash.HashValue.ToString());
            }//foreach DialogueEvents

            foreach (var hash in file.charas)
            {
                hashSets["Chara"].Add(hash.HashValue.ToString());
            }//foreach DialogueEvents

            foreach (var group2 in file.groups)
            {
                hashSets["GroupName"].Add(group2.Name.HashValue.ToString());
                foreach (var part in group2.LabelParts)
                {
                    if (part is RadioLabelGroup labelGroup)
                    {
                        hashSets["LabelGroupName"].Add(labelGroup.Name.HashValue.ToString());
                    }
                    else if(part is RadioLabelPart2 labelPart)
                    {
                        hashSets["Condition"].Add(labelPart.Condition.HashValue.ToString());
                    }
                }
            }//foreach labels

            foreach (var optionalSet in file.groupSets)
            {
                hashSets["GroupSetName"].Add(optionalSet.Name.HashValue.ToString());
                //DEBUGNOW are these unique to optionalset or are they same as LabelName
                foreach (var hash in optionalSet.GroupNames)
                {
                    hashSets["LabelName"].Add(hash.HashValue.ToString());
                }//foreach LabelNames
            }//foreach OptionalSets

            foreach (var labelGroup in file.labelGroups)
            {
                hashSets["LabelGroupName"].Add(labelGroup.Name.HashValue.ToString());
                foreach (var hash in labelGroup.LabelParts)
                {
                    hashSets["Condition"].Add(hash.Condition.HashValue.ToString());
                }//foreach LabelNames
            }//foreach OptionalSets

            foreach (KeyValuePair<string, HashSet<string>> kvp in hashSets)
            {
                string hashName = kvp.Key;
                WriteHashes(kvp.Value, filePath, fileType, hashName, hashTypeNames[hashName], gameId, outputPath);
            }
        }//OutputHashes
        private static void AddHashType(string hashName, string hashType, ref Dictionary<string, HashSet<string>> hashSets, ref Dictionary<string, string> hashTypeNames)
        {
            hashSets.Add(hashName, new HashSet<string>());
            hashTypeNames.Add(hashName, hashType);
        }//AddHashType
        private static string GetAssetsPath(string inputPath)
        {
            int index = inputPath.LastIndexOf("Assets");
            if (index != -1)
            {
                return inputPath.Substring(index);
            }
            return Path.GetFileName(inputPath);
        }//GetAssetsPath
         //tex outputs to mgsv-lookup-strings repo layout
        private static void WriteHashes(HashSet<string> hashSet, string inputFilePath, string fileType, string hashName, string hashTypeName, string gameId, string outputPath)
        {
            if (hashSet.Count > 0)
            {
                string assetsPath = GetAssetsPath(inputFilePath);
                //OFF string destPath = {inputFilePath}_{hashName}_{hashTypeName}.txt" //Alt: just output to input file path_whatev
                string destPath = Path.Combine(outputPath, $"{fileType}\\Hashes\\{gameId}\\{hashName}\\{assetsPath}_{hashName}_{hashTypeName}.txt");

                List<string> hashes = hashSet.ToList<string>();
                hashes.Sort();

                string destDir = Path.GetDirectoryName(destPath);
                DirectoryInfo di = Directory.CreateDirectory(destDir);
                File.WriteAllLines(destPath, hashes.ToArray());
            }
        }//WriteHashes
    }//DumpInfo
}//namespace Dump