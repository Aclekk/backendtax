# Tax-refund-api - AI Memory

Performance View is an enterprise KPI monitoring system
used for fashion sales performance analysis.

This repository represents the Backend API service.

---

## Technology Stack

- .NET 8 Web API
- Entity Framework Core 8
- PostgreSQL
- Clean modular architecture
- REST API
- Dapper

---

## Architectural Principles

- Feature-based modular structure
- Controller → Service → Repository pattern
- DTO separation required
- Database access only through repositories
- Business logic belongs to services

---

## Core Rules

- Minimal modification principle
- Reuse existing implementation
- Maintain backward compatibility
- Production-ready code only

All AI assistants must follow rules defined in `.ai/*`
before generating code.