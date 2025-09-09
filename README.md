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
