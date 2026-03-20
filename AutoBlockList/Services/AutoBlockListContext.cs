using Umbraco.Community.LegacyFeatureConverter.Hubs;
using Umbraco.Community.LegacyFeatureConverter.Services.interfaces;

namespace Umbraco.Community.LegacyFeatureConverter.Services
{
    public class AutoBlockListContext : IAutoBlockListContext
    {
        private IAutoBlockListHubClient? _client;

        public IAutoBlockListHubClient? Client => _client;

        public void SetClient(IAutoBlockListHubClient client)
        {
            _client = client;
        }

        public void ClearClient()
        {
            _client = null;
        }
    }
}