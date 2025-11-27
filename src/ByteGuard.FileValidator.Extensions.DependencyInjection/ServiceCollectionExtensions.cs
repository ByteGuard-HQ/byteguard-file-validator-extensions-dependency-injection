using Microsoft.Extensions.DependencyInjection;

namespace ByteGuard.FileValidator.Extensions.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddFileValidator(this IServiceCollection services, Action<Configuration.FileValidatorSettingsConfiguration> configureSettings)
    {
        // TODO.

        return services;
    }
}
