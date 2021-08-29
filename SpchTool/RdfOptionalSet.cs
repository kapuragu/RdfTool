using System;
using System.IO;
using System.Xml;
using System.Xml.Schema;
using System.Collections.Generic;

namespace RdfTool
{
    public class RdfOptionalSet
    {
        public FoxHash OptionalSetName { get; set; }

        public List<FoxHash> LabelNames = new List<FoxHash>();
        public void Read(BinaryReader reader, HashManager hashManager, HashIdentifiedDelegate hashIdentifiedCallback)
        {
            OptionalSetName = new FoxHash();
            OptionalSetName.Read(reader, hashManager.StrCode32LookupTable, hashIdentifiedCallback);
            Console.WriteLine($"    SetName: {OptionalSetName.HashValue}");

            byte count = reader.ReadByte();

            for (int i = 0; i < count; i++)
            {
                FoxHash labelName = new FoxHash();
                labelName.Read(reader, hashManager.StrCode32LookupTable, hashIdentifiedCallback);
                LabelNames.Add(labelName);
            }
        }
        public void Write(BinaryWriter writer)
        {
            OptionalSetName.Write(writer);
            writer.Write((byte)LabelNames.Count);
            foreach (FoxHash labelName in LabelNames)
            {
                labelName.Write(writer);
            }
        }
        public void ReadXml(XmlReader reader)
        {
            OptionalSetName = new FoxHash();
            OptionalSetName.ReadXml(reader, "optionalSetName");
            reader.ReadStartElement("optionalSet");
            while (2 > 1)
            {
                switch (reader.NodeType)
                {
                    case XmlNodeType.Element:
                        FoxHash label = new FoxHash();
                        label.ReadXml(reader, "labelName");
                        LabelNames.Add(label);
                        reader.ReadStartElement("label");
                        Console.WriteLine($"    {label.HashValue}");
                        continue;
                    case XmlNodeType.EndElement:
                        reader.Read();
                        return;
                }
            }
        }

        public void WriteXml(XmlWriter writer)
        {
            writer.WriteStartElement("optionalSet");
            OptionalSetName.WriteXml(writer, "optionalSetName");

            Console.WriteLine($"    optionalSetName: {OptionalSetName.HashValue}");

            foreach (FoxHash labelName in LabelNames)
            {
                writer.WriteStartElement("label");
                labelName.WriteXml(writer, "labelName");
                writer.WriteEndElement();
            }
            writer.WriteEndElement();
        }

        public XmlSchema GetSchema() { return null; }
    }
}
