using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Dump
{
    static class DumpInfo
    {
        //tex even though this is specific to the tool, isolating it a bit to its own file to make merging/adapting to other tools easier
        public static void OutputHashes(string gameId, string fileType, string outputPath, string filePath, RdfTool.RdfFile file)
        {
            //SYNC mgsv-lookup-strings/rdf/rdf_hash_types.json
            //tex you might be thinking, why dont you just read the json? it's a chicken and egg, well not really, you figure out the names and hash types from the tool parameters first then use that to create the json
            var hashSets = new Dictionary<string, HashSet<string>>();
            var hashTypeNames = new Dictionary<string, string>();
            AddHashType("DialogueEvent", "FNV1Hash32", ref hashSets, ref hashTypeNames);
            AddHashType("VoiceType", "FNV1Hash32", ref hashSets, ref hashTypeNames);
            AddHashType("LabelName", "StrCode32", ref hashSets, ref hashTypeNames);
            AddHashType("VoiceEvent", "FNV1Hash32", ref hashSets, ref hashTypeNames);
            AddHashType("VoiceId", "FNV1Hash32", ref hashSets, ref hashTypeNames);
            AddHashType("OptionalSetName", "StrCode32", ref hashSets, ref hashTypeNames);
            AddHashType("VariationSetName", "StrCode32", ref hashSets, ref hashTypeNames);//DEBUGNOW caplag Though there's still the problem of not knowing whether variation set names are FNV132 or StrCode32.

            foreach (var hash in file.DialogueEvents)
            {
                hashSets["DialogueEvent"].Add(hash.HashValue.ToString());
            }//foreach DialogueEvents

            foreach (var hash in file.VoiceTypes)
            {
                hashSets["VoiceType"].Add(hash.HashValue.ToString());
            }//foreach DialogueEvents

            foreach (var label in file.Labels)
            {
                hashSets["LabelName"].Add(label.LabelName.HashValue.ToString());
                foreach (var voiceClip in label.VoiceClips)
                {
                    if (voiceClip.IsVariationSet==1)
                    {
                        hashSets["VariationSetName"].Add(voiceClip.VoiceId.HashValue.ToString());
                    }
                    else
                    {
                    hashSets["VoiceId"].Add(voiceClip.VoiceId.HashValue.ToString());
                    }
                }
            }//foreach labels

            foreach (var optionalSet in file.OptionalSets)
            {
                hashSets["OptionalSetName"].Add(optionalSet.OptionalSetName.HashValue.ToString());
                //DEBUGNOW are these unique to optionalset or are they same as LabelName
                foreach (var hash in optionalSet.LabelNames)
                {
                    hashSets["LabelName"].Add(hash.HashValue.ToString());
                }//foreach LabelNames
            }//foreach OptionalSets

            foreach (var variationSet in file.VariationSets)
            {
                hashSets["VariationSetName"].Add(variationSet.VariationSetName.HashValue.ToString());
                foreach (var hash in variationSet.VoiceClips)
                {
                    hashSets["VoiceId"].Add(hash.VoiceId.HashValue.ToString());
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