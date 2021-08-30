using System;
using System.IO;
using System.Xml;
using System.Xml.Schema;
using System.Globalization;

namespace RdfTool
{
    public class RdfVoiceClip
    {
        public FnvHash VoiceId { get; set; } //TODO: could be a variationset name, which could be a strcode32
        public byte u00 { get; set; }
        public byte u01 { get; set; }
        public byte VoiceTypeIndex { get; set; }
        public byte u11 { get; set; }
        public byte IsVariationSet { get; set; }
        public byte u21 { get; set; }
        public void Read(BinaryReader reader, HashManager hashManager, HashIdentifiedDelegate hashIdentifiedCallback)
        {
            VoiceId = new FnvHash(); //TODO: could be a variationset name, which could be a strcode32
            VoiceId.Read(reader, hashManager.Fnv1LookupTable, hashIdentifiedCallback);
            Console.WriteLine($"    Sbp VoiceClip: {VoiceId.HashValue}");

            byte[] flagbytes = reader.ReadBytes(3);
            Console.WriteLine($"        flags: {flagbytes[0]} {flagbytes[1]} {flagbytes[2]}");

            u00 = (byte)(flagbytes[0] & 0x0F);
            u01 = (byte)(flagbytes[0] >> 4);
            VoiceTypeIndex = (byte)(flagbytes[1] & 0x0F);
            u11 = (byte)(flagbytes[1] >> 4);
            IsVariationSet = (byte)(flagbytes[2] & 0x0F);
            u21 = (byte)(flagbytes[2] >> 4);
            Console.WriteLine($"         u00: {u00} u01: {u01}");
            Console.WriteLine($"         voiceTypeIndex: {VoiceTypeIndex} u11: {u11}");
            Console.WriteLine($"         isVariationSet: {IsVariationSet} u21: {u21}");
        }
        public void Write(BinaryWriter writer)
        {
            VoiceId.Write(writer);

            byte[] flagbytes = new byte[3];
            flagbytes[0] = (byte)((u00 & 0x0F) | (u01 << 4));
            flagbytes[1] = (byte)((VoiceTypeIndex & 0x0F) | (u11 << 4));
            flagbytes[2] = (byte)((IsVariationSet & 0x0F) | (u21 << 4));

            writer.Write(flagbytes);
        }
        public void ReadXml(XmlReader reader)
        {
            VoiceId = new FnvHash();
            VoiceId.ReadXml(reader, "voiceId");
            u00 = byte.Parse(reader["u00"]);
            u01 = byte.Parse(reader["u01"]);
            VoiceTypeIndex = byte.Parse(reader["voiceTypeIndex"]);
            u11 = byte.Parse(reader["u11"]);
            IsVariationSet = byte.Parse(reader["isVariationSet"]);
            u21 = byte.Parse(reader["u21"]);
            reader.ReadStartElement("voiceClip");

            Console.WriteLine($"    voiceId: {VoiceId.StringLiteral}");
            Console.WriteLine($"         u00: {u00} u01: {u01}");
            Console.WriteLine($"         voiceTypeIndex: {VoiceTypeIndex} u11: {u11}");
            Console.WriteLine($"         isVariationSet: {IsVariationSet} u21: {u21}");
        }

        public void WriteXml(XmlWriter writer)
        {
            writer.WriteStartElement("voiceClip");

            VoiceId.WriteXml(writer, "voiceId");
            writer.WriteAttributeString("u00", u00.ToString(CultureInfo.InvariantCulture));
            writer.WriteAttributeString("u01", u01.ToString(CultureInfo.InvariantCulture));
            writer.WriteAttributeString("voiceTypeIndex", VoiceTypeIndex.ToString(CultureInfo.InvariantCulture));
            writer.WriteAttributeString("u11", u11.ToString(CultureInfo.InvariantCulture));
            writer.WriteAttributeString("isVariationSet", IsVariationSet.ToString(CultureInfo.InvariantCulture));
            writer.WriteAttributeString("u21", u21.ToString(CultureInfo.InvariantCulture));
            writer.WriteEndElement();

            Console.WriteLine($"    Sbp VoiceClip: {VoiceId.StringLiteral}");
        }

        public XmlSchema GetSchema() {return null;}
    }
}
