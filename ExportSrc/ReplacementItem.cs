#region usings

using System.Xml.Serialization;

#endregion

namespace ExportSrc
{
    public class ReplacementItem
    {
        [XmlAttribute("by")]
        public string ReplacementText { get; set; }

        [XmlAttribute("text")]
        public string SearchText { get; set; }
    }
}