using System;
using System.IO;
using System.Xml;
using System.Xml.Schema;
using System.Collections.Generic;

namespace RdfTool
{
    public class RdfVariationSet
    {
        public FoxHash VariationSetName { get; set; }

        public List<RdfVoiceClip> VoiceClips = new List<RdfVoiceClip>();
        public void Read(BinaryReader reader, HashManager hashManager, HashIdentifiedDelegate hashIdentifiedCallback)
        {
            VariationSetName = new FoxHash(); //TODO assumption that it's strcode32, not a single one's been unhashed
            VariationSetName.Read(reader, hashManager.StrCode32LookupTable, hashIdentifiedCallback);
            Console.WriteLine($"    SetName: {VariationSetName.HashValue}");

            byte count = reader.ReadByte();

            for (int i = 0; i < count; i++)
            {
                RdfVoiceClip voiceClip = new RdfVoiceClip();
                voiceClip.Read(reader, hashManager, hashIdentifiedCallback);
                VoiceClips.Add(voiceClip);
            }
        }
        public void Write(BinaryWriter writer)
        {
            VariationSetName.Write(writer);
            writer.Write((byte)VoiceClips.Count);
            foreach (RdfVoiceClip voiceClip in VoiceClips)
            {
                voiceClip.Write(writer);
            }
        }
        public void ReadXml(XmlReader reader)
        {
            VariationSetName = new FoxHash();
            VariationSetName.ReadXml(reader, "variationSetName");
            reader.ReadStartElement("variationSet");
            while (2 > 1)
            {
                switch (reader.NodeType)
                {
                    case XmlNodeType.Element:
                        RdfVoiceClip voiceClip = new RdfVoiceClip();
                        voiceClip.ReadXml(reader);
                        VoiceClips.Add(voiceClip);
                        Console.WriteLine($"    {voiceClip.VoiceId.HashValue}");
                        continue;
                    case XmlNodeType.EndElement:
                        reader.Read();
                        return;
                }
            }
        }

        public void WriteXml(XmlWriter writer)
        {
            writer.WriteStartElement("variationSet");
            VariationSetName.WriteXml(writer, "variationSetName");

            Console.WriteLine($"    variationSetName: {VariationSetName.HashValue}");

            foreach (RdfVoiceClip voiceClip in VoiceClips)
            {
                voiceClip.WriteXml(writer);
            }
            writer.WriteEndElement();
        }

        public XmlSchema GetSchema() { return null; }
    }
}
