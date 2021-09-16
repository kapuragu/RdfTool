using System;
using System.IO;
using System.Xml;
using System.Xml.Schema;
using System.Collections.Generic;

namespace RdfTool
{
    public class RdfOptionalSetGZ
    {
        public FoxHash OptionalSetName { get; set; }

        public List<FoxHash> LabelNames = new List<FoxHash>();
        public void ReadXml(XmlReader reader)
        {
            OptionalSetName = new FoxHash();
            OptionalSetName.ReadXml(reader, "optionalSetName");
            bool doNodeLoop = true;
            if (reader.IsEmptyElement)
                doNodeLoop = false;
            reader.ReadStartElement("optionalSet");
            while (doNodeLoop)
                switch (reader.NodeType)
                {
                    case XmlNodeType.Element:
                        FoxHash label = new FoxHash();
                        label.ReadXml(reader, "labelName");
                        LabelNames.Add(label);
                        reader.ReadStartElement("label");
                        Console.WriteLine($"    {label.HashValue}");
                        continue;
                    case XmlNodeType.EndElement:
                        doNodeLoop = false;
                        reader.Read();
                        return;
                }
        }

        public void WriteXml(XmlWriter writer)
        {
            writer.WriteStartElement("optionalSet");
            OptionalSetName.WriteXml(writer, "optionalSetName");
            foreach (FoxHash labelName in LabelNames)
            {
                writer.WriteStartElement("label");
                labelName.WriteXml(writer, "labelName");
                writer.WriteEndElement();
            }
            writer.WriteEndElement();
        }

        public XmlSchema GetSchema() { return null; }
    }
}
