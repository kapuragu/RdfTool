using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Xml;
using System.Xml.Schema;

namespace RdfTool
{
    public class RadioGroup2
    {
        public FoxHash Name { get; set; } = new FoxHash() { StringLiteral="" };
        public byte FLAGS { get; set; } = 0; // 5 bits
        public RadioType RadioType { get; set; } = 0; // 3 bits
        public bool IsSneak { get; set; } = true;
        public bool IsCaution { get; set; } = true;
        public bool IsEvasion { get; set; } = true;
        public bool IsClearing { get; set; } = true;
        public bool IsAlert { get; set; } = true;
        public byte InvalidTimeId { get; set; } = 0; // 3 bits
        //label part count 6 bits
        public byte Unknown2 { get; set; } = 0; // 2 bits
        public byte PriorityId { get; set; } = 0; // 4 bits
        public byte PlayType { get; set; } = 4; // 4 bits
        public byte StartId { get; set; } = 0; // 8 bits
        public byte EndId { get; set; } = 0; // 8 bits

        public List<RadioGroupPart> LabelParts = new List<RadioGroupPart>();
        public void Read(BinaryReader reader, HashManager hashManager, HashIdentifiedDelegate hashIdentifiedCallback)
        {
            Name = new FoxHash();
            Name.Read(reader, hashManager.StrCode32LookupTable, hashIdentifiedCallback);
            Console.WriteLine($"Label name: {Name.HashValue}");

            byte[] flags = reader.ReadBytes(4);
            Console.WriteLine($"    flags: {flags[0]} {flags[1]} {flags[2]} {flags[3]}");

            //byte 0
            FLAGS = (byte)(flags[0] & 0x1F ); // 5 bits
            RadioType = (RadioType)(flags[0] >> 5); // 3 bits
            Console.WriteLine($"    FLAGS: {FLAGS} RadioType {RadioType}");

            //byte 1
            IsSneak = (flags[1] & 1)==1; // 5 bits
            IsCaution = (flags[1] & 2) == 2;
            IsEvasion = (flags[1] & 4) == 4;
            IsClearing = (flags[1] & 8) == 8;
            IsAlert = (flags[1] & 0x10) == 0x10;
            InvalidTimeId = (byte)(flags[1] >> 5); // 3 bits
            Console.WriteLine($"    Phase: {IsSneak} {IsCaution} {IsEvasion} {IsClearing} {IsAlert}");
            Console.WriteLine($"    InvalidTimeId {InvalidTimeId}");

            //byte 2
            var voiceClipCount = flags[2] & 0x3F; // 6 bits
            Unknown2 = (byte)(flags[2] >> 6); // 2 bits
            Console.WriteLine($"    voiceClipCount {voiceClipCount}");
            Console.WriteLine($"    Unknown2 {Unknown2}");

            //byte 3
            PriorityId = (byte)(flags[3] & 0x0F); // 4 bits
            PlayType = (byte)(flags[3] >> 4); // 4 bits
            Console.WriteLine($"    PriorityId {PriorityId}");
            Console.WriteLine($"    PlayType {PlayType}");

            //byte 4 and 5
            StartId = reader.ReadByte();
            EndId = reader.ReadByte();
            Console.WriteLine($"    StartId {StartId}");
            Console.WriteLine($"    EndId {EndId}");

            for (int i = 0; i < voiceClipCount; i++)
            {
                bool isLabelGroup;
                uint startPos = (uint)reader.BaseStream.Position;
                reader.BaseStream.Position += 6;
                isLabelGroup = (reader.ReadByte()&1)==1;
                reader.BaseStream.Position = startPos;

                if(!isLabelGroup)
                {
                    RadioLabelPart2 labelPart = new RadioLabelPart2();
                    labelPart.Read(reader, hashManager, hashIdentifiedCallback);
                    LabelParts.Add(labelPart);
                }
                else
                {
                    RadioLabelGroup labelGroup = new RadioLabelGroup();
                    labelGroup.Read(reader, hashManager, hashIdentifiedCallback);
                    LabelParts.Add(labelGroup);
                }
            }
        }
        public void Write(BinaryWriter writer)
        {
            Name.Write(writer);
            byte[] flagbytes = new byte[4];

            flagbytes[0] = (byte)((byte)(FLAGS & 0x1F) | (byte)(((byte)RadioType & 0x07) << 5));

            byte phase = 0;
            phase = (byte)(IsSneak ? phase | 0x1 : phase);
            phase = (byte)(IsCaution ? phase | 0x2 : phase);
            phase = (byte)(IsEvasion ? phase | 0x4 : phase);
            phase = (byte)(IsClearing ? phase | 0x8 : phase);
            phase = (byte)(IsAlert ? phase | 0x10 : phase);
            flagbytes[1] = (byte)((byte)(phase & 0x1F) | (byte)((InvalidTimeId & 0x07) << 5));
            
            flagbytes[2] = (byte)((byte)(LabelParts.Count & 0x3F) | (byte)((Unknown2 | 0x04) << 6));
            
            flagbytes[3] = (byte)((byte)(PriorityId & 0x0F) | (byte)((PlayType & 0x0F) << 4));
            
            writer.Write(flagbytes);
            
            writer.Write(StartId);
            writer.Write(EndId);

            foreach (RadioGroupPart groupPart in LabelParts)
            {
                groupPart.Write(writer);
            }
        }
        public void ReadXml(XmlReader reader)
        {
            Name.ReadXml(reader, "id");
            FLAGS = byte.Parse(reader["FLAGS"]);

            RadioType = (RadioType)Enum.Parse(RadioType.GetType(), reader["radioType"]);
            Console.WriteLine($"id: {Name.HashValue}, FLAGS: {FLAGS}, RadioType: {RadioType}");

            reader.ReadStartElement("group2");

            IsSneak = bool.Parse(reader["sneak"]);
            IsCaution = bool.Parse(reader["caution"]);
            IsEvasion = bool.Parse(reader["evasion"]);
            IsClearing = bool.Parse(reader["clearing"]);
            IsAlert = bool.Parse(reader["alert"]);
            Console.WriteLine($"IsSneak: {IsSneak}, IsCaution: {IsCaution}, IsEvasion: {IsEvasion}, IsClearing: {IsClearing}, IsAlert: {IsAlert}");
            reader.ReadStartElement("phase");

            InvalidTimeId = byte.Parse(reader["invalidTimeId"]);
            Unknown2 = byte.Parse(reader["u2"]);
            PriorityId = byte.Parse(reader["priorityId"]);
            Console.WriteLine($"InvalidTimeId: {InvalidTimeId}, Unknown2: {Unknown2}, PriorityId: {PriorityId}");
            PlayType = byte.Parse(reader["playType"]);
            StartId = byte.Parse(reader["startId"]);
            EndId = byte.Parse(reader["endId"]);
            Console.WriteLine($"PlayType: {PlayType}, StartId: {StartId}, EndId: {EndId}");
            reader.ReadStartElement("info");
        }
        public void WriteXml(XmlWriter writer)
        {
            writer.WriteStartElement("group2");

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
            writer.WriteAttributeString("invalidTimeId", InvalidTimeId.ToString(CultureInfo.InvariantCulture));
            writer.WriteAttributeString("u2", Unknown2.ToString(CultureInfo.InvariantCulture));
            writer.WriteAttributeString("priorityId", PriorityId.ToString(CultureInfo.InvariantCulture));
            writer.WriteAttributeString("playType", PlayType.ToString(CultureInfo.InvariantCulture));
            writer.WriteAttributeString("startId", StartId.ToString(CultureInfo.InvariantCulture));
            writer.WriteAttributeString("endId", EndId.ToString(CultureInfo.InvariantCulture));
            writer.WriteEndElement();

            Console.WriteLine($"Label name: {Name.StringLiteral}");
        }
        public XmlSchema GetSchema() { return null; }
    }
}
