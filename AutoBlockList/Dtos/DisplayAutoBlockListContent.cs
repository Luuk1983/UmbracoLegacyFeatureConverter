using Umbraco.Cms.Core.Models;
using System.Runtime.Serialization;

namespace Umbraco.Community.LegacyFeatureConverter.Dtos
{
    public class DisplayAutoBlockListContent : AutoBlockListContent
	{
		[DataMember]
        public ISimpleContentType? ContentType { get; set; }
    }
}
