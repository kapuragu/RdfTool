using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;

namespace RdfTool
{
    public class RadioData : IXmlSerializable
    {
        public List<RadioGroup> groups = new List<RadioGroup>();
        public List<RadioGroupSet> groupSets = new List<RadioGroupSet>();
        public void Read(BinaryReader reader, HashManager hashManager)
        {
            reader.BaseStream.Position += 1;
            short labelCount = reader.ReadInt16();
            int offsetToEndSection = reader.ReadInt32();

            List<int> offsets = new List<int>();

            for (int i = 0; i < labelCount; i++)
            {
                RadioGroup label = new RadioGroup();
                groups.Add(label);

                FoxHash labelName = new FoxHash();
                labelName.Read(reader, hashManager.StrCode32LookupTable, hashManager.OnHashIdentified);
                label.Name = labelName;

                offsets.Add(reader.ReadInt32());
            }

            for (int i = 0; i < labelCount; i++)
            {
                groups[i].Read(reader, hashManager, hashManager.OnHashIdentified);
            }

            var optionalSetCount = reader.ReadByte();
            reader.BaseStream.Position += 3;

            List<int> offsetsSets = new List<int>();

            for (int i = 0; i < optionalSetCount; i++)
            {
                RadioGroupSet label = new RadioGroupSet();
                groupSets.Add(label);

                FoxHash labelName = new FoxHash();
                labelName.Read(reader, hashManager.StrCode32LookupTable, hashManager.OnHashIdentified);
                label.Name = labelName;

                offsets.Add(reader.ReadInt32());
            }

            for (int i = 0; i < optionalSetCount; i++)
            {
                var entryCount = reader.ReadInt16();
                reader.BaseStream.Position += 2;
                for (int j = 0; j < entryCount; j++)
                {
                    var offsetToLabel = reader.ReadInt32();
                    if (offsets.Contains(offsetToLabel))
                    {
                        var labelIndex = offsets.IndexOf(offsetToLabel);
                        groupSets[i].GroupNames.Add(groups[labelIndex].Name);
                    }
                }
            }

        }
        public void Write(BinaryWriter writer)
        {
            writer.Write((byte)1);
            writer.Write((byte)0);
            writer.Write((short)groups.Count);
            long offsetToOptionalSection = (int)writer.BaseStream.Position;
            writer.Write(0); //offset to labels end
            List<long> positionsOfLabelOffsets = new List<long>();
            List<long> labelStartOffsts = new List<long>();
            foreach (RadioGroup label in groups)
            {
                positionsOfLabelOffsets.Add(writer.BaseStream.Position);
                label.Name.Write(writer);
                writer.Write(0); //offset to label start
            }
            foreach (RadioGroup label in groups)
            {
                labelStartOffsts.Add(writer.BaseStream.Position);
                label.Write(writer);
            }

            //Going back into the file for offsets: <<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<
            int startOfOptionalSection = (int)writer.BaseStream.Position;

            writer.BaseStream.Position = offsetToOptionalSection;
            writer.Write(startOfOptionalSection);

            foreach (RadioGroup label in groups)
            {
                writer.BaseStream.Position = positionsOfLabelOffsets[groups.IndexOf(label)] + 4;
                writer.Write((int)labelStartOffsts[groups.IndexOf(label)]);
            }

            writer.BaseStream.Position = startOfOptionalSection;
            //Returning to write the rest of the file: >>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>

            writer.Write((byte)groupSets.Count);
            writer.Write((byte)0);
            writer.Write((short)0);
            List<long> positionsOfSetOffsets = new List<long>();
            List<long> setStartOffsets = new List<long>();
            foreach (RadioGroupSet set in groupSets)
            {
                set.Name.Write(writer);
                positionsOfSetOffsets.Add(writer.BaseStream.Position);
                writer.Write(0); //offset to label start
            }
            foreach (RadioGroupSet set in groupSets)
            {
                setStartOffsets.Add(writer.BaseStream.Position - startOfOptionalSection);
                writer.Write((short)set.GroupNames.Count);
                writer.Write((short)0);
                foreach (FoxHash labelName in set.GroupNames)
                {
                    foreach (RadioGroup label in groups)
                    {
                        if (label.Name.HashValue == labelName.HashValue)
                        {
                            writer.Write((int)labelStartOffsts[groups.IndexOf(label)]);
                            break;
                        }
                    }
                }
            }

            if (writer.BaseStream.Position % 0x10 != 0)
            {
                var zeroesToWrite = 0x10 - writer.BaseStream.Position % 0x10;
                for (int i = 0; i < zeroesToWrite; i++)
                {
                    writer.Write((byte)0);
                }
            }

            // Go back to write the offsets: <<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<
            foreach (RadioGroupSet set in groupSets)
            {
                writer.BaseStream.Position = positionsOfSetOffsets[groupSets.IndexOf(set)];
                writer.Write((int)setStartOffsets[groupSets.IndexOf(set)]);
            }
            // Go back to write the offsets: >>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>
        }
        public void ReadXml(XmlReader reader)
        {
            while (reader.Read())
            {
                if (reader.NodeType == XmlNodeType.Element)
                {
                    if (reader.Name == "group")
                    {
                        RadioGroup radioGroup = new RadioGroup();
                        radioGroup.ReadXml(reader);
                        groups.Add(radioGroup);
                    }
                    else if (reader.Name == "groupSet")
                    {
                        RadioGroupSet groupSet = new RadioGroupSet();
                        groupSet.ReadXml(reader);
                        groupSets.Add(groupSet);
                    }
                }
            }

        }
        public void WriteXml(XmlWriter writer)
        {
            writer.WriteStartElement("rdf");
            writer.WriteAttributeString("version", Version.GZ.ToString());
            foreach (RadioGroup label in groups)
            {
                label.WriteXml(writer);
            }
            foreach (RadioGroupSet set in groupSets)
            {
                set.WriteXml(writer);
            }
        }
        public XmlSchema GetSchema() { return null; }
    }
}
