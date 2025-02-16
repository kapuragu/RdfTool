using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Eventing.Reader;
using System.Globalization;
using System.IO;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Schema;
using System.Xml.Serialization;

namespace RdfTool
{
    public class RadioData2 : IXmlSerializable
    {
        public List<FnvHash> dialogueEvents = new List<FnvHash>();
        public List<FnvHash> charas = new List<FnvHash>();
        public List<RadioGroup2> groups = new List<RadioGroup2>();
        public List<RadioGroupSet2> groupSets = new List<RadioGroupSet2>();
        public List<RadioLabelGroup> labelGroups = new List<RadioLabelGroup>();
        public void Read(BinaryReader reader, HashManager hashManager)
        {
            byte dialogueEventCount = reader.ReadByte();

            ushort groupCount = reader.ReadUInt16();
            ushort groupSetCount = reader.ReadUInt16();
            ushort labelGroupCount = reader.ReadUInt16();

            byte charasCount = reader.ReadByte();

            Console.WriteLine($"Dialogue events count: {dialogueEventCount}");
            Console.WriteLine($"Labels count: {groupCount}");
            Console.WriteLine($"Optional sets count: {groupSetCount}");
            Console.WriteLine($"Variation sets count: {labelGroupCount}");
            Console.WriteLine($"Voice types count: {charasCount}");

            dialogueEvents.Capacity = dialogueEventCount;

            groups.Capacity = groupCount;
            groupSets.Capacity = groupSetCount;
            labelGroups.Capacity = labelGroupCount;

            charas.Capacity = charasCount;

            for (int i = 0; i < dialogueEventCount; i++)
            {
                FnvHash dialogueEvent = new FnvHash();
                dialogueEvent.Read(reader, hashManager.Fnv1LookupTable, hashManager.OnHashIdentified);
                Console.WriteLine($"Dialogue event: {dialogueEvent.HashValue}");
                dialogueEvents.Insert(i,dialogueEvent);
            }

            for (int i = 0; i < charasCount; i++)
            {
                FnvHash voiceType = new FnvHash();
                voiceType.Read(reader, hashManager.Fnv1LookupTable, hashManager.OnHashIdentified);
                Console.WriteLine($"Voice type: {voiceType.HashValue}");
                charas.Insert(i,voiceType);
            }

            for (int i = 0; i < groupCount; i++)
            {
                RadioGroup2 group = new RadioGroup2();
                group.Read(reader, hashManager, hashManager.OnHashIdentified);
                groups.Insert(i, group);
            }

            for (int i = 0; i < groupSetCount; i++)
            {
                RadioGroupSet2 groupSet = new RadioGroupSet2();
                groupSet.Read(reader, hashManager, hashManager.OnHashIdentified);
                groupSets.Insert(i, groupSet);
            }

            for (int i = 0; i < labelGroupCount; i++)
            {
                RadioLabelGroup labelGroup = new RadioLabelGroup();
                labelGroup.ReadGroup(reader, hashManager, hashManager.OnHashIdentified);
                labelGroups.Insert(i, labelGroup);
            }
        }
        public void Write(BinaryWriter writer)
        {
            // Write header
            writer.Write((byte)3);
            writer.Write((byte)dialogueEvents.Count);
            writer.Write((short)groups.Count);
            writer.Write((short)groupSets.Count);
            writer.Write((short)labelGroups.Count);
            writer.Write((byte)charas.Count);
            foreach (FnvHash dialogueEvent in dialogueEvents)
                dialogueEvent.Write(writer);
            foreach (FnvHash voiceType in charas)
                voiceType.Write(writer);
            foreach (RadioGroup2 label in groups)
                label.Write(writer);
            foreach (RadioGroupSet2 set in groupSets)
                set.Write(writer);
            foreach (RadioLabelGroup set in labelGroups)
                set.WriteGroup(writer);
            //From FMS, thx Bob!
            if (writer.BaseStream.Position % 0x10 != 0)
                writer.Write(new byte[(int)(0x10 - writer.BaseStream.Position % 0x10)]);
        }
        public void ReadXml(XmlReader reader)
        {
            while (reader.Read())
            {
                if (reader.NodeType == XmlNodeType.Element)
                {
                    if (reader.Name == "group2")
                    {
                        RadioGroup2 radioGroup = new RadioGroup2();
                        radioGroup.ReadXml(reader);

                        bool doNodeLoop = true;
                        while (doNodeLoop)
                        {
                            if (reader.Name == "labelPart")
                            {
                                Console.WriteLine("labelPart");
                                RadioLabelPart2 labelPart = new RadioLabelPart2();
                                labelPart.ReadXml(reader,dialogueEvents,charas);
                                radioGroup.LabelParts.Add(labelPart);
                            }
                            else if (reader.Name == "labelGroup")
                            {
                                Console.WriteLine("labelGroup");
                                var isEmpty = reader.IsEmptyElement;
                                RadioLabelGroup labelGroup = new RadioLabelGroup();
                                labelGroup.ReadXml(reader);

                                bool labelGroupDoAdd = true;
                                if (isEmpty)
                                {
                                    reader.ReadStartElement("labelGroup");
                                    labelGroupDoAdd = false;
                                }
                                else
                                {
                                    foreach (RadioLabelGroup _labelGroup in labelGroups)
                                        if (_labelGroup.Name.HashValue == labelGroup.Name.HashValue)
                                        {
                                            labelGroupDoAdd = false;
                                            labelGroup = _labelGroup;
                                        }

                                    reader.ReadStartElement("labelGroup");
                                    while (reader.Name == "labelPart")
                                    {
                                        Console.WriteLine("     labelPart");
                                        RadioLabelPart2 labelPart = new RadioLabelPart2();
                                        labelPart.ReadXml(reader, dialogueEvents, charas);

                                        if (labelGroupDoAdd)
                                            labelGroup.LabelParts.Add(labelPart);
                                    }

                                    if (labelGroupDoAdd)
                                    {
                                        labelGroups.Add(labelGroup);
                                    }
                                    reader.ReadEndElement();
                                }
                                Console.WriteLine($"    labelGroupDoAdd: {labelGroupDoAdd}");
                                radioGroup.LabelParts.Add(labelGroup);
                            }
                            else
                            {
                                doNodeLoop = false; 
                            }
                        }
                        groups.Add(radioGroup);
                    }
                    else if (reader.Name == "groupSet2")
                    {
                        RadioGroupSet2 groupSet = new RadioGroupSet2();
                        groupSet.ReadXml(reader);
                        groupSets.Add(groupSet);
                    }
                    else if (reader.Name == "labelGroup")
                    {
                        Console.WriteLine("labelGroup");
                        var isEmpty = reader.IsEmptyElement;
                        RadioLabelGroup labelGroup = new RadioLabelGroup();
                        labelGroup.Name = new FoxHash();
                        labelGroup.Name.ReadXml(reader, "id");

                        bool labelGroupDoAdd = true;
                        if (isEmpty)
                        {
                            reader.ReadStartElement("labelGroup");
                            labelGroupDoAdd = false;
                        }
                        else
                        {
                            foreach (RadioLabelGroup _labelGroup in labelGroups)
                                if (_labelGroup.Name.HashValue == labelGroup.Name.HashValue)
                                {
                                    labelGroupDoAdd = false;
                                    labelGroup = _labelGroup;
                                }

                            reader.ReadStartElement("labelGroup");
                            while (reader.Name == "labelPart")
                            {
                                Console.WriteLine("     labelPart");
                                RadioLabelPart2 labelPart = new RadioLabelPart2();
                                labelPart.ReadXml(reader, dialogueEvents, charas);

                                if (labelGroupDoAdd)
                                    labelGroup.LabelParts.Add(labelPart);
                            }

                            if (labelGroupDoAdd)
                            {
                                labelGroups.Add(labelGroup);
                            }
                            reader.ReadEndElement();
                        }
                    }
                }
            }
        }
        public void WriteXml(XmlWriter writer)
        {
            writer.WriteStartElement("rdf");
            writer.WriteAttributeString("version", Version.TPP.ToString());
            foreach (RadioGroup2 group in groups)
            {
                group.WriteXml(writer);
                foreach (RadioGroupPart part in group.LabelParts)
                {
                    if (part is RadioLabelPart2 labelPart2)
                    {
                        labelPart2.WriteXml(writer);

                        if (dialogueEvents.Count > labelPart2.DialogueEventIndex && labelPart2.DialogueEventIndex >= 0)
                            dialogueEvents[labelPart2.DialogueEventIndex].WriteXml(writer, "dialogueEvent");
                        else
                            writer.WriteAttributeString("dialogueEvent", "Invalid");

                        if (charas.Count > labelPart2.CharaIndex && labelPart2.CharaIndex >= 0)
                            charas[labelPart2.CharaIndex].WriteXml(writer, "chara");
                        else
                            writer.WriteAttributeString("chara", "Invalid");

                        writer.WriteAttributeString("intervalNextLabelId", labelPart2.IntervalNextLabelId.ToString(CultureInfo.InvariantCulture));
                    }
                    else if (part is RadioLabelGroup _labelGroup)
                    {
                        _labelGroup.WriteXml(writer);
                        writer.WriteAttributeString("intervalNextLabelId", _labelGroup.IntervalNextLabelId.ToString(CultureInfo.InvariantCulture));

                        RadioLabelGroup labelGroup = null;
                        for (int i = 0; i<labelGroups.Count; i++)
                        {
                            if (labelGroups[i].Name.HashValue == _labelGroup.Name.HashValue) 
                            {
                                labelGroup = labelGroups[i];
                                break;
                            }
                        }
                        if (labelGroup != null)
                            for (int i = 0; i < labelGroup.LabelParts.Count; i++)
                            {
                                RadioLabelPart2 labelGroupPart = labelGroup.LabelParts[i];

                                labelGroupPart.WriteXml(writer);

                                if (dialogueEvents.Count > labelGroupPart.DialogueEventIndex && labelGroupPart.DialogueEventIndex >= 0)
                                    dialogueEvents[labelGroupPart.DialogueEventIndex].WriteXml(writer, "dialogueEvent");
                                else
                                    writer.WriteAttributeString("dialogueEvent", "Invalid");

                                if (charas.Count > labelGroupPart.CharaIndex && labelGroupPart.CharaIndex >= 0)
                                    charas[labelGroupPart.CharaIndex].WriteXml(writer, "chara");
                                else
                                    writer.WriteAttributeString("chara", "Invalid");

                                writer.WriteAttributeString("intervalNextLabelId", labelGroupPart.IntervalNextLabelId.ToString(CultureInfo.InvariantCulture));
                                writer.WriteEndElement();
                            }
                    }
                    writer.WriteEndElement();
                }
                writer.WriteEndElement();
            }

            foreach (RadioGroupSet2 groupSet in groupSets)
            {
                groupSet.WriteXml(writer);
            }

            foreach (RadioLabelGroup labelGroup in labelGroups)
            {
                bool isUsed = false;
                foreach (RadioGroup2 group in groups)
                {
                    foreach (RadioGroupPart part in group.LabelParts)
                    {
                        if (part is RadioLabelGroup _labelGroup)
                        {
                            if (labelGroup.Name.HashValue == _labelGroup.Name.HashValue)
                            {
                                isUsed = true;
                                break;
                            }
                        }
                        if (isUsed)
                            break;
                    }
                    if (isUsed)
                        break;
                }
                if (isUsed)
                    continue;

                labelGroup.WriteXml(writer); 
                for (int i = 0; i < labelGroup.LabelParts.Count; i++)
                {
                    RadioLabelPart2 labelGroupPart = labelGroup.LabelParts[i];

                    labelGroupPart.WriteXml(writer);

                    if (dialogueEvents.Count > labelGroupPart.DialogueEventIndex && labelGroupPart.DialogueEventIndex >= 0)
                        dialogueEvents[labelGroupPart.DialogueEventIndex].WriteXml(writer, "dialogueEvent");
                    else
                        writer.WriteAttributeString("dialogueEvent", "Invalid");

                    if (charas.Count > labelGroupPart.CharaIndex && labelGroupPart.CharaIndex >= 0)
                        charas[labelGroupPart.CharaIndex].WriteXml(writer, "chara");
                    else
                        writer.WriteAttributeString("chara", "Invalid");

                    writer.WriteAttributeString("intervalNextLabelId", labelGroupPart.IntervalNextLabelId.ToString(CultureInfo.InvariantCulture));
                    writer.WriteEndElement();
                }
                writer.WriteEndElement();
            }

            writer.WriteEndDocument();
        }
        public XmlSchema GetSchema() {return null;}
    }
}
