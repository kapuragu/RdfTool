using System;
using System.IO;
using System.Xml;
using System.Xml.Schema;
using System.Collections.Generic;

namespace RdfTool
{
    public class RadioGroupSet
    {
        public FoxHash Name { get; set; }

        public List<FoxHash> GroupNames = new List<FoxHash>();
        public void ReadXml(XmlReader reader)
        {
            Name = new FoxHash();
            Name.ReadXml(reader, "id");
            bool doNodeLoop = true;
            if (reader.IsEmptyElement)
                doNodeLoop = false;
            reader.ReadStartElement("groupSet");
            while (doNodeLoop)
                switch (reader.NodeType)
                {
                    case XmlNodeType.Element:
                        FoxHash label = new FoxHash();
                        label.ReadXml(reader, "groupId");
                        GroupNames.Add(label);
                        reader.ReadStartElement("groupId");
                        Console.WriteLine($"    {label.HashValue}");
                        continue;
                    case XmlNodeType.EndElement:
                        doNodeLoop = false;
                        return;
                }
        }

        public void WriteXml(XmlWriter writer)
        {
            writer.WriteStartElement("groupSet");
            Name.WriteXml(writer, "id");
            foreach (FoxHash groupName in GroupNames)
            {
                writer.WriteStartElement("groupId");
                groupName.WriteXml(writer, "groupId");
                writer.WriteEndElement();
            }
            writer.WriteEndElement();
        }

        public XmlSchema GetSchema() { return null; }
    }
}
