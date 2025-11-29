using ByteGuard.FileValidator.Configuration;
using Microsoft.Extensions.Options;

namespace ByteGuard.FileValidator.Extensions.DependencyInjection.Configuration;

/// <summary>
/// File validator configuration options validator for use with the options pattern.
/// </summary>
public class FileValidatorConfigurationOptionsValidator : IValidateOptions<FileValidatorConfiguration>
{
    /// <inheritdoc />
    public ValidateOptionsResult Validate(string? name, FileValidatorConfiguration config)
    {
        try
        {
            ConfigurationValidator.ThrowIfInvalid(config);
            return ValidateOptionsResult.Success;
        }
        catch (Exception ex)
        {
            return ValidateOptionsResult.Fail(ex.Message);
        }
    }
}
