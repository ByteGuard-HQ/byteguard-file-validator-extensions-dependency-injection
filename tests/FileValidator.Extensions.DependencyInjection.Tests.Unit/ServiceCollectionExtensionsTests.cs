using ByteGuard.FileValidator.Configuration;
using ByteGuard.FileValidator.Extensions.DependencyInjection.Configuration;
using ByteGuard.FileValidator.Scanners;
using FileValidator.Extensions.DependencyInjection.Tests.Unit;
using Microsoft.Extensions.Configuration;
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

    [Fact(DisplayName = "AddFileValidator should register the configured antimalware scanner")]
    public void AddFileValidator_ConfiguredAntimalwareScanner_ShouldRegisterScanner()
    {
        // Arrange
        var services = new ServiceCollection();
        Action<FileValidatorSettingsConfiguration> configAction = options =>
        {
            options.SupportedFileTypes = [".pdf"];
            options.FileSizeLimit = ByteSize.MegaBytes(15);
            options.Scanner = ScannerRegistration.Create<MockAntimalwareScanner, MockAntimalwareScannerOptions>(opts =>
            {
                opts.OptionA = "TestOption";
                opts.OptionB = 42;
            });
        };

        // Act
        services.AddFileValidator(configAction);

        // Assert
        var sp = services.BuildServiceProvider();
        var scanner = sp.GetRequiredService<IAntimalwareScanner>();
        Assert.IsType<MockAntimalwareScanner>(scanner);
    }

    [Fact(DisplayName = "AddFileValidator should register the configured antimalware scanner without options")]
    public void AddFileValidator_ConfiguredAntimalwareScannerWithoutOptions_ShouldRegisterScanner()
    {
        // Arrange
        var services = new ServiceCollection();
        Action<FileValidatorSettingsConfiguration> configAction = options =>
        {
            options.SupportedFileTypes = [".pdf"];
            options.FileSizeLimit = ByteSize.MegaBytes(15);
            options.Scanner = ScannerRegistration.Create<MockAntimalwareScanner, MockAntimalwareScannerOptions>(_ => { });
        };

        // Act
        services.AddFileValidator(configAction);

        // Assert
        var sp = services.BuildServiceProvider();
        var scanner = sp.GetRequiredService<IAntimalwareScanner>();
        Assert.IsType<MockAntimalwareScanner>(scanner);
    }

    [Fact(DisplayName = "AddFileValidator should not register an antimalware scanner when none is configured")]
    public void AddFileValidator_NoConfiguredAntimalwareScanner_ShouldNotRegisterScanner()
    {
        // Arrange
        var services = new ServiceCollection();
        Action<FileValidatorSettingsConfiguration> configAction = options =>
        {
            options.SupportedFileTypes = [".pdf"];
            options.FileSizeLimit = ByteSize.MegaBytes(15);
            options.Scanner = null; // No scanner configured
        };

        // Act
        services.AddFileValidator(configAction);

        // Assert
        var sp = services.BuildServiceProvider();
        var scanner = sp.GetService<IAntimalwareScanner>();
        Assert.Null(scanner);
    }

    [Fact(DisplayName = "AddFileValidator should throw exception when configured antimalware scanner type is invalid")]
    public void AddFileValidator_ConfiguredAntimalwareScannerWithInvalidType_ShouldThrowException()
    {
        // Arrange
        var services = new ServiceCollection();
        Action<FileValidatorSettingsConfiguration> configAction = options =>
        {
            options.SupportedFileTypes = [".pdf"];
            options.FileSizeLimit = ByteSize.MegaBytes(15);
            options.Scanner = new ScannerRegistration
            {
                ScannerType = "Unknown.Namespace.ImplementationScanner, ImplementationScanner", // Invalid type, does not implement IAntimalwareScanner
                OptionsInstance = null
            };
        };

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => services.AddFileValidator(configAction));
    }

    [Fact(DisplayName = "AddFileValidator should inject the antimalware scanner into the FileValidator")]
    public void AddFileValidator_ConfiguredAntimalwareScanner_ShouldInjectScannerIntoFileValidator()
    {
        // Arrange
        var services = new ServiceCollection();
        Action<FileValidatorSettingsConfiguration> configAction = options =>
        {
            options.SupportedFileTypes = [".pdf"];
            options.FileSizeLimit = ByteSize.MegaBytes(15);
            options.Scanner = ScannerRegistration.Create<MockAntimalwareScanner, MockAntimalwareScannerOptions>(opts =>
            {
                opts.OptionA = "InjectedOption";
                opts.OptionB = 99;
            });
        };

        // Act
        services.AddFileValidator(configAction);

        // Assert
        var sp = services.BuildServiceProvider();
        var fileValidator = sp.GetRequiredService<FileValidator>();
        Assert.NotNull(fileValidator);

        var scannerField = typeof(FileValidator).GetField("_antimalwareScanner", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        Assert.NotNull(scannerField);

        var scannerInstance = scannerField.GetValue(fileValidator) as MockAntimalwareScanner;
        Assert.NotNull(scannerInstance);
    }

    [Fact(DisplayName = "AddFileValidator should create FileValidator without antimalware scanner when none is configured")]
    public void AddFileValidator_NoConfiguredAntimalwareScanner_ShouldCreateFileValidatorWithoutScanner()
    {
        // Arrange
        var services = new ServiceCollection();
        Action<FileValidatorSettingsConfiguration> configAction = options =>
        {
            options.SupportedFileTypes = [".pdf"];
            options.FileSizeLimit = ByteSize.MegaBytes(15);
            options.Scanner = null; // No scanner configured
        };

        // Act
        services.AddFileValidator(configAction);

        // Assert
        var sp = services.BuildServiceProvider();
        var fileValidator = sp.GetRequiredService<FileValidator>();
        Assert.NotNull(fileValidator);

        var scannerField = typeof(FileValidator).GetField("_antimalwareScanner", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        Assert.NotNull(scannerField);

        var scannerInstance = scannerField.GetValue(fileValidator);
        Assert.Null(scannerInstance);
    }

    [Fact(DisplayName = "AddFileValidator should register the scanner when configuration has been provided through appsettings.json")]
    public void AddFileValidator_ConfigurationFromAppSettings_ShouldRegisterScanner()
    {
        // Arrange
        var services = new ServiceCollection();

        var inMemorySettings = new Dictionary<string, string?>
        {
            {"FileValidator:SupportedFileTypes:0", ".pdf"},
            {"FileValidator:UnitFileSizeLimit", "15MB"},
            {"FileValidator:Scanner:ScannerType", "FileValidator.Extensions.DependencyInjection.Tests.Unit.MockAntimalwareScanner, FileValidator.Extensions.DependencyInjection.Tests.Unit"},
            {"FileValidator:Scanner:Options:OptionA", "ConfigOption"},
            {"FileValidator:Scanner:Options:OptionB", "123"}
        };

        IConfiguration configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(inMemorySettings.AsEnumerable())
            .Build();

        // Act
        services.AddFileValidator(configuration);

        // Assert
        var sp = services.BuildServiceProvider();
        var scanner = sp.GetRequiredService<IAntimalwareScanner>();
        Assert.IsType<MockAntimalwareScanner>(scanner);
    }

    [Fact(DisplayName = "AddFileValidator should register the scanner when configuration has been provided through appsettings.json")]
    public void AddFileValidator_ConfigurationFromAppSettingsCustomSectionName_ShouldRegisterScanner()
    {
        // Arrange
        var services = new ServiceCollection();

        var inMemorySettings = new Dictionary<string, string?>
        {
            {"CustomName:SupportedFileTypes:0", ".pdf"},
            {"CustomName:UnitFileSizeLimit", "15MB"},
            {"CustomName:Scanner:ScannerType", "FileValidator.Extensions.DependencyInjection.Tests.Unit.MockAntimalwareScanner, FileValidator.Extensions.DependencyInjection.Tests.Unit"},
            {"CustomName:Scanner:Options:OptionA", "ConfigOption"},
            {"CustomName:Scanner:Options:OptionB", "123"}
        };

        IConfiguration configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(inMemorySettings.AsEnumerable())
            .Build();

        // Act
        services.AddFileValidator(configuration, "CustomName");

        // Assert
        var sp = services.BuildServiceProvider();
        var scanner = sp.GetRequiredService<IAntimalwareScanner>();
        Assert.IsType<MockAntimalwareScanner>(scanner);
    }
}
