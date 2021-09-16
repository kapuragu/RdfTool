using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;

namespace RdfTool
{
    public enum Version 
    {
        TPP=3,
        GZ=1,
    };

    public class RdfFile : IXmlSerializable
    {
        public List<FnvHash> DialogueEvents = new List<FnvHash>();
        public List<FnvHash> VoiceTypes = new List<FnvHash>();
        public List<RdfLabel> Labels = new List<RdfLabel>();
        public List<RdfOptionalSet> OptionalSets = new List<RdfOptionalSet>();
        public List<RdfVariationSet> VariationSets = new List<RdfVariationSet>();

        public List<RdfLabelGZ> LabelsGZ = new List<RdfLabelGZ>();
        public List<RdfOptionalSetGZ> OptionalSetsGZ = new List<RdfOptionalSetGZ>();
        public void Read(BinaryReader reader, HashManager hashManager)
        {
            // Read header
            byte version = reader.ReadByte();

            if (version != (byte)Version.TPP && version != (byte)Version.GZ)
                Console.WriteLine("Incorrect version!! Not an .rdf file??");

            if (version == (byte)Version.TPP)
                ReadTPP(reader, hashManager);
            else if (version==(byte)Version.GZ)
                ReadGZ(reader, hashManager);
        }
        public void ReadTPP(BinaryReader reader, HashManager hashManager)
        {
            byte dialogueEventCount = reader.ReadByte();

            ushort labelCount = reader.ReadUInt16();
            ushort optionalSetCount = reader.ReadUInt16();
            ushort variationSetCount = reader.ReadUInt16();

            byte voiceTypesCount = reader.ReadByte();

            Console.WriteLine($"Dialogue events count: {dialogueEventCount}");
            Console.WriteLine($"Labels count: {labelCount}");
            Console.WriteLine($"Optional sets count: {optionalSetCount}");
            Console.WriteLine($"Variation sets count: {variationSetCount}");
            Console.WriteLine($"Voice types count: {voiceTypesCount}");

            for (int i = 0; i < dialogueEventCount; i++)
            {
                FnvHash dialogueEvent = new FnvHash();
                dialogueEvent.Read(reader, hashManager.Fnv1LookupTable, hashManager.OnHashIdentified);
                Console.WriteLine($"Dialogue event: {dialogueEvent.StringLiteral}");
                DialogueEvents.Add(dialogueEvent);
            }

            for (int i = 0; i < voiceTypesCount; i++)
            {
                FnvHash voiceType = new FnvHash();
                voiceType.Read(reader, hashManager.Fnv1LookupTable, hashManager.OnHashIdentified);
                Console.WriteLine($"Voice type: {voiceType.StringLiteral}");
                VoiceTypes.Add(voiceType);
            }

            for (int i = 0; i < labelCount; i++)
            {
                RdfLabel label = new RdfLabel();
                label.Read(reader, hashManager, hashManager.OnHashIdentified);
                Labels.Add(label);
            }

            for (int i = 0; i < optionalSetCount; i++)
            {
                RdfOptionalSet set = new RdfOptionalSet();
                set.Read(reader, hashManager, hashManager.OnHashIdentified);
                OptionalSets.Add(set);
            }

            for (int i = 0; i < variationSetCount; i++)
            {
                RdfVariationSet set = new RdfVariationSet();
                set.Read(reader, hashManager, hashManager.OnHashIdentified);
                VariationSets.Add(set);
            }
        }
        public void ReadGZ(BinaryReader reader, HashManager hashManager)
        {
            reader.BaseStream.Position += 1;
            short labelCount = reader.ReadInt16();
            int offsetToEndSection = reader.ReadInt32();

            List<int> offsets = new List<int>();

            for (int i = 0; i < labelCount; i++)
            {
                RdfLabelGZ label = new RdfLabelGZ();
                LabelsGZ.Add(label);

                FoxHash labelName = new FoxHash();
                labelName.Read(reader, hashManager.StrCode32LookupTable, hashManager.OnHashIdentified);
                label.LabelName = labelName;

                offsets.Add(reader.ReadInt32());
            }

            for (int i = 0; i < labelCount; i++)
            {
                LabelsGZ[i].Read(reader, hashManager, hashManager.OnHashIdentified);
            }

            var optionalSetCount = reader.ReadByte();
            reader.BaseStream.Position += 3;

            List<int> offsetsSets = new List<int>();

            for (int i = 0; i < optionalSetCount; i++)
            {
                RdfOptionalSetGZ label = new RdfOptionalSetGZ();
                OptionalSetsGZ.Add(label);

                FoxHash labelName = new FoxHash();
                labelName.Read(reader, hashManager.StrCode32LookupTable, hashManager.OnHashIdentified);
                label.OptionalSetName = labelName;

                offsets.Add(reader.ReadInt32());
            }

            for (int i = 0; i < optionalSetCount; i++)
            {
                var entryCount = reader.ReadInt16();
                reader.BaseStream.Position += 2;
                for (int j = 0; j < entryCount; j++)
                {
                    var offsetToLabel = reader.ReadInt32();
                    if (offsets.Contains(offsetToLabel))
                    {
                        var labelIndex = offsets.IndexOf(offsetToLabel);
                        OptionalSetsGZ[i].LabelNames.Add(LabelsGZ[labelIndex].LabelName);
                    }
                }
            }

        }
        public void Write(BinaryWriter writer)
        {
            if (LabelsGZ.Count == 0 && OptionalSetsGZ.Count == 0)
                WriteTPP(writer);
            else
                WriteGZ(writer);

        }
        public void WriteGZ(BinaryWriter writer)
        {
            writer.Write((byte)1);
            writer.Write((byte)0);
            writer.Write((short)LabelsGZ.Count);
            long offsetToOptionalSection = (int)writer.BaseStream.Position;
            writer.Write(0); //offset to labels end
            List<long> positionsOfLabelOffsets = new List<long>();
            List<long> labelStartOffsts = new List<long>();
            foreach (RdfLabelGZ label in LabelsGZ)
            {
                positionsOfLabelOffsets.Add(writer.BaseStream.Position);
                label.LabelName.Write(writer);
                writer.Write(0); //offset to label start
            }
            foreach (RdfLabelGZ label in LabelsGZ)
            {
                labelStartOffsts.Add(writer.BaseStream.Position);
                label.Write(writer);
            }

            //Going back into the file for offsets: <<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<
            int startOfOptionalSection = (int)writer.BaseStream.Position;

            writer.BaseStream.Position = offsetToOptionalSection;
            writer.Write(startOfOptionalSection);

            foreach (RdfLabelGZ label in LabelsGZ)
            {
                writer.BaseStream.Position = positionsOfLabelOffsets[LabelsGZ.IndexOf(label)]+4;
                writer.Write((int)labelStartOffsts[LabelsGZ.IndexOf(label)]);
            }

            writer.BaseStream.Position = startOfOptionalSection;
            //Returning to write the rest of the file: >>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>

            writer.Write((byte)OptionalSetsGZ.Count);
            writer.Write((byte)0);
            writer.Write((short)0);
            List<long> positionsOfSetOffsets = new List<long>();
            List<long> setStartOffsets = new List<long>();
            foreach (RdfOptionalSetGZ set in OptionalSetsGZ)
            {
                set.OptionalSetName.Write(writer);
                positionsOfSetOffsets.Add(writer.BaseStream.Position);
                writer.Write(0); //offset to label start
            }
            foreach (RdfOptionalSetGZ set in OptionalSetsGZ)
            {
                setStartOffsets.Add(writer.BaseStream.Position-startOfOptionalSection);
                writer.Write((short)set.LabelNames.Count);
                writer.Write((short)0);
                foreach (FoxHash labelName in set.LabelNames)
                {
                    foreach (RdfLabelGZ label in LabelsGZ)
                    {
                        if (label.LabelName.HashValue==labelName.HashValue)
                        {
                            writer.Write((int)labelStartOffsts[LabelsGZ.IndexOf(label)]);
                            break;
                        }
                    }
                }
            }

            if (writer.BaseStream.Position % 0x10 != 0)
            {
                var zeroesToWrite = 0x10 - writer.BaseStream.Position % 0x10;
                for (int i = 0; i < zeroesToWrite; i++)
                {
                    writer.Write((byte)0);
                }
            }

            // Go back to write the offsets: <<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<
            foreach (RdfOptionalSetGZ set in OptionalSetsGZ)
            {
                writer.BaseStream.Position = positionsOfSetOffsets[OptionalSetsGZ.IndexOf(set)];
                writer.Write((int)setStartOffsets[OptionalSetsGZ.IndexOf(set)]);
            }
            // Go back to write the offsets: >>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>
        }
        public void WriteTPP(BinaryWriter writer)
        {
            // Write header
            writer.Write((byte)3);
            writer.Write((byte)DialogueEvents.Count);
            writer.Write((short)Labels.Count);
            writer.Write((short)OptionalSets.Count);
            writer.Write((short)VariationSets.Count);
            writer.Write((byte)VoiceTypes.Count);
            foreach (FnvHash dialogueEvent in DialogueEvents)
                dialogueEvent.Write(writer);
            foreach (FnvHash voiceType in VoiceTypes)
                voiceType.Write(writer);
            foreach (RdfLabel label in Labels)
                label.Write(writer);
            foreach (RdfOptionalSet set in OptionalSets)
                set.Write(writer);
            foreach (RdfVariationSet set in VariationSets)
                set.Write(writer);
            //From FMS, thx Bob!
            if (writer.BaseStream.Position % 0x10 != 0)
                writer.Write(new byte[(int)(0x10 - writer.BaseStream.Position % 0x10)]);
        }

        public void ReadXml(XmlReader reader)
        {
            reader.Read();
            reader.Read();

            var version = (Version)short.Parse(reader["version"]);

            if (version != Version.TPP && version != Version.GZ)
                throw new ArgumentOutOfRangeException();
            reader.ReadStartElement("rdf");

            if (version == Version.TPP)
                ReadXmlTPP(reader);
            else if (version == Version.GZ)
                ReadXmlGZ(reader);
        }
        public void ReadXmlGZ(XmlReader reader)
        {
            bool doNodeLoop = true;
            if (reader.IsEmptyElement)
                doNodeLoop = false;
            reader.ReadStartElement("labels");
            while (doNodeLoop)
                switch (reader.NodeType)
                {
                    case XmlNodeType.Element:
                        RdfLabelGZ label = new RdfLabelGZ();
                        label.ReadXml(reader);
                        LabelsGZ.Add(label);
                        Console.WriteLine($"{label.LabelName.HashValue}");
                        continue;
                    case XmlNodeType.EndElement:
                        doNodeLoop = false;
                        reader.Read();
                        break;
                }

            doNodeLoop = true;
            if (reader.IsEmptyElement)
                doNodeLoop = false;
            reader.ReadStartElement("optionalSets");
            while (doNodeLoop)
                switch (reader.NodeType)
                {
                    case XmlNodeType.Element:
                        RdfOptionalSetGZ optSet = new RdfOptionalSetGZ();
                        optSet.ReadXml(reader);
                        OptionalSetsGZ.Add(optSet);
                        Console.WriteLine($"{optSet.OptionalSetName.HashValue}");
                        continue;
                    case XmlNodeType.EndElement:
                        doNodeLoop = false;
                        reader.Read();
                        break;
                }

        }
        public void ReadXmlTPP(XmlReader reader)
        {
            bool doNodeLoop = true;
            if (reader.IsEmptyElement)
                doNodeLoop = false;
            reader.ReadStartElement("labels");
            while (doNodeLoop)
                switch (reader.NodeType)
                {
                    case XmlNodeType.Element:
                        RdfLabel label = new RdfLabel();
                        label.ReadXml(reader, DialogueEvents, VoiceTypes);
                        Labels.Add(label);
                        Console.WriteLine($"{label.LabelName.HashValue}");
                        continue;
                    case XmlNodeType.EndElement:
                        doNodeLoop = false;
                        reader.Read();
                        break;
                }

            doNodeLoop = true;
            if (reader.IsEmptyElement)
                doNodeLoop = false;
            reader.ReadStartElement("optionalSets");
            while (doNodeLoop)
                switch (reader.NodeType)
                {
                    case XmlNodeType.Element:
                        RdfOptionalSet optSet = new RdfOptionalSet();
                        optSet.ReadXml(reader);
                        OptionalSets.Add(optSet);
                        Console.WriteLine($"{optSet.OptionalSetName.HashValue}");
                        continue;
                    case XmlNodeType.EndElement:
                        doNodeLoop = false;
                        reader.Read();
                        break;
                }

            doNodeLoop = true;
            if (reader.IsEmptyElement)
                doNodeLoop = false;
            reader.ReadStartElement("variationSets");
            while (doNodeLoop)
                switch (reader.NodeType)
                {
                    case XmlNodeType.Element:
                        RdfVariationSet varSet = new RdfVariationSet();
                        varSet.ReadXml(reader, DialogueEvents, VoiceTypes);
                        VariationSets.Add(varSet);
                        Console.WriteLine($"{varSet.VariationSetName.HashValue}");
                        continue;
                    case XmlNodeType.EndElement:
                        doNodeLoop = false;
                        reader.Read();
                        break;
                }
        }
        public void WriteXml(XmlWriter writer)
        {
            writer.WriteStartDocument();
            writer.WriteStartElement("rdf");
            if (LabelsGZ.Count == 0 && OptionalSetsGZ.Count == 0)
                WriteXmlTPP(writer);
            else
                WriteXmlGZ(writer);
        }
        public void WriteXmlGZ(XmlWriter writer)
        {
            writer.WriteAttributeString("version", 1.ToString());
            writer.WriteStartElement("labels");
            foreach (RdfLabelGZ label in LabelsGZ)
            {
                label.WriteXml(writer);
            }
            writer.WriteEndElement();
            writer.WriteStartElement("optionalSets");
            foreach (RdfOptionalSetGZ set in OptionalSetsGZ)
            {
                set.WriteXml(writer);
            }
            writer.WriteEndElement();
        }
        public void WriteXmlTPP(XmlWriter writer)
        {
            writer.WriteAttributeString("version", 3.ToString());
            writer.WriteStartElement("labels");
            foreach (RdfLabel label in Labels)
            {
                label.WriteXml(writer, DialogueEvents, VoiceTypes);
            }
            writer.WriteEndElement();

            writer.WriteStartElement("optionalSets");
            foreach (RdfOptionalSet set in OptionalSets)
            {
                set.WriteXml(writer);
            }
            writer.WriteEndElement();

            writer.WriteStartElement("variationSets");
            foreach (RdfVariationSet set in VariationSets)
            {
                set.WriteXml(writer, DialogueEvents, VoiceTypes);
            }
            writer.WriteEndElement();

            writer.WriteEndDocument();
        }
        public XmlSchema GetSchema() {return null;}
    }
}
