using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Xml;
using System.Xml.Schema;

namespace RdfTool
{
    public class RdfLabelGZ
    {
        public FoxHash LabelName { get; set; }
        public byte u0 { get; set; }
        public byte u1 { get; set; }
        public ushort unk2 { get; set; }
        public ushort unk3 { get; set; }
        public byte u4 { get; set; }
        public byte u5 { get; set; }
        public byte u6 { get; set; }
        //voiceclipcount
        public byte u8 { get; set; }
        public byte u9 { get; set; }

        public List<RdfVoiceClipGZ> VoiceClips = new List<RdfVoiceClipGZ>();
        public void Read(BinaryReader reader, HashManager hashManager, HashIdentifiedDelegate hashIdentifiedCallback)
        {
            u0 = reader.ReadByte();
            u1 = reader.ReadByte();
            unk2 = reader.ReadUInt16();
            reader.BaseStream.Position += 2;
            unk3 = reader.ReadUInt16();
            u4 = reader.ReadByte();
            u5 = reader.ReadByte();
            u6 = reader.ReadByte();
            var voiceClipCount = reader.ReadByte();
            u8 = reader.ReadByte();
            u9 = reader.ReadByte();
            reader.BaseStream.Position += 2;
            for (int i = 0; i < voiceClipCount; i++)
            {
                RdfVoiceClipGZ voiceClip = new RdfVoiceClipGZ();
                voiceClip.Read(reader, hashManager, hashIdentifiedCallback);
                VoiceClips.Add(voiceClip);
            }
        }
        public void Write(BinaryWriter writer)
        {
            writer.Write(u0);
            writer.Write(u1);
            writer.Write(unk2);
            writer.Write(unk3);
            writer.Write(u4);
            writer.Write(u5);
            writer.Write(u6);
            writer.Write((byte)VoiceClips.Count);
            writer.Write(u8);
            writer.Write(u9);
            writer.Write((short)0);
            foreach (RdfVoiceClipGZ voiceClip in VoiceClips)
                voiceClip.Write(writer);
        }
        public void WriteXml(XmlWriter writer, List<FnvHash> dialogueEvents, List<FnvHash> voiceTypes)
        {
            writer.WriteStartElement("label");
            LabelName.WriteXml(writer, "labelName");
            writer.WriteAttributeString("u0", u0.ToString(CultureInfo.InvariantCulture));
            writer.WriteAttributeString("u1", u1.ToString(CultureInfo.InvariantCulture));
            writer.WriteAttributeString("unk2", unk2.ToString(CultureInfo.InvariantCulture));
            writer.WriteAttributeString("unk3", unk3.ToString(CultureInfo.InvariantCulture));
            writer.WriteAttributeString("u4", u4.ToString(CultureInfo.InvariantCulture));
            writer.WriteAttributeString("u5", u5.ToString(CultureInfo.InvariantCulture));
            writer.WriteAttributeString("u6", u6.ToString(CultureInfo.InvariantCulture));
            writer.WriteAttributeString("u8", u8.ToString(CultureInfo.InvariantCulture));
            writer.WriteAttributeString("u9", u9.ToString(CultureInfo.InvariantCulture));
            foreach (RdfVoiceClipGZ voiceClip in VoiceClips)
            {
                voiceClip.WriteXml(writer, dialogueEvents, voiceTypes);
            }
            writer.WriteEndElement();
        }
        public XmlSchema GetSchema() { return null; }
    }
}
