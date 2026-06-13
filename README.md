# Card Wallet Platform

A comprehensive card wallet platform built with .NET, featuring admin management, user authentication, card exchange, and wallet operations.

## Features

- User authentication and authorization
- Card rate management
- Wallet management
- Card exchange functionality
- KYC (Know Your Customer) integration
- Admin dashboard
- User management
- Withdrawal management
- Points system

## Technology Stack

- **Backend**: ASP.NET Core 6+
- **Database**: MySQL 8.0+
- **ORM**: Entity Framework Core
- **Authentication**: JWT
- **Validation**: FluentValidation

## Project Structure

```
backend/
  ├── CardWallet.Api/          # API layer with controllers
  ├── CardWallet.Application/  # Business logic and services
  ├── CardWallet.Domain/       # Domain entities and enums
  └── CardWallet.Infrastructure/  # Data access and repositories
tests/
  └── CardWallet.Application.Tests/  # Unit and integration tests
```

## Getting Started

### Prerequisites

- .NET 6 or higher
- MySQL 8.0 or higher

### Setup

1. Clone the repository
```bash
git clone https://github.com/namtruong123/card-wallet-platform.git
cd card-wallet-platform
```

2. Configure database connection
   - Update `appsettings.json` in `backend/CardWallet.Api/`
   - Set your MySQL connection string

3. Apply migrations
```bash
cd backend/CardWallet.Api
dotnet ef database update
```

4. Run the application
```bash
dotnet run
```

The API will be available at `https://localhost:5001`

## API Documentation

Swagger UI is available at: `/swagger/index.html`

## Configuration

Key configuration files:
- `appsettings.json` - Default settings
- `appsettings.Development.json` - Development-specific settings

## Database

The application uses MySQL with Entity Framework Core. Migrations are located in:
- `CardWallet.Infrastructure/Migrations/`

## Docker

Build and run with Docker:
```bash
docker-compose up --build
```

## License

[Add your license here]

## Author

Nam Truong - [GitHub Profile](https://github.com/namtruong123)
"# cardwallet" 
