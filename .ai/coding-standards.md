# Coding Standards

AI generated code must be production ready.

---

## General Rules

- Clean readable code
- Async methods preferred
- Proper exception handling
- Logging supported

---

## Error Handling

Use global exception middleware.

Avoid try/catch duplication.

---

## Performance

- Avoid unnecessary queries
- Use projection when possible
- Prevent N+1 queries

---

## Security

- Validate input
- Never expose internal exception
- Sanitize request data

---

## Modification Rule

AI must:

- Modify minimal files
- Preserve architecture
- Avoid refactoring unrelated code