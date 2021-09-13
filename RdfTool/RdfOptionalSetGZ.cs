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
