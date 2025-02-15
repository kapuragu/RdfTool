using System;
using System.IO;
using System.Xml;
using System.Xml.Schema;
using System.Collections.Generic;
using System.Globalization;

namespace RdfTool
{
    public class RadioLabelGroup : RadioGroupPart
    {
        public FoxHash Name { get; set; }
        public List<RadioLabelPart2> LabelParts = new List<RadioLabelPart2>();
        public override void Read(BinaryReader reader, HashManager hashManager, HashIdentifiedDelegate hashIdentifiedCallback)
        {
            Name = new FoxHash(); //TODO assumption that it's strcode32, not a single one's been unhashed
            Name.Read(reader, hashManager.StrCode32LookupTable, hashIdentifiedCallback);
            Console.WriteLine($"    SetName: {Name.HashValue}");

            base.Read(reader, hashManager, hashIdentifiedCallback);
        }
        public void ReadGroup(BinaryReader reader, HashManager hashManager, HashIdentifiedDelegate hashIdentifiedCallback)
        {
            Name = new FoxHash(); //TODO assumption that it's strcode32, not a single one's been unhashed
            Name.Read(reader, hashManager.StrCode32LookupTable, hashIdentifiedCallback);
            Console.WriteLine($"    SetName: {Name.HashValue}");

            byte count = reader.ReadByte();

            for (int i = 0; i < count; i++)
            {
                RadioLabelPart2 voiceClip = new RadioLabelPart2();
                voiceClip.Read(reader, hashManager, hashIdentifiedCallback);
                LabelParts.Add(voiceClip);
            }
        }
        public override void Write(BinaryWriter writer)
        {
            Name.Write(writer);

            base.Write(writer);

            writer.Write((byte)(1|IntervalNextLabelId << 4));
        }
        public void WriteGroup(BinaryWriter writer)
        {
            Name.Write(writer);
            Console.WriteLine($"@{writer.BaseStream.Position} Name: {Name.HashValue}");
            writer.Write((byte)LabelParts.Count);
            Console.WriteLine($"@{writer.BaseStream.Position} LabelParts.Count: {LabelParts.Count}");
            foreach (RadioLabelPart2 voiceClip in LabelParts)
            {
                voiceClip.Write(writer);
            }
        }
        public void ReadXml(XmlReader reader)
        {
            Name = new FoxHash();
            Name.ReadXml(reader, "id");
            IntervalNextLabelId = byte.Parse(reader["intervalNextLabelId"]);
            Console.WriteLine($"    labelGroup id: {Name.HashValue}, IntervalNextLabelId: {IntervalNextLabelId}");
        }

        public override void WriteXml(XmlWriter writer)
        {
            writer.WriteStartElement("labelGroup");
            Name.WriteXml(writer, "id");
        }

        public XmlSchema GetSchema() { return null; }
    }
}
