# Backend Development Rules

AI must follow these rules strictly.

---

## 1. Controller Rules

Controllers MUST:

- Be thin
- Call services only
- Return standardized response

Controllers MUST NOT:

- Access DbContext
- Contain business logic
- Execute queries

---

## 2. Service Rules

Services:

- Contain business logic
- Handle validation
- Manage transactions

Services call repositories only.

---

## 3. Repository Rules

Repositories:

- Handle Dapper queries
- Return entities or projections
- Async operations required

Always use:

async / await

---

## 4. DTO Rules

DTO separation mandatory:

RequestDTO
ResponseDTO

Never expose Entity directly.

---

## 5. API Response Standard

All responses must follow:


---

## 6. Dependency Injection

Register services via extension methods.

Do not manually instantiate services.

---

## 7. Database Rules

- Use EF Core Fluent API
- Avoid raw SQL unless necessary
- Pagination required for list endpoints

---

## 8. Naming Convention

Controller:
BrandController

Service:
BrandService

Repository:
BrandRepository

DTO:
CreateBrandRequest
BrandResponse
