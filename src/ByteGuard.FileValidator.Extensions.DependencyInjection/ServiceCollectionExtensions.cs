using ByteGuard.FileValidator.Configuration;
using ByteGuard.FileValidator.Extensions.DependencyInjection.Configuration;
using ByteGuard.FileValidator.Scanners;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace ByteGuard.FileValidator.Extensions.DependencyInjection;

/// <summary>
/// Extension methods for registering the File Validator services in an <see cref="IServiceCollection"/>.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// The default configuration section name for File Validator settings.
    /// </summary>
    public const string DefaultSectionName = "FileValidator";

    /// <summary>
    /// Adds the File Validator services to the specified <see cref="IServiceCollection"/> with configuration from the provided <see cref="IConfiguration"/>.
    /// </summary>
    /// <remarks>
    /// This method binds the configuration section named "FileValidator" by default.
    /// </remarks>
    /// <param name="services">Service collection.</param>
    /// <param name="configuration">Configuration.</param>
    public static IServiceCollection AddFileValidator(this IServiceCollection services, IConfiguration configuration)
    {
        return services.AddFileValidator(options => configuration.GetSection(DefaultSectionName).Bind(options));
    }

    /// <summary>
    /// Adds the File Validator services to the specified <see cref="IServiceCollection"/> with configuration from the provided <see cref="IConfiguration"/>.
    /// </summary>
    /// <param name="services">Service collection.</param>
    /// <param name="configuration">Configuration.</param>
    /// <param name="sectionName">Section name.</param>
    public static IServiceCollection AddFileValidator(this IServiceCollection services, IConfiguration configuration, string sectionName)
    {
        return services.AddFileValidator(options => configuration.GetSection(sectionName).Bind(options));
    }

    /// <summary>
    /// Adds the File Validator services to the specified <see cref="IServiceCollection"/> with custom configuration options.
    /// </summary>
    /// <param name="services">Service collection.</param>
    /// <param name="options">Configuration options.</param>
    public static IServiceCollection AddFileValidator(this IServiceCollection services, Action<FileValidatorSettingsConfiguration> options)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(options);

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

        // Register antimalware scanner.
        var settings = new FileValidatorSettingsConfiguration();
        options(settings);

        RegisterConfiguredScanner(services, settings.Scanner);

        // Register the FileValidator service.
        services.AddSingleton(serviceProvider =>
        {
            var configuration = serviceProvider.GetRequiredService<IOptions<FileValidatorConfiguration>>().Value;

            // If an antimalware scanner is registered, resolve it and pass it to the FileValidator.
            var antimalwareScanner = serviceProvider.GetService<IAntimalwareScanner>();
            if (antimalwareScanner is not null)
            {
                return new FileValidator(configuration, antimalwareScanner);
            }

            // No antimalware scanner registered.
            return new FileValidator(configuration);
        });

        return services;
    }

    /// <summary>
    /// Registers the configured antimalware scanner.
    /// </summary>
    /// <param name="services">Service collection.</param>
    /// <param name="scanner">Scanner registration.</param>
    private static void RegisterConfiguredScanner(IServiceCollection services, ScannerRegistration? scanner)
    {
        // If no scanner has been registerered.
        if (scanner is null) return;

        var scannerType = scanner.Type;
        if (scannerType is null)
        {
            throw new InvalidOperationException("The specified scanner type could not be resolved.");
        }

        if (!typeof(IAntimalwareScanner).IsAssignableFrom(scanner.Type))
        {
            throw new InvalidOperationException($"The specified scanner type '{scanner.Type.FullName}' does not implement the '{nameof(IAntimalwareScanner)}' interface.");
        }

        var genericScannerInterface = scanner.Type
            .GetInterfaces()
            .FirstOrDefault(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IAntimalwareScanner<>));

        var optionsType = genericScannerInterface?.GetGenericArguments()[0];

        // Register scanner as IAntimalwareScanner implementation.
        services.AddSingleton(_ =>
        {
            object? optionsInstance = scanner.OptionsInstance;
            if (optionsInstance is null && scanner.OptionsConfiguration is not null)
            {
                if (optionsType is null)
                {
                    throw new InvalidOperationException($"Scanner '{scannerType.FullName}' must implement IAntimalwareScanner<TOptions> to be configured from appsettings.");
                }

                optionsInstance = Activator.CreateInstance(optionsType);
                if (optionsInstance is null)
                {
                    throw new InvalidOperationException($"Could not create options instance of type '{optionsType.FullName}'.");
                }

                scanner.OptionsConfiguration.Bind(optionsInstance);
            }

            object? impl;

            if (optionsInstance is not null)
            {
                impl = Activator.CreateInstance(scanner.Type, optionsInstance);
            }
            else
            {
                impl = Activator.CreateInstance(scanner.Type);
            }

            if (impl is not IAntimalwareScanner typedScanner)
            {
                throw new InvalidOperationException($"Scanner type '{scanner.Type.FullName}' does not implement the '{nameof(IAntimalwareScanner)}' interface.");
            }

            return typedScanner;
        });
    }
}
