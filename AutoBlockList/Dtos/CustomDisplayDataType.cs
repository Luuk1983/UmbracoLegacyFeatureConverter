using System.Runtime.Serialization;

namespace Umbraco.Community.LegacyFeatureConverter.Dtos
{
    public class CustomDisplayDataType
    {
        [DataMember]
        public int Id { get; set; }

        [DataMember]
        public string? Icon { get; set; }

        [DataMember]
        public string? Name { get; set; }
    }
}
