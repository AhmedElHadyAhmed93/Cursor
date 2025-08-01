# Clean Architecture .NET 8 + Angular 18 Solution

A production-ready, full-stack application built with .NET 8 Web API and Angular 18, following Clean Architecture principles. This solution demonstrates enterprise-level patterns, security, real-time communication, and comprehensive integrations.

## 🚀 Features

### Backend (.NET 8)
- **Clean Architecture** with Domain, Application, Infrastructure, and API layers
- **JWT Authentication** with refresh tokens and role-based authorization
- **Entity Framework Core** with SQL Server, migrations, and audit interceptor
- **SignalR** for real-time communication with Redis backplane
- **MongoDB** integration for logging and audit trails
- **Firebase Admin SDK** for push notifications
- **Hangfire** for background jobs and recurring tasks
- **AutoMapper** for object mapping with feature-based profiles
- **FluentValidation** for request validation
- **Serilog** structured logging to console, file, and MongoDB
- **Swagger/OpenAPI** with JWT authentication support
- **Rate limiting** and CORS configuration
- **Global exception handling** with ProblemDetails
- **Health checks** for all dependencies
- **Docker** containerization with multi-stage builds

### Frontend (Angular 18)
- **Standalone components** architecture
- **JWT authentication** with automatic token refresh
- **Role-based access control** (RBAC) with dynamic menus
- **SignalR client** for real-time updates
- **Firebase SDK** for push notifications
- **Angular Material** UI components
- **Route guards** and interceptors
- **Responsive design** with modern UX patterns

### Infrastructure & DevOps
- **Docker Compose** for local development
- **SQL Server** for primary data storage
- **Redis Stack** for SignalR scaling and caching
- **MongoDB** for logs and audit trails
- **Nginx** reverse proxy configuration
- **Environment-based configuration**
- **Health monitoring** and observability

## 🏗️ Architecture

```
backend/
├── src/
│   ├── Core/                 # Domain entities and interfaces
│   ├── Application/          # Use cases, DTOs, validators, mappings
│   ├── Infrastructure/       # Data access, external services, audit
│   ├── Identity/             # Authentication, authorization, JWT
│   ├── Api/                  # Controllers, middleware, configuration
│   └── Shared/               # Common DTOs and utilities
├── tests/
│   ├── UnitTests/
│   └── IntegrationTests/
└── docker/

frontend/
└── admin/
    └── src/app/
        ├── features/         # Feature modules (auth, users, cars, etc.)
        ├── core/             # Guards, interceptors, services
        └── shared/           # Reusable components
```

## 🚦 Getting Started

### Prerequisites
- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [Node.js 18+](https://nodejs.org/)
- [Docker & Docker Compose](https://docs.docker.com/get-docker/)
- [Angular CLI](https://angular.io/cli): `npm install -g @angular/cli`

### Quick Start with Docker

1. **Clone the repository**
   ```bash
   git clone <repository-url>
   cd clean-architecture-solution
   ```

2. **Copy environment configuration**
   ```bash
   cp .env.example .env
   # Edit .env with your specific configuration
   ```

3. **Start the entire stack**
   ```bash
   # Production mode
   docker-compose up -d

   # Development mode with hot reload
   docker-compose -f docker-compose.yml -f docker-compose.dev.yml up -d
   ```

4. **Access the applications**
   - **API**: http://localhost:5000
   - **Swagger UI**: http://localhost:5000/swagger
   - **Angular App**: http://localhost:4200
   - **Hangfire Dashboard**: http://localhost:5000/hangfire
   - **Redis Insight**: http://localhost:8001
   - **MongoDB Express**: http://localhost:8081 (dev mode)

### Manual Setup (Development)

1. **Database Setup**
   ```bash
   # Start dependencies
   docker-compose up sqlserver redis mongodb -d
   ```

2. **Backend Setup**
   ```bash
   cd backend
   dotnet restore
   dotnet ef database update --project src/Infrastructure --startup-project src/Api
   dotnet ef database update --project src/Identity --startup-project src/Api
   dotnet run --project src/Api
   ```

3. **Frontend Setup**
   ```bash
   cd frontend/admin
   npm install
   ng serve
   ```

## 🔐 Authentication & Authorization

### Default Credentials
- **Email**: `admin@example.com`
- **Password**: `Admin@123456`
- **Roles**: SuperAdmin, Admin

### JWT Configuration
The solution uses JWT tokens with refresh token rotation:
- **Access Token**: 60 minutes (configurable)
- **Refresh Token**: 7 days (configurable)
- **Automatic renewal** on API calls
- **Secure logout** with token revocation

### Role-Based Access Control
- **SuperAdmin**: Full system access
- **Admin**: User and car management
- **User**: Limited access to own resources

## 📊 API Endpoints

### Authentication
- `POST /api/auth/register` - User registration
- `POST /api/auth/login` - User login
- `POST /api/auth/refresh` - Token refresh
- `POST /api/auth/logout` - User logout
- `GET /api/auth/me` - Get current user profile

### Cars Management
- `GET /api/cars` - Get paginated cars
- `GET /api/cars/{id}` - Get car by ID
- `GET /api/cars/{id}/details` - Get car with owners
- `POST /api/cars` - Create new car (Admin)
- `PUT /api/cars/{id}` - Update car (Admin)
- `DELETE /api/cars/{id}` - Delete car (Admin)
- `POST /api/cars/{id}/owners/{userId}` - Assign owner (Admin)
- `DELETE /api/cars/{id}/owners/{userId}` - Unassign owner (Admin)

### Notifications
- `POST /api/notifications/test` - Send test notification
- `POST /api/notifications/broadcast` - Broadcast to all users (Admin)
- `POST /api/firebase/token` - Update Firebase token

### System
- `GET /health` - Health check endpoint

## 🔄 Real-time Features

### SignalR Hub (`/hubs/socket`)
- **Connection tracking** with Redis
- **User-to-user messaging**
- **Group messaging**
- **Car update notifications**
- **Admin broadcasts**

### Events
- `ReceiveMessage` - Chat messages
- `CarUpdated` - Car CRUD notifications
- `AdminBroadcast` - System announcements
- `UserJoined`/`UserLeft` - Group management

## 🗄️ Database Schema

### SQL Server (Primary Data)
- **AspNetUsers** - User accounts with Firebase tokens
- **AspNetRoles** - User roles (SuperAdmin, Admin, User)
- **Cars** - Vehicle information
- **OwnerCars** - Many-to-many user-car relationships
- **RefreshTokens** - JWT refresh token storage

### MongoDB (Logs & Audit)
- **app_logs** - Application logs from Serilog
- **audit_logs** - Entity change tracking

### Redis (Cache & SignalR)
- **signalr:user:{userId}** - User connection tracking
- **General caching** for performance optimization

## 🔧 Configuration

### Environment Variables
See `.env.example` for all available configuration options.

### Key Settings
- **JWT__Key**: Secret key for JWT signing (256-bit minimum)
- **ConnectionStrings__Default**: SQL Server connection
- **ConnectionStrings__Redis**: Redis connection
- **ConnectionStrings__MongoDB**: MongoDB connection
- **Firebase__ProjectId**: Firebase project ID
- **SEED__ADMIN__EMAIL**: Default admin email
- **SEED__ADMIN__PASSWORD**: Default admin password

## 🔍 Monitoring & Observability

### Health Checks
The `/health` endpoint provides comprehensive health information:
- SQL Server connectivity
- Redis connectivity  
- MongoDB connectivity
- Application version and environment

### Logging
Structured logging with Serilog to multiple sinks:
- **Console** for development
- **File** with daily rolling
- **MongoDB** for centralized logging

### Audit Trail
Automatic tracking of all entity changes:
- **Create/Update/Delete** operations
- **User attribution** and timestamps
- **Before/after values** for updates
- **Configurable per entity** via AuditMode enum

## 🚀 Background Jobs

### Hangfire Recurring Jobs
- **Monthly Notifications**: Send updates to all users (1st of month)
- **Token Cleanup**: Remove expired refresh tokens (daily at 2 AM)
- **Monthly Reports**: Generate usage statistics (1st of month at 1 AM)

### Dashboard
Access Hangfire dashboard at `/hangfire` (Admin authentication required)

## 🧪 Testing

### Backend Tests
```bash
cd backend
dotnet test
```

### Frontend Tests
```bash
cd frontend/admin
npm test
ng e2e
```

## 📦 Deployment

### Docker Production
```bash
# Build and deploy
docker-compose -f docker-compose.yml up -d

# Scale services
docker-compose up -d --scale api=3
```

### Environment-Specific Deployments
- **Development**: `docker-compose.dev.yml`
- **Staging**: `docker-compose.staging.yml`
- **Production**: `docker-compose.yml`

## 🔒 Security Considerations

### Authentication & Authorization
- JWT tokens with secure signing
- Refresh token rotation
- Role-based access control
- Password strength requirements
- Account lockout policies

### API Security
- Rate limiting per endpoint
- CORS configuration
- HTTPS enforcement
- Request validation
- Global exception handling

### Data Protection
- SQL injection prevention (EF Core)
- XSS protection (Angular)
- Audit logging
- Soft delete for sensitive data

## 🤝 Contributing

1. Fork the repository
2. Create a feature branch (`git checkout -b feature/amazing-feature`)
3. Commit your changes (`git commit -m 'Add amazing feature'`)
4. Push to the branch (`git push origin feature/amazing-feature`)
5. Open a Pull Request

## 📄 License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## 🙏 Acknowledgments

- Clean Architecture by Robert C. Martin
- .NET Community for excellent documentation
- Angular team for the robust framework
- All open-source contributors

## 📞 Support

For support and questions:
- Create an issue in the repository
- Check the documentation
- Review the health check endpoint for system status

---

**Built with ❤️ using Clean Architecture principles**