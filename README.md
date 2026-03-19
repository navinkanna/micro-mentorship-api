# micro-mentorship-api
.Net API for Micro Mentorship App

## Secret Management

Do not store production secrets in `appsettings.json`.

For local development, use ASP.NET Core user secrets:

```powershell
dotnet user-secrets set "JwtSettings:securityKey" "replace-with-a-long-random-secret" --project .\MicroMentorshipAPI\MicroMentorshipAPI.csproj
dotnet user-secrets set "ConnectionStrings:postgreConnection" "Host=...;Port=5432;Database=...;Username=...;Password=...;SSL Mode=Require;Trust Server Certificate=true" --project .\MicroMentorshipAPI\MicroMentorshipAPI.csproj
```

For production hosting, set environment variables instead:

```text
JwtSettings__securityKey=replace-with-a-long-random-secret
ConnectionStrings__postgreConnection=Host=...;Port=5432;Database=...;Username=...;Password=...;SSL Mode=Require;Trust Server Certificate=true
```

ASP.NET Core maps double underscores in environment variables to nested configuration keys.
