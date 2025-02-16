using System.IO;
using System;
using System.Xml;
using System.Globalization;

namespace RdfTool
{
    public abstract class RadioGroupPart
    {
        public sbyte DialogueEventIndex { get; set; } // index into table
        public sbyte CharaIndex { get; set; } // index into table
        public byte IntervalNextLabelId { get; set; } // 4 bits
        public virtual void Read(BinaryReader reader, HashManager hashManager, HashIdentifiedDelegate hashIdentifiedCallback)
        {
            DialogueEventIndex = reader.ReadSByte(); //max 0xFF 255 max is invalid

            CharaIndex = reader.ReadSByte(); //max 0xFF 255 max is invalid

            // label group bool

            IntervalNextLabelId = (byte)(reader.ReadByte() >> 4);  //max 0x0F 15

            if (Program.Verbose)
            {
                Console.WriteLine($"         DialogueEventIndex: {DialogueEventIndex}");
                Console.WriteLine($"         voiceTypeIndex: {CharaIndex}");
                Console.WriteLine($"         IntervalNextLabelId: {IntervalNextLabelId}");
            }
        }
        public virtual void Write(BinaryWriter writer)
        {
            writer.Write(DialogueEventIndex);
            writer.Write(CharaIndex);
        }
        public virtual void WriteXml(XmlWriter writer)
        {
        }
    }
}
