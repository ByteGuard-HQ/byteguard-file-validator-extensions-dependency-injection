using ByteGuard.FileValidator.Scanners;
using Microsoft.Extensions.Configuration;

namespace ByteGuard.FileValidator.Extensions.DependencyInjection.Configuration;

/// <summary>
/// Registration information for an antimalware scanner.
/// </summary>
public sealed class ScannerRegistration
{
    /// <summary>
    /// Type of the scanner to register. Must be derived from IAntimalwareScanner.
    /// </summary>
    public string ScannerType { get; set; } = default!;

    private Type? _scannerType;

    /// <summary>
    /// Gets or sets the <see cref="Type"/> of the scanner.
    /// </summary>
    public Type Type
    {
        get
        {
            if (_scannerType is not null)
                return _scannerType;

            _scannerType = Type.GetType(ScannerType, throwOnError: false)!;
            return _scannerType;
        }
        set
        {
            _scannerType = value;
            ScannerType = value.AssemblyQualifiedName!;
        }
    }

    /// <summary>
    /// Options for the scanner.
    /// </summary>
    public object? OptionsInstance { get; set; }

    /// <summary>
    /// Raw configuration section for options (for appsettings registration).
    /// </summary>
    public IConfigurationSection? OptionsConfiguration { get; set; }

    /// <summary>
    /// Creates a new <see cref="ScannerRegistration"/> instance for the specified scanner type and options.
    /// </summary>
    /// <param name="options">Scanner options.</param>
    /// <typeparam name="TScanner">Scanner implementation inheriting from IAntimalwareScanner.</typeparam>
    /// <typeparam name="TOptions">Scanner options.</typeparam>
    public static ScannerRegistration Create<TScanner, TOptions>(Action<TOptions> options)
        where TScanner : IAntimalwareScanner<TOptions>
        where TOptions : class, new()
    {
        var opts = new TOptions();
        options?.Invoke(opts);

        return new ScannerRegistration
        {
            Type = typeof(TScanner),
            OptionsInstance = opts
        };
    }

    /// <summary>
    /// Creates a new <see cref="ScannerRegistration"/> instance for the specified scanner type and options.
    /// </summary>
    /// <param name="options">Scanner options.</param>
    /// <typeparam name="TScanner">Scanner implementation inheriting from IAntimalwareScanner.</typeparam>
    /// <typeparam name="TOptions">Scanner options.</typeparam>
    public static ScannerRegistration Create<TScanner, TOptions>(TOptions options)
        where TScanner : IAntimalwareScanner<TOptions>
        where TOptions : class, new()
    {
        return new ScannerRegistration
        {
            Type = typeof(TScanner),
            OptionsInstance = options
        };
    }
}
