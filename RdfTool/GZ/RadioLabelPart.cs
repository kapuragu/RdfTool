using System;
using System.IO;
using System.Xml;
using System.Xml.Schema;
using System.Globalization;
using System.Collections.Generic;

namespace RdfTool
{
    public class RadioLabelPart
    {
        public FnvHash DialogueEvent { get; set; }
        public FnvHash Chara { get; set; }
        public FnvHash Condition { get; set; }
        public byte Weight { get; set; }
        public byte IntervalNextLabelId { get; set; }
        public void Read(BinaryReader reader, HashManager hashManager, HashIdentifiedDelegate hashIdentifiedCallback)
        {
            DialogueEvent = new FnvHash();
            DialogueEvent.Read(reader, hashManager.Fnv1LookupTable, hashIdentifiedCallback);
            Chara = new FnvHash();
            Chara.Read(reader, hashManager.Fnv1LookupTable, hashIdentifiedCallback);
            Condition = new FnvHash();
            Condition.Read(reader, hashManager.Fnv1LookupTable, hashIdentifiedCallback);
            Weight = reader.ReadByte();
            IntervalNextLabelId = reader.ReadByte();
            reader.BaseStream.Position += 2;
        }
        public void Write(BinaryWriter writer)
        {
            DialogueEvent.Write(writer);
            Chara.Write(writer);
            Condition.Write(writer);
            writer.Write(Weight);
            writer.Write(IntervalNextLabelId);
            writer.Write((short)0);
        }
        public void ReadXml(XmlReader reader)
        {
            DialogueEvent = new FnvHash();
            DialogueEvent.ReadXml(reader, "dialogueEvent");
            Chara = new FnvHash();
            Chara.ReadXml(reader, "chara");
            Condition = new FnvHash();
            Condition.ReadXml(reader, "condition");

            Weight = byte.Parse(reader["weight"]);
            IntervalNextLabelId = byte.Parse(reader["intervalNextLabelId"]);

            reader.ReadStartElement("labelPart");
        }
        public void WriteXml(XmlWriter writer)
        {
            writer.WriteStartElement("labelPart");
            DialogueEvent.WriteXml(writer, "dialogueEvent");
            Chara.WriteXml(writer, "chara");
            Condition.WriteXml(writer, "condition");
            writer.WriteAttributeString("weight", Weight.ToString(CultureInfo.InvariantCulture));
            writer.WriteAttributeString("intervalNextLabelId", IntervalNextLabelId.ToString(CultureInfo.InvariantCulture));
            writer.WriteEndElement();
        }
        public XmlSchema GetSchema() {return null;}
    }
}
