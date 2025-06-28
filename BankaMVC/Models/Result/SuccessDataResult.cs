using Newtonsoft.Json;
using System.Xml.Serialization;

namespace BankaMVC.Models.Result
{
    [XmlRoot("SuccessDataResult")]
    public class SuccessDataResult<T>
    {
        [XmlElement("Success")]
        public bool Success { get; set; }

        [XmlElement("Message")]
        public string Message { get; set; }

        [XmlArray("Data")]
        [XmlArrayItem("Card")]
        public T Data { get; set; }
    }
}
