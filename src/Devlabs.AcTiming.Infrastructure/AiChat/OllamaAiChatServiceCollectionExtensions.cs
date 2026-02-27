using Devlabs.AcTiming.Application.Shared;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Devlabs.AcTiming.Infrastructure.AiChat;

internal static class OllamaAiChatServiceCollectionExtensions
{
    public static IServiceCollection AddOllamaAiChat(
        this IServiceCollection services,
        IConfiguration configuration
    )
    {
        services
            .AddOptions<OllamaAiChatOptions>()
            .Bind(configuration.GetSection(OllamaAiChatOptions.SectionName))
            .Validate(
                o =>
                {
                    if (!o.IsEnabled)
                    {
                        return true;
                    }

                    return o.BaseUri is not null
                        && o.BaseUri.IsAbsoluteUri
                        && (
                            o.BaseUri.Scheme == Uri.UriSchemeHttp
                            || o.BaseUri.Scheme == Uri.UriSchemeHttps
                        )
                        && !string.IsNullOrWhiteSpace(o.Model);
                },
                "AiChat is enabled but BaseUri/Model is not configured properly."
            )
            .ValidateOnStart();

        services.AddScoped<IAiChat, OllamaAiChat>();
        return services;
    }
}
