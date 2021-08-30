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
        public void Read(BinaryReader reader, HashManager hashManager)
        {
            // Read header
            byte version = reader.ReadByte();

            if (version == (byte)Version.GZ) //if gz version
            {
                Console.WriteLine("Version 1 not yet supported!!");
                throw new ArgumentOutOfRangeException();
            }
            else if (version != (byte)Version.TPP)
            {
                Console.WriteLine("Incorrect version!! Not an .rdf file??");
            };
            if (version == (byte)Version.TPP)
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
        }
        public void Write(BinaryWriter writer)
        {
            // Write header
            writer.Write((byte)3);
            writer.Write((byte)DialogueEvents.Count);
            writer.Write((short)Labels.Count);
            writer.Write((short)OptionalSets.Count);
            writer.Write((short)VariationSets.Count);
            writer.Write((byte)VoiceTypes.Count);
            foreach (FnvHash dialogueEvent in DialogueEvents)
            {
                dialogueEvent.Write(writer);
            }
            foreach (FnvHash voiceType in VoiceTypes)
            {
                voiceType.Write(writer);
            }
            foreach (RdfLabel label in Labels)
            {
                label.Write(writer);
            }
            foreach (RdfOptionalSet set in OptionalSets)
            {
                set.Write(writer);
            }
            foreach (RdfVariationSet set in VariationSets)
            {
                set.Write(writer);
            }
            //From FMS, thx Bob!
            if (writer.BaseStream.Position % 0x10 != 0)
                writer.Write(new byte[(int)(0x10 - writer.BaseStream.Position % 0x10)]);
        }

        public void ReadXml(XmlReader reader)
        {
            reader.Read();
            reader.Read();

            if ((Version)short.Parse(reader["version"]) != Version.TPP)
            {
                throw new ArgumentOutOfRangeException();
            }
            reader.ReadStartElement("rdf");

            reader.ReadStartElement("dialogueEvents");
            var loop = 0;
            while (loop==0)
            {
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
                        loop = 1;
                        reader.Read();
                        break;
                }
            }

            reader.ReadStartElement("voiceTypes");
            loop = 0;
            while (loop == 0)
            {
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
                        loop = 1;
                        reader.Read();
                        break;
                }
            }

            reader.ReadStartElement("labels");
            loop = 0;
            while (loop == 0)
            {
                switch (reader.NodeType)
                {
                    case XmlNodeType.Element:
                        RdfLabel label = new RdfLabel();
                        label.ReadXml(reader);
                        Labels.Add(label);
                        Console.WriteLine($"{label.LabelName.HashValue}");
                        continue;
                    case XmlNodeType.EndElement:
                        loop = 1;
                        reader.Read();
                        break;
                }
            }

            reader.ReadStartElement("optionalSets");
            loop = 0;
            while (loop == 0)
            {
                switch (reader.NodeType)
                {
                    case XmlNodeType.Element:
                        RdfOptionalSet optSet = new RdfOptionalSet();
                        optSet.ReadXml(reader);
                        OptionalSets.Add(optSet);
                        Console.WriteLine($"{optSet.OptionalSetName.HashValue}");
                        continue;
                    case XmlNodeType.EndElement:
                        loop = 1;
                        reader.Read();
                        break;
                }
            }

            reader.ReadStartElement("variationSets");
            loop = 0;
            while (loop == 0)
            {
                switch (reader.NodeType)
                {
                    case XmlNodeType.Element:
                        RdfVariationSet varSet = new RdfVariationSet();
                        varSet.ReadXml(reader);
                        VariationSets.Add(varSet);
                        Console.WriteLine($"{varSet.VariationSetName.HashValue}");
                        continue;
                    case XmlNodeType.EndElement:
                        loop = 1;
                        reader.Read();
                        break;
                }
            }
        }
        public void WriteXml(XmlWriter writer)
        {
            writer.WriteStartDocument();
            writer.WriteStartElement("rdf");
            writer.WriteAttributeString("version", 3.ToString());

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

            writer.WriteStartElement("labels");
            foreach (RdfLabel label in Labels)
            {
                label.WriteXml(writer);
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
                set.WriteXml(writer);
            }
            writer.WriteEndElement();

            writer.WriteEndDocument();
        }
        public XmlSchema GetSchema() {return null;}
    }
}
