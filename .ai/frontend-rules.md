# Frontend Integration Awareness

This backend serves performance-view-frontend.

Frontend stack:

- Next.js App Router
- TypeScript
- TanStack Query
- Axios

---

## API Design Rules

Endpoints must:

- Be RESTful
- Predictable naming
- Pagination support
- Consistent response shape

Example:

GET /api/brand
POST /api/brand
PUT /api/brand/{id}
DELETE /api/brand/{id}

---

## Compatibility

Do not break existing API contracts.