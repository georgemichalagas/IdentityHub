---
# IdentityHub

A premium authentication and user management service for modern applications. Built with ASP.NET Core 8, Entity Framework Core, PostgreSQL, JWT, and gRPC. Supports HTTP REST and gRPC APIs, robust security, and seamless integration.

## Features

- **JWT Authentication**: Secure token-based authentication
- **Refresh Tokens**: Automatic token refresh
- **User Management**: Registration, login, profile, roles
- **Password Management**: Reset, change, forgot password
- **Email Confirmation**: Email verification system
- **Role-based Authorization**: Admin/User roles
- **PostgreSQL Database**: Code-first EF Core
- **Swagger & gRPC Docs**: Interactive API documentation
- **Security Best Practices**: Password policies, lockout, CORS
- **Clean Architecture**: Separation of concerns

## Technology Stack

- ASP.NET Core 8
- PostgreSQL + Entity Framework Core
- ASP.NET Core Identity + JWT
- Swagger/OpenAPI & gRPC
- AutoMapper
- Docker support

## Quick Start

1. **Clone the repository**
    ```bash
    git clone https://github.com/yourusername/IdentityHub.git
    cd IdentityHub
    ```
2. **Start PostgreSQL (Docker recommended)**
    ```bash
    docker-compose up -d postgres
    ```
3. **Run the API**
    ```bash
    ./start-dev.sh
    # or for Windows
    ./start-dev.ps1
    ```
4. **Access the API**
    - Swagger UI: `http://localhost:5040`
    - gRPC: `https://localhost:7001`

### Default Admin Account
Email: `admin@identityhub.com`  Password: `Admin123!`

## Environment Configuration

- `appsettings.Development.json` (JWT: 60 min)
- `appsettings.Staging.json` (JWT: 30 min)
- `appsettings.Production.json` (JWT: 15 min)
- `.env.*` files for secrets (never commit real secrets)

**Key Variables:**
`API_PORT`, `DB_HOST`, `DB_NAME`, `DB_USER`, `DB_PASSWORD`, `JWT_SECRET_KEY`, `SMTP_*`

## Deployment

- **Development:**
   ```bash
   ./start-dev.sh     # Linux/macOS
   ./start-dev.ps1    # Windows
   ```
- **Staging:**
   ```bash
   ./deploy-staging.sh
   ./deploy-staging.ps1
   ```
- **Production:**
   ```bash
   ./deploy-production.sh
   ./deploy-production.ps1
   ```

## API Endpoints

### HTTP REST
- `POST /api/auth/register` — Register user
- `POST /api/auth/login` — Login
- `POST /api/auth/refresh-token` — Refresh token
- `POST /api/auth/logout` — Logout
- `POST /api/auth/forgot-password` — Request password reset
- `POST /api/auth/reset-password` — Reset password
- `POST /api/auth/change-password` — Change password
- `GET /api/auth/confirm-email` — Confirm email
- `GET /api/user/profile` — Get profile
- `PUT /api/user/profile` — Update profile
- `GET /api/user/{id}` — Get user by ID (Admin)
- `GET /health` — Health check

### gRPC
- `UserService` — User management
- `AuthService` — Authentication/authorization

#### Example gRPC Methods
```proto
rpc ValidateToken(ValidateTokenRequest) returns (ValidateTokenResponse);
rpc GetUser(GetUserRequest) returns (UserResponse);
rpc RefreshToken(RefreshTokenRequest) returns (RefreshTokenResponse);
```

#### Example gRPC Usage (C#)
```csharp
using Grpc.Net.Client;
var channel = GrpcChannel.ForAddress("https://localhost:7001");
var authClient = new AuthService.AuthServiceClient(channel);
var tokenResponse = await authClient.ValidateTokenAsync(new ValidateTokenRequest { Token = "your-jwt-token" });
```

#### Example gRPC Usage (Python)
```python
import grpc
from grpc_generated import auth_pb2, auth_pb2_grpc
channel = grpc.secure_channel('localhost:7001', grpc.ssl_channel_credentials())
auth_stub = auth_pb2_grpc.AuthServiceStub(channel)
token_response = auth_stub.ValidateToken(auth_pb2.ValidateTokenRequest(token="your-jwt-token"))
```

## Configuration

**JWT:**
```json
{
   "JwtSettings": {
      "SecretKey": "YourSecretKey",
      "Issuer": "IdentityHub",
      "Audience": "IdentityHubClient",
      "ExpiryInMinutes": 15
   }
}
```

**gRPC:**
```json
{
   "GrpcSettings": {
      "Port": 7001,
      "EnableReflection": true
   }
}
```

**CORS:**
```csharp
policy.WithOrigins("http://localhost:3000", "https://yourdomain.com")
```

## Testing

### Integration Testing with Testcontainers
Integration tests use [Testcontainers](https://github.com/testcontainers/testcontainers-dotnet) to spin up real PostgreSQL containers for reliable, isolated testing.

**How it works:**
- Testcontainers starts a PostgreSQL container before tests run
- Test DB is initialized and seeded
- Containers are disposed after tests

**Run all tests:**
```bash
dotnet test
```

## Security & Best Practices

- Password policy, account lockout, token expiration
- Role-based access, email confirmation
- Secure headers, HTTPS enforcement
- Never commit secrets

## Project Structure

```
IdentityHub.Api/
├── Controllers/         # API controllers (HTTP & gRPC)
├── Data/               # Database context
├── DTOs/               # Data transfer objects
├── Mappings/           # AutoMapper profiles
├── Models/             # Entity models
├── Services/           # Business logic services
└── Program.cs          # Application entry point
```

## Docker & CI/CD

- Dockerfile & docker-compose for containerization
- GitHub Actions for CI/CD

## Contributing

1. Fork the repository
2. Create your feature branch (`git checkout -b feature/AmazingFeature`)
3. Commit your changes (`git commit -m 'Add some AmazingFeature'`)
4. Push to the branch (`git push origin feature/AmazingFeature`)
5. Open a Pull Request

## License

MIT License — see [LICENSE](LICENSE)

## Support

Open an issue on GitHub or contact support@identityhub.com

## Roadmap

- [ ] Email service integration
- [ ] Two-factor authentication (2FA)
- [ ] OAuth providers (Google, Facebook, etc.)
- [ ] Rate limiting
- [ ] Audit logging
- [ ] Health checks
- [ ] Docker containerization
- [ ] Kubernetes deployment manifests
- [ ] Admin dashboard
- [ ] User analytics
# IdentityHub

A comprehensive authentication service built with ASP.NET Core 8, Entity Framework Core, PostgreSQL, and JWT tokens. This service provides a robust foundation for authentication and user management that can be used by multiple applications.

## Features

- 🔐 **JWT Authentication** - Secure token-based authentication
- 🔄 **Refresh Tokens** - Automatic token refresh mechanism
- 👤 **User Management** - Complete user registration, login, and profile management
- 🔑 **Password Management** - Password reset, change password functionality
- 📧 **Email Confirmation** - Email verification system
- 🛡️ **Role-based Authorization** - Admin and User roles
- 📊 **PostgreSQL Database** - Code-first approach with Entity Framework Core
- 📖 **Swagger Documentation** - Interactive API documentation
- 🔒 **Security Best Practices** - Password policies, account lockout, CORS configuration
- 🏗️ **Clean Architecture** - Well-structured project with separation of concerns

## Technology Stack

- **Backend**: ASP.NET Core 8
- **Database**: PostgreSQL with Entity Framework Core
- **Authentication**: ASP.NET Core Identity + JWT
- **Documentation**: Swagger/OpenAPI
- **Mapping**: AutoMapper
- **Containerization**: Docker support

## Getting Started

### Prerequisites

- .NET 8 SDK
- Docker (recommended for PostgreSQL)
- Git

### Quick Start

1. **Clone the repository**
   ```bash
   git clone https://github.com/yourusername/IdentityHub.git
   cd IdentityHub
   ```

2. **Start PostgreSQL using Docker**
   ```bash
   docker-compose up -d postgres
   ```

3. **Run the application**
   ```bash
   ./start-dev.sh
   # or for Windows
   ./start-dev.ps1
   ```

4. **Access the API**
   - Swagger UI: `http://localhost:5040` 
   - API Base URL: `http://localhost:5040/api`

### Default Admin Account

In development mode, a default admin account is created:
- **Email**: admin@identityhub.com
- **Password**: Admin123!

## Environment Configuration

The application supports multiple environments with dedicated configurations:

### Environment Files
- `appsettings.Development.json` - Development settings (JWT: 60 min expiry)
- `appsettings.Staging.json` - Staging settings (JWT: 30 min expiry)  
- `appsettings.Production.json` - Production settings (JWT: 15 min expiry)

### Environment Variables
Each environment can use `.env` files for sensitive configuration:
- `.env.development` - Optional for development (uses defaults)
- `.env.staging` - Required for staging deployment
- `.env.production` - Required for production deployment

**Key Environment Variables:**
- `API_PORT` - Port for the API service (5040 for dev, 7080 for staging, 8080 for production)
- `DB_HOST`, `DB_NAME`, `DB_USER`, `DB_PASSWORD` - Database connection settings
- `JWT_SECRET_KEY` - Secret key for JWT token signing
- `SMTP_*` - Email service configuration

**Setup Environment Variables:**
```bash
# Copy templates and update with your values
cp .env.staging .env.staging.local
cp .env.production .env.production.local

# Edit the .local files with your actual values, then rename:
mv .env.staging.local .env.staging
mv .env.production.local .env.production
```

⚠️ **Never commit actual `.env` files to version control**

## Deployment

### Development
Use the included development scripts for local development:
```bash
./start-dev.sh     # Linux/macOS
./start-dev.ps1    # Windows PowerShell
```

### Staging Deployment
```bash
./deploy-staging.sh     # Linux/macOS
./deploy-staging.ps1    # Windows PowerShell
```
- API runs on: `http://localhost:7080`
- Includes health checks and automated database migrations

### Production Deployment  
```bash
./deploy-production.sh  # Linux/macOS
./deploy-production.ps1 # Windows PowerShell
```
- API runs on: `http://localhost:8080`
- Includes backup creation, health checks, and rollback options
- Enhanced security settings and SSL requirements

2. **Update connection string**
   
   Update the connection string in `appsettings.json` and `appsettings.Development.json`:
   ```json
   {
     "ConnectionStrings": {
       "DefaultConnection": "Host=localhost;Database=identityhub;Username=your_username;Password=your_password"
     }
   }
   ```

3. **Install dependencies**
   ```bash
   cd IdentityHub.Api
   dotnet restore
   ```

4. **Run the application**
   ```bash
   dotnet run
   ```

5. **Access the API**
   - Swagger UI: `https://localhost:7266` (or your configured port)
   - API Base URL: `https://localhost:7266/api`

### Default Admin Account

In development mode, a default admin account is created:
- **Email**: admin@identityhub.com
- **Password**: Admin123!

## API Endpoints

### Authentication
- `POST /api/auth/register` - Register a new user
- `POST /api/auth/login` - Login user
- `POST /api/auth/refresh-token` - Refresh access token
- `POST /api/auth/logout` - Logout user
- `POST /api/auth/forgot-password` - Request password reset
- `POST /api/auth/reset-password` - Reset password
- `POST /api/auth/change-password` - Change password
- `GET /api/auth/confirm-email` - Confirm email address

### User Management
- `GET /api/user/profile` - Get current user profile
- `PUT /api/user/profile` - Update current user profile
- `GET /api/user/{id}` - Get user by ID (Admin only)

### System
- `GET /health` - Health check endpoint

## Configuration

### JWT Settings
```json
{
  "JwtSettings": {
    "SecretKey": "YourSecretKey",
    "Issuer": "IdentityHub",
    "Audience": "IdentityHubClient",
    "ExpiryInMinutes": 15
  }
}
```

### Database Configuration
The application uses PostgreSQL with Entity Framework Core. The database is automatically created and seeded when the application starts.

### CORS Configuration
Configure allowed origins in `Program.cs`:
```csharp
policy.WithOrigins("http://localhost:3000", "https://yourdomain.com")
```

## Security Features

- **Password Policy**: Configurable password requirements
- **Account Lockout**: Protection against brute force attacks
- **Token Expiration**: Short-lived access tokens with refresh mechanism
- **Role-based Access**: Admin and User roles with different permissions
- **Email Confirmation**: Optional email verification
- **Secure Headers**: HTTPS enforcement and security headers

## Development

### Project Structure
```
IdentityHub.Api/
├── Controllers/         # API controllers
├── Data/               # Database context
├── DTOs/               # Data transfer objects
├── Mappings/           # AutoMapper profiles
├── Models/             # Entity models
├── Services/           # Business logic services
└── Program.cs          # Application entry point
```

### Database Migrations

To create and apply migrations:

```bash
# Add migration
dotnet ef migrations add InitialCreate

# Update database
dotnet ef database update
```

### Running Tests

### Integration Testing with Testcontainers

Integration tests use [Testcontainers](https://github.com/testcontainers/testcontainers-dotnet) to spin up real PostgreSQL containers for reliable, isolated testing. This ensures your tests run against a real database instance, mirroring production as closely as possible.

**How it works:**
- Testcontainers automatically starts a PostgreSQL container before tests run.
- The test database is initialized and seeded as needed.
- Containers are disposed after tests complete, ensuring a clean environment.

**Integration test files:**
- `IdentityHub.Tests/Controllers/AuthControllerIntegrationTests.cs`
- `IdentityHub.Tests/Controllers/UserControllerIntegrationTests.cs`
- `IdentityHub.Tests/Data/ApplicationDbContextIntegrationTests.cs`
- `IdentityHub.Tests/Grpc/GrpcAuthServiceIntegrationTests.cs`
- `IdentityHub.Tests/Grpc/GrpcUserServiceIntegrationTests.cs`
- `IdentityHub.Tests/Infrastructure/TestContainerManager.cs` (Testcontainers setup)

**Run all tests (unit + integration):**
```bash
dotnet test
```

**Testcontainers will automatically manage the lifecycle of the PostgreSQL container during test execution.**

## Docker Support

A Dockerfile and docker-compose configuration will be added to support containerized deployment.

## Contributing

1. Fork the repository
2. Create your feature branch (`git checkout -b feature/AmazingFeature`)
3. Commit your changes (`git commit -m 'Add some AmazingFeature'`)
4. Push to the branch (`git push origin feature/AmazingFeature`)
5. Open a Pull Request

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## Support

For support and questions, please open an issue on GitHub or contact support@identityhub.com.

## Roadmap

- [ ] Email service integration
- [ ] Two-factor authentication (2FA)
- [ ] OAuth providers (Google, Facebook, etc.)
- [ ] Rate limiting
- [ ] Audit logging
- [ ] Health checks
- [ ] Docker containerization
- [ ] Kubernetes deployment manifests
- [ ] Admin dashboard
- [ ] User analytics
