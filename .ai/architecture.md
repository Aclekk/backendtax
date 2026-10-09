# Backend Architecture

## Layer Structure

API Layer
↓
Service Layer
↓
Repository Layer
↓
Database

---

## Folder Structure

src/

Modules/
 ├── Brand/
 │    ├── Controllers
 │    ├── Services
 │    ├── Repositories
 │    ├── DTOs
 │    └── Entities

Shared/
 ├── Database
 ├── Middleware
 ├── Extensions
 └── Common

---

## Responsibilities

### Controller
- Handle HTTP request
- Return response
- No business logic

### Service
- Business logic
- Validation
- Transaction coordination

### Repository
- Database access
- EF Core queries only

---

## Dependency Flow

Controller → Service → Repository

Reverse dependency is forbidden.