#region usings

using System;
using System.Xml.Serialization;

#endregion

namespace ExportSrc
{
    public class Project
    {
        [XmlAttribute]
        public Guid Id { get; set; }

        [XmlAttribute]
        public string Name { get; set; }
    }
}