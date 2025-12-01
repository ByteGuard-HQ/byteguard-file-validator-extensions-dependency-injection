# ByteGuard.FileValidator.Extensions.DependencyInjection ![NuGet Version](https://img.shields.io/nuget/v/ByteGuard.FileValidator.Extensions.DependencyInjection)

`ByteGuard.FileValidator.Extensions.DependencyInjection` provides first-class integration of `ByteGuard.FileValidator` with `Microsoft.Extensions.DependencyInjection`.

It gives you: 
- Extension methods to register the file validator in the DI container
- Easy configuration via `appsettings.json` or fluent configuration in code

> This package is the `Microsoft.Extensions.DependencyInjection` integration layer.
> The core validation logic lives in [`ByteGuard.FileValidator`](https://github.com/ByteGuard-HQ/byteguard-file-validator-net).

## Getting Started

### Installation
This package is published and installed via [NuGet](https://www.nuget.org/packages/ByteGuard.FileValidator.Extensions.DependencyInjection).

Reference the package in your project:
```bash
dotnet add package ByteGuard.FileValidator.Extensions.DependencyInjection
```

## Usage

### Add to DI container
In your `Program.cs` (or `Startup.cs` in older projects), register the validator:

**Using inline configuration**
```csharp
// Using inline configuration
builder.Services.AddFileValidator(options => 
{
    options.AllowFileTypes(FileExtensions.Pdf, FileExtensions.Jpg, FileExtensions.Png);
    options.FileSizeLimit = ByteSize.MegaBytes(25);
    options.ThrowOnInvalidFiles(false);

    // If an antimalware package has been added
    options.Scanner = ScannerRegistration.Create<ScannerImplementation, ScannerImplementationOptions>(opts => 
    {
        // Refer to the individual scanner implementations for ScannerType value and possible options.
        // ...
    })
});
```

**Using configuration from appsettings.json with default "FileValidator" section name**
```csharp
builder.Services.AddFileValidator(builder.Configuration);
```

**Using configuration from appsettings.json with custom section name**
```csharp
builder.Services.AddFileValidator(builder.Configuration, "MySection");
```

### Injection & Usage
You can then inject `FileValidator` into your services and other classes.

```csharp
public class MyService
{
    private readonly FileValidator _fileValidator;

    public MyService(FileValidator fileValidator)
    {
        _fileValidator = fileValidator;
    }

    public bool SaveFile(Stream fileStream, string fileName)
    {
        var isValid = _fileValidator.IsValidFile(fileName, fileStream);
        
        // ...
    }
}
```

### Configuration via appsettings
It's possible to configure the `FileValidator` through `appsettings.json`.

> _ℹ️ As you'll notice below, you can either define the `FileSizeLimit` in raw byte size, or use the `UnitFileSizeLimit` to define
> the file size in a more human readable format. When both are defined, `FileSizeLimit` always wins over `UnitFileSizeLimit`._

```json
{
  "FileValidatorConfiguration": {
    "SupportedFileTypes": [ ".pdf", ".jpg", ".png" ],
    "FileSizeLimit": 26214400,
    "UnitFileSizeLimit": "25MB",
    "ThrowExceptionOnInvalidFile": true
  }
}
```

**With antimalware scanner**  
It's possible to configure an antimalware scanner directly through `appsettings.json`.

> ℹ️ _Refer to the individual scanner implementations for `ScannerType` value and possible options._

```json
{
  "FileValidatorConfiguration": {
    "SupportedFileTypes": [ ".pdf", ".jpg", ".png" ],
    "FileSizeLimit": 26214400,
    "UnitFileSizeLimit": "25MB",
    "ThrowExceptionOnInvalidFile": true,
    "Scanner": {
      "ScannerType": "...",
      "Options": {
        "OptionA": "...",
        "OptionB": "..."
      }
    }
  }
}
```

## License
_ByteGuard.FileValidator.Extensions.DpendencyInjection is Copyright © ByteGuard Contributors - Provided under the MIT license._