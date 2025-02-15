using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Xml;
using System.Xml.Schema;

namespace RdfTool
{
    public class RadioGroup
    {
        public FoxHash Name { get; set; }
        public RadioType RadioType { get; set; } = 0; // 4 bits
        public byte PlayType { get; set; } = 4; // 4 bits
        //4 empty bits
        public ushort InvalidTime { get; set; } = 0;
        //16 empty bits
        public ushort Unknown16 { get; set; } = 2400;
        public byte FLAGS { get; set; } = 0; // 5 bits
        public byte Unknown3 { get; set; } = 0; // 4 bits
        public bool IsSneak { get; set; } = true;
        public bool IsCaution { get; set; } = true;
        public bool IsEvasion { get; set; } = true;
        public bool IsClearing { get; set; } = true;
        public bool IsAlert { get; set; } = true;
        //3 empty bits
        public byte Priority { get; set; } = 0;
        //labelPartCount
        public byte StartId { get; set; } = 0;
        public byte EndId { get; set; } = 0;

        public List<RadioLabelPart> LabelParts = new List<RadioLabelPart>();
        public void Read(BinaryReader reader, HashManager hashManager, HashIdentifiedDelegate hashIdentifiedCallback)
        {
            RadioType = (RadioType)reader.ReadByte();

            PlayType = reader.ReadByte();

            InvalidTime = reader.ReadUInt16();

            reader.BaseStream.Position += 2;

            Unknown16 = reader.ReadUInt16();

            var _FLAGS = reader.ReadByte();
            FLAGS = (byte)(_FLAGS & 0x1F);
            Unknown3 = (byte)(_FLAGS >> 5);

            var phase = reader.ReadByte();
            IsSneak = (phase & 1) == 1; // 5 bits
            IsCaution = (phase & 2) == 2;
            IsEvasion = (phase & 4) == 4;
            IsClearing = (phase & 8) == 8;
            IsAlert = (phase & 0x10) == 0x10;

            Priority = reader.ReadByte();
            var labelPartCount = reader.ReadByte();

            StartId = reader.ReadByte();
            EndId = reader.ReadByte();

            reader.BaseStream.Position += 2;
            for (int i = 0; i < labelPartCount; i++)
            {
                RadioLabelPart part = new RadioLabelPart();
                part.Read(reader, hashManager, hashIdentifiedCallback);
                LabelParts.Add(part);
            }
        }
        public void Write(BinaryWriter writer)
        {
            writer.Write((byte)RadioType);
            writer.Write(PlayType);
            writer.Write(InvalidTime);
            writer.Write((short)0);
            writer.Write(Unknown16);
            writer.Write((byte)((FLAGS&0x1f)|Unknown3<<5));
            byte phase = 0;
            phase = (byte)(IsSneak ? phase | 0x1 : phase);
            phase = (byte)(IsCaution ? phase | 0x2 : phase);
            phase = (byte)(IsEvasion ? phase | 0x4 : phase);
            phase = (byte)(IsClearing ? phase | 0x8 : phase);
            phase = (byte)(IsAlert ? phase | 0x10 : phase);
            writer.Write(phase);
            writer.Write(Priority);
            writer.Write((byte)LabelParts.Count);
            writer.Write(StartId);
            writer.Write(EndId);
            writer.Write((short)0);
            foreach (RadioLabelPart part in LabelParts)
                part.Write(writer);
        }
        public void ReadXml(XmlReader reader)
        {
            Name = new FoxHash();
            Name.ReadXml(reader, "id");
            FLAGS = byte.Parse(reader["FLAGS"]);
            RadioType = (RadioType)Enum.Parse(RadioType.GetType(), reader["radioType"]);
            reader.ReadStartElement("group");

            IsSneak = bool.Parse(reader["sneak"]);
            IsCaution = bool.Parse(reader["caution"]);
            IsEvasion = bool.Parse(reader["evasion"]);
            IsClearing = bool.Parse(reader["clearing"]);
            IsAlert = bool.Parse(reader["alert"]);
            reader.ReadStartElement("phase");

            InvalidTime = ushort.Parse(reader["invalidTime"]);
            Unknown16 = ushort.Parse(reader["u16"]);
            Unknown3 = byte.Parse(reader["u3"]);
            Priority = byte.Parse(reader["priority"]);
            PlayType = byte.Parse(reader["playType"]);
            StartId = byte.Parse(reader["startId"]);
            EndId = byte.Parse(reader["endId"]);
            reader.ReadStartElement("info");

            bool doNodeLoop = true;
            while (doNodeLoop)
            {
                if (reader.Name == "labelPart")
                {
                    RadioLabelPart labelPart = new RadioLabelPart();
                    labelPart.ReadXml(reader);
                    LabelParts.Add(labelPart);
                    Console.WriteLine($"    {labelPart.Condition.HashValue}");
                }
                else
                {
                    doNodeLoop = false;
                }
            }
        }
        public void WriteXml(XmlWriter writer)
        {
            writer.WriteStartElement("group");
            Name.WriteXml(writer, "id");
            writer.WriteAttributeString("FLAGS", FLAGS.ToString(CultureInfo.InvariantCulture));
            writer.WriteAttributeString("radioType", RadioType.ToString());

            writer.WriteStartElement("phase");
            writer.WriteAttributeString("sneak", IsSneak.ToString(CultureInfo.InvariantCulture));
            writer.WriteAttributeString("caution", IsCaution.ToString(CultureInfo.InvariantCulture));
            writer.WriteAttributeString("evasion", IsEvasion.ToString(CultureInfo.InvariantCulture));
            writer.WriteAttributeString("clearing", IsClearing.ToString(CultureInfo.InvariantCulture));
            writer.WriteAttributeString("alert", IsAlert.ToString(CultureInfo.InvariantCulture));
            writer.WriteEndElement();

            writer.WriteStartElement("info");
            writer.WriteAttributeString("invalidTime", InvalidTime.ToString(CultureInfo.InvariantCulture));
            writer.WriteAttributeString("u16", Unknown16.ToString(CultureInfo.InvariantCulture));
            writer.WriteAttributeString("u3", Unknown3.ToString(CultureInfo.InvariantCulture));
            writer.WriteAttributeString("priority", Priority.ToString(CultureInfo.InvariantCulture));
            writer.WriteAttributeString("playType", PlayType.ToString(CultureInfo.InvariantCulture));
            writer.WriteAttributeString("startId", StartId.ToString(CultureInfo.InvariantCulture));
            writer.WriteAttributeString("endId", EndId.ToString(CultureInfo.InvariantCulture));
            writer.WriteEndElement();

            foreach (RadioLabelPart voiceClip in LabelParts)
            {
                voiceClip.WriteXml(writer);
            }
            writer.WriteEndElement();
        }
        public XmlSchema GetSchema() { return null; }
    }
}
