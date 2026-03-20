using System.Runtime.Serialization;
using static Umbraco.Community.LegacyFeatureConverter.Constants.AutoBlockListConstants;

namespace Umbraco.Community.LegacyFeatureConverter.Dtos
{
    public class ConvertReport
    {
        [DataMember]
        public string? Task { get; set; }

        [DataMember]
        public Status Status { get; set; }

        [DataMember]
        public string? ErrorMessage { get; set; }
    }
}
