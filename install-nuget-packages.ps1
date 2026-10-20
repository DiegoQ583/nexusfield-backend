$ErrorActionPreference = "Stop"

# Stable versions compatible with TargetFramework net10.0.
$MicrosoftNet10Version = "10.0.11"
$FluentValidationVersion = "12.1.1"
$BCryptVersion = "4.2.0"
$DotNetEnvVersion = "3.2.0"
$ScalarVersion = "2.17.2"

dotnet add .\NexusField.Application\NexusField.Application.csproj package FluentValidation.DependencyInjectionExtensions --version $FluentValidationVersion
dotnet add .\NexusField.Application\NexusField.Application.csproj package BCrypt.Net-Next --version $BCryptVersion

dotnet add .\NexusField.Infrastructure\NexusField.Infrastructure.csproj package Microsoft.EntityFrameworkCore.SqlServer --version $MicrosoftNet10Version
dotnet add .\NexusField.Infrastructure\NexusField.Infrastructure.csproj package Microsoft.EntityFrameworkCore.Tools --version $MicrosoftNet10Version

dotnet add .\NexusField.API\NexusField.API.csproj package DotNetEnv --version $DotNetEnvVersion
dotnet add .\NexusField.API\NexusField.API.csproj package Scalar.AspNetCore --version $ScalarVersion
dotnet add .\NexusField.API\NexusField.API.csproj package Microsoft.AspNetCore.Authentication.JwtBearer --version $MicrosoftNet10Version
