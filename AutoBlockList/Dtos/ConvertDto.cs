using System.Runtime.Serialization;

namespace Umbraco.Community.LegacyFeatureConverter.Dtos
{
	public class ConvertDto
	{
		[DataMember]
		public IEnumerable<AutoBlockListContent>? Contents { get; set; }
        
		[DataMember]
		public string? ConnectionId { get; set; }
    }
}