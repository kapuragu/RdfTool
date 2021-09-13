using System;
using System.IO;
using System.Xml;
using System.Xml.Schema;
using System.Globalization;
using System.Collections.Generic;

namespace RdfTool
{
    public class RdfVoiceClipGZ
    {
        public FnvHash DialogueEvent { get; set; }
        public FnvHash VoiceType { get; set; }
        public FnvHash VoiceId { get; set; }
        public byte u0 { get; set; }
        public byte u1 { get; set; }
        public void Read(BinaryReader reader, HashManager hashManager, HashIdentifiedDelegate hashIdentifiedCallback)
        {
            DialogueEvent = new FnvHash();
            DialogueEvent.Read(reader, hashManager.Fnv1LookupTable, hashIdentifiedCallback);
            VoiceType = new FnvHash();
            VoiceType.Read(reader, hashManager.Fnv1LookupTable, hashIdentifiedCallback);
            VoiceId = new FnvHash();
            VoiceId.Read(reader, hashManager.Fnv1LookupTable, hashIdentifiedCallback);
            u0 = reader.ReadByte();
            u1 = reader.ReadByte();
            reader.BaseStream.Position += 2;
        }
        public void Write(BinaryWriter writer)
        {
            DialogueEvent.Write(writer);
            VoiceType.Write(writer);
            VoiceId.Write(writer);
            writer.Write(u0);
            writer.Write(u1);
            writer.Write((short)0);
        }
        public void WriteXml(XmlWriter writer, List<FnvHash> dialogueEvents, List<FnvHash> voiceTypes)
        {
            writer.WriteStartElement("voiceClip");
            DialogueEvent.WriteXml(writer, "dialogueEvent");
            VoiceType.WriteXml(writer, "voiceType");
            VoiceId.WriteXml(writer, "voiceId");
            writer.WriteAttributeString("u0", u0.ToString(CultureInfo.InvariantCulture));
            writer.WriteAttributeString("u1", u1.ToString(CultureInfo.InvariantCulture));
            writer.WriteEndElement();
        }
        public XmlSchema GetSchema() {return null;}
    }
}
