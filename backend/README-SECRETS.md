# Secrets Configuration

Sensitive values are loaded from a `.env` file, not from committed configuration files.

## Required Environment Variables

| Variable | Purpose |
|----------|---------|
| `LEARNPATH_NVIDIA_API_KEY` | NVIDIA API key for AI-powered feedback generation |
| `LEARNPATH_GROQ_API_KEY` | Groq API key for Question Bank AI (future use) |
| `LEARNPATH_JWT_SECRET` | Secret key used to sign and verify JWT tokens (minimum 32 characters) |
| `LEARNPATH_DB_CONNECTION` | SQL Server connection string for the application database |

## Setup

1. Copy `.env.example` to `.env` in the `backend/` directory:

```pwsh
copy .env.example .env
```

2. Open `.env` and fill in your real values:

```
LEARNPATH_NVIDIA_API_KEY=nvapi-your-real-key
LEARNPATH_GROQ_API_KEY=gsk-your-real-key
LEARNPATH_JWT_SECRET=a-32-plus-character-secret-key
LEARNPATH_DB_CONNECTION=Server=(localdb)\mssqllocaldb;Database=LearnPathDb;Trusted_Connection=True;TrustServerCertificate=True
```

## Verification

Run the application:

```pwsh
dotnet build
dotnet run
```

If any environment variable is missing, the application will fail to start with a clear message listing every missing variable.

## Switching AI Providers

The `AiOptions:ApiKey` key is populated from `LEARNPATH_NVIDIA_API_KEY`. If you switch to a different provider (e.g. Groq), update the env var name mapping in `Program.cs` and reconfigure the provider and model in `appsettings.json`.
