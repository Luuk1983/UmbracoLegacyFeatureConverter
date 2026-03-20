using Umbraco.Community.LegacyFeatureConverter.Hubs;

namespace Umbraco.Community.LegacyFeatureConverter.Services.interfaces
{
	public interface IAutoBlockListContext
	{
		IAutoBlockListHubClient? Client { get; }
		void SetClient(IAutoBlockListHubClient client);
		void ClearClient();
	}
}