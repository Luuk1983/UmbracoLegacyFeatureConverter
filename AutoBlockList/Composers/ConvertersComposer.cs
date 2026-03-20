using Microsoft.Extensions.DependencyInjection;
using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Core.DependencyInjection;
using Umbraco.Community.LegacyFeatureConverter.Converters;
using Umbraco.Community.LegacyFeatureConverter.Services;
using Umbraco.Community.LegacyFeatureConverter.Services.interfaces;

namespace Umbraco.Community.LegacyFeatureConverter.Composers;

/// <summary>
/// Composer to register property converters as scoped services.
/// </summary>
public class ConvertersComposer : IComposer
{
    public void Compose(IUmbracoBuilder builder)
    {
        // Register converter discovery service
        builder.Services.AddScoped<IConverterService, ConverterService>();

        // Register converters as scoped services
        builder.Services.AddScoped<IPropertyConverter, NestedContentConverter>();
        builder.Services.AddScoped<IPropertyConverter, MediaPickerConverter>();

        // Future converters can be added here, e.g.:
        // builder.Services.AddScoped<IPropertyConverter, MacroConverter>();
    }
}
