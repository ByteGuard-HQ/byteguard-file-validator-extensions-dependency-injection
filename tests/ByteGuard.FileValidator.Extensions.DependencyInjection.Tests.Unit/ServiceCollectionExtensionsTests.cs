using ByteGuard.FileValidator.Configuration;
using ByteGuard.FileValidator.Extensions.DependencyInjection;
using ByteGuard.FileValidator.Extensions.DependencyInjection.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace ByteGuard.FileValidator.Extensions.DependencyInjection.Tests.Unit;

public class ServiceCollectionExtensionsTests
{
    [Fact(DisplayName = "AddFileValidator adds FileValidator to the service collection")]
    public void AddFileValidator_AddsFileValidatorToServiceCollection()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddFileValidator(config =>
        {
            config.SupportedFileTypes = new List<string>() { ".png", ".jpg" };
            config.UnitFileSizeLimit = "25MB";
        });

        var serviceProvider = services.BuildServiceProvider();

        // Assert
        var fileValidator = serviceProvider.GetService<FileValidator>();
        Assert.NotNull(fileValidator);
    }

    [Fact(DisplayName = "AddFileValidator should throws exception when configuration is invalid")]
    public void AddFileValidator_ShouldThrowException_WhenConfigurationIsInvalid()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddFileValidator(config =>
        {
            config.SupportedFileTypes = []; // Invalid configuration - missing supported file types
        });

        var serviceProvider = services.BuildServiceProvider();

        // Assert
        Assert.ThrowsAny<Exception>(() => serviceProvider.GetRequiredService<FileValidator>());
    }

    [Fact(DisplayName = "AddFileValidator should set correct file size limit when using the friendly file size limit property")]
    public void AddFileValidator_FriendlyFileSizeLimitSet_ShouldAddAFileValidator()
    {
        // Arrange
        var services = new ServiceCollection();
        Action<FileValidatorSettingsConfiguration> configAction = options =>
        {
            options.SupportedFileTypes = [".pdf"];
            options.UnitFileSizeLimit = "25MB";
        };

        // Act
        services.AddFileValidator(configAction);

        // Assert
        var sp = services.BuildServiceProvider();
        var options = sp.GetRequiredService<IOptions<FileValidatorConfiguration>>();
        Assert.Equal(ByteSize.MegaBytes(25), options.Value.FileSizeLimit);
    }

    [Fact(DisplayName = "AddFileValidator should use file size limit over friendly file size limit if both are set")]
    public void AddFileValidator_FileSizeLimitAndFriendlyFileSizeLimitBothSet_ShouldUseFileSizeLimitOverFriendlyFileSizeLimit()
    {
        // Arrange
        var services = new ServiceCollection();
        Action<FileValidatorSettingsConfiguration> configAction = options =>
        {
            options.SupportedFileTypes = [".pdf"];
            options.FileSizeLimit = ByteSize.MegaBytes(10);
            options.UnitFileSizeLimit = "25MB";
        };

        // Act
        services.AddFileValidator(configAction);

        // Assert
        var sp = services.BuildServiceProvider();
        var options = sp.GetRequiredService<IOptions<FileValidatorConfiguration>>();
        Assert.Equal(ByteSize.MegaBytes(10), options.Value.FileSizeLimit);
    }
}
