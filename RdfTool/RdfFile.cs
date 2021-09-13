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
            writer.Write((short)LabelsGZ.Count);
            var labelsEndOffsetValueOffset = writer.BaseStream.Position;
            writer.Write(0); //offset to labels end
            List<int> offsetsToLabels = new List<int>();
            foreach (RdfLabelGZ label in LabelsGZ)
            {
                int ind = LabelsGZ.IndexOf(label);
                label.LabelName.Write(writer);
                offsetsToLabels[ind] = (int)writer.BaseStream.Position;
                writer.Write(0); //offset to label start
            }
            foreach (RdfLabelGZ label in LabelsGZ)
                label.Write(writer);
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

            if ((Version)short.Parse(reader["version"]) != Version.TPP)
                throw new ArgumentOutOfRangeException();
            reader.ReadStartElement("rdf");

            bool doNodeLoop = true;/*
            if (reader.IsEmptyElement)
                doNodeLoop = false;
            reader.ReadStartElement("dialogueEvents");
            while (doNodeLoop)
                switch (reader.NodeType)
                {
                    case XmlNodeType.Element:
                        FnvHash dialogueEvent = new FnvHash();
                        dialogueEvent.ReadXml(reader, "dialogueEvent");
                        DialogueEvents.Add(dialogueEvent);
                        reader.ReadStartElement("dialogueEvent");
                        Console.WriteLine($"{dialogueEvent.HashValue}");
                        continue;
                    case XmlNodeType.EndElement:
                        doNodeLoop = false;
                        reader.Read();
                        break;
                }

            doNodeLoop = true;
            if (reader.IsEmptyElement)
                doNodeLoop = false;
            reader.ReadStartElement("voiceTypes");
            while (doNodeLoop)
                switch (reader.NodeType)
                {
                    case XmlNodeType.Element:
                        FnvHash voiceType = new FnvHash();
                        voiceType.ReadXml(reader, "voiceType");
                        VoiceTypes.Add(voiceType);
                        reader.ReadStartElement("voiceType");
                        Console.WriteLine($"{voiceType.HashValue}");
                        continue;
                    case XmlNodeType.EndElement:
                        doNodeLoop = false;
                        reader.Read();
                        break;
                }

            doNodeLoop = true;*/
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
                label.WriteXml(writer, DialogueEvents, VoiceTypes);
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
            /*
            writer.WriteStartElement("dialogueEvents");
            foreach (FnvHash dialogueEvent in DialogueEvents)
            {
                writer.WriteStartElement("dialogueEvent");
                dialogueEvent.WriteXml(writer, "dialogueEvent");
                writer.WriteEndElement();
            }
            writer.WriteEndElement();

            writer.WriteStartElement("voiceTypes");
            foreach (FnvHash voiceType in VoiceTypes)
            {
                writer.WriteStartElement("voiceType");
                voiceType.WriteXml(writer, "voiceType");
                writer.WriteEndElement();
            }
            writer.WriteEndElement();
            */
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
