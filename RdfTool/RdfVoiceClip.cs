using System;
using System.IO;
using System.Xml;
using System.Xml.Schema;
using System.Globalization;
using System.Collections.Generic;

namespace RdfTool
{
    public class RdfVoiceClip
    {
        public FnvHash VoiceId { get; set; } //TODO: could be a variationset name, which could be a strcode32
        public sbyte DialogueEventIndex { get; set; } //max 0xFF 255 max is invalid
        public sbyte VoiceTypeIndex { get; set; } //max 0xFF 255 max is invalid
        public byte IsVariationSet { get; set; } //max 0x0F 15 - only observed in vanilla to be a 0/1 bool
        public byte u21 { get; set; } //max 0x0F 15
        public void Read(BinaryReader reader, HashManager hashManager, HashIdentifiedDelegate hashIdentifiedCallback)
        {
            VoiceId = new FnvHash(); //TODO: could be a variationset name, which could be a strcode32
            VoiceId.Read(reader, hashManager.Fnv1LookupTable, hashIdentifiedCallback);
            Console.WriteLine($"    Sbp VoiceClip: {VoiceId.HashValue}");

            DialogueEventIndex = reader.ReadSByte(); //max 0xFF 255 max is invalid
            VoiceTypeIndex = reader.ReadSByte(); //max 0xFF 255 max is invalid
            byte byte3 = reader.ReadByte();
            IsVariationSet = ((byte)(byte3 & 0x0F));  //max 0x0F 15 - only observed in vanilla to be a bool
            u21 = (byte)(byte3 >> 4);  //max 0x0F 15
            Console.WriteLine($"         DialogueEventIndex: {DialogueEventIndex}");
            Console.WriteLine($"         voiceTypeIndex: {VoiceTypeIndex}");
            Console.WriteLine($"         isVariationSet: {IsVariationSet} u21: {u21}");
        }
        public void Write(BinaryWriter writer)
        {
            VoiceId.Write(writer);

            byte[] flagbytes = new byte[3];
            flagbytes[0] = (byte)DialogueEventIndex;
            flagbytes[1] = (byte)VoiceTypeIndex;
            flagbytes[2] = (byte)((IsVariationSet & 0x0F) | (u21 << 4));

            writer.Write(flagbytes);
        }
        public void ReadXml(XmlReader reader, List<FnvHash> dialogueEvents, List<FnvHash> voiceTypes)
        {
            VoiceId = new FnvHash();
            VoiceId.ReadXml(reader, "voiceId");

            FnvHash dialogueEvent = new FnvHash();
            dialogueEvent.ReadXml(reader, "dialogueEvent");
            FnvHash voiceType = new FnvHash();
            voiceType.ReadXml(reader, "voiceType");

            bool dialogueEventDoAdd = true;
            if (reader["dialogueEvent"] == string.Empty)
                DialogueEventIndex = -1;
            else
            { 
                foreach (FnvHash diaEveEntry in dialogueEvents)
                    if (diaEveEntry.HashValue==dialogueEvent.HashValue)
                    {
                        dialogueEventDoAdd = false;
                        DialogueEventIndex = (sbyte)dialogueEvents.IndexOf(diaEveEntry);
                    }

                if (dialogueEventDoAdd)
                {
                    dialogueEvents.Add(dialogueEvent);
                    DialogueEventIndex = (sbyte)dialogueEvents.IndexOf(dialogueEvent);
                }
                Console.WriteLine($"    dialogueEventDoAdd: {dialogueEventDoAdd}");
            }

            bool voiceTypeDoAdd = true;
            if (reader["voiceType"] == string.Empty)
                VoiceTypeIndex = -1;
            else
            {
                foreach (FnvHash voiTypEntry in voiceTypes)
                    if (voiTypEntry.HashValue == voiceType.HashValue)
                    {
                        voiceTypeDoAdd = false;
                        VoiceTypeIndex = (sbyte)voiceTypes.IndexOf(voiTypEntry);
                    }

                if (voiceTypeDoAdd)
                {
                    voiceTypes.Add(voiceType);
                    VoiceTypeIndex = (sbyte)voiceTypes.IndexOf(voiceType);
                }
                Console.WriteLine($"    voiceTypeDoAdd: {voiceTypeDoAdd}");
            }

            IsVariationSet = byte.Parse(reader["isVariationSet"]);
            u21 = byte.Parse(reader["u21"]);
            reader.ReadStartElement("voiceClip");

            Console.WriteLine($"    voiceId: {VoiceId.StringLiteral}");
            Console.WriteLine($"         dialogueEventIndex: {DialogueEventIndex}");
            Console.WriteLine($"         voiceTypeIndex: {VoiceTypeIndex}");
            Console.WriteLine($"         isVariationSet: {IsVariationSet} u21: {u21}");
        }

        public void WriteXml(XmlWriter writer, List<FnvHash> dialogueEvents, List<FnvHash> voiceTypes)
        {
            writer.WriteStartElement("voiceClip");
            VoiceId.WriteXml(writer, "voiceId");

            //writer.WriteAttributeString("dialogueEventIndex", DialogueEventIndex.ToString(CultureInfo.InvariantCulture));
            //writer.WriteAttributeString("voiceTypeIndex", VoiceTypeIndex.ToString(CultureInfo.InvariantCulture));

            if (DialogueEventIndex==-1)
                writer.WriteAttributeString("dialogueEvent", string.Empty.ToString(CultureInfo.InvariantCulture));
            else
                dialogueEvents[DialogueEventIndex].WriteXml(writer, "dialogueEvent");

            if (VoiceTypeIndex == -1)
                writer.WriteAttributeString("voiceType", string.Empty.ToString(CultureInfo.InvariantCulture));
            else
                voiceTypes[VoiceTypeIndex].WriteXml(writer, "voiceType");

            writer.WriteAttributeString("isVariationSet", IsVariationSet.ToString(CultureInfo.InvariantCulture));
            writer.WriteAttributeString("u21", u21.ToString(CultureInfo.InvariantCulture));
            writer.WriteEndElement();

            Console.WriteLine($"    Sbp VoiceClip: {VoiceId.StringLiteral}");
        }

        public XmlSchema GetSchema() {return null;}
    }
}
