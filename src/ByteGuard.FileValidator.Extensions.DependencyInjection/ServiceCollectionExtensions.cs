using ByteGuard.FileValidator.Configuration;
using ByteGuard.FileValidator.Extensions.DependencyInjection.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace ByteGuard.FileValidator.Extensions.DependencyInjection;

/// <summary>
/// Extension methods for registering the File Validator services in an <see cref="IServiceCollection"/>.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds the File Validator services to the specified <see cref="IServiceCollection"/> with custom configuration options.
    /// </summary>
    /// <param name="services">Service collection.</param>
    /// <param name="options">Configuration options.</param>
    public static IServiceCollection AddFileValidator(this IServiceCollection services, Action<FileValidatorSettingsConfiguration> options)
    {
        // Validate and setup configuration options.
        services.AddSingleton<IValidateOptions<FileValidatorConfiguration>,
            FileValidatorConfigurationOptionsValidator>();

        services.Configure(options);

        services.AddOptions<FileValidatorConfiguration>()
            .Configure<IOptions<FileValidatorSettingsConfiguration>>((cfg, settings) =>
            {
                // Convert from FileValidatorSettingsConfiguration to FileValidatorConfiguration.
                cfg.SupportedFileTypes = settings.Value.SupportedFileTypes;
                cfg.ThrowExceptionOnInvalidFile = settings.Value.ThrowExceptionOnInvalidFile;

                if (settings.Value.FileSizeLimit != -1)
                {
                    cfg.FileSizeLimit = settings.Value.FileSizeLimit;
                }
                else if (!string.IsNullOrWhiteSpace(settings.Value.UnitFileSizeLimit))
                {
                    cfg.FileSizeLimit = ByteSize.Parse(settings.Value.UnitFileSizeLimit);
                }
            })
            .ValidateOnStart();

        // Register the FileValidator service.
        services.AddSingleton(serviceProvider =>
        {
            var configuration = serviceProvider.GetRequiredService<IOptions<FileValidatorConfiguration>>().Value;
            return new FileValidator(configuration);
        });

        return services;
    }
}
