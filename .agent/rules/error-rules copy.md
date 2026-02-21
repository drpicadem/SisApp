# PostGhost Cursor Rules: Error handling

## Principles

- **Exceptions are for Exceptional Circumstances**: Use Exceptions for valid error states, not for flow control (unless using the specific `UserException` pattern).
- **Centralized Handling**: Do NOT catch generic `Exception` in business logic. Let them bubble up to the global `ExceptionFilter`.
- **User vs. System**: Distinguish clearly between errors the user can fix (`UserException`) and errors the system needs to fix (Internal Server Error).

## Behavior-based classification (Domain Meaning)

Instead of relying on implementation details (e.g., `SqlException`), use domain-specific exceptions to trigger specific behaviors in the API layer.

### Backend (.NET)

- **User Error (400 Bad Request)**: Throw `UserException`.
  - _Usage_: Validation failures, domain rule violations (e.g., "Not enough funds").
  - _Handled By_: `ExceptionFilter` catches this and returns 400 with a `userError` field.
- **Not Found (404)**: Throw `UserException` with a specific message like "X not found" OR create a specific `NotFoundException` if strict status code control is needed. (Reference `TripService` uses `UserException("Trip not found")`).
- **System Error (500)**: Any other unhandled exception.
  - _Handled By_: `ExceptionFilter` logs the stack trace and returns 500 with a generic "Server side error" message.

### Frontend (Flutter)

- **User Notification**:
  - If the API returns 400/UserException, display the `userError` message in a **Toast** or **SnackBar**.
  - Do NOT crash the app.
- **System Failure**:
  - If the API returns 500, display a generic "Something went wrong" message.
  - Log the error to a monitoring service (e.g., Sentry, Crashlytics).

## Layer responsibilities

### 1. Database/Repository Layer (EF Core)

- **Responsibility**: Execute queries.
- **Behavior**:
  - If `SingleOrDefault` returns null when it shouldn't, let it return null (handle in Service).
  - If a specific DB constraint is violated, Wrap it in a `UserException` if it's user-correctable (e.g., "Duplicate Email").

### 2. Service Layer (`tripTicket.Services`)

- **Responsibility**: Enforce business rules.
- **Behavior**:
  - **Null Checks**: If an entity is missing, throw `UserException("Entity not found")`. (See `TripService.cs`).
  - **Validation**: If a rule is broken, throw `UserException("Rule broken")`.
  - **Context**: Add context to exceptions where helpful, but prefer simple valid messages for the UI.

### 3. API Layer (`tripTicket.API`)

- **Responsibility**: Map Exceptions to HTTP Responses.
- **Mechanism**: The `ExceptionFilter` matches behaviors to status codes:
  - `UserException` -> **400 Bad Request** + JSON `{ "userError": "..." }`
  - `Exception` (everything else) -> **500 Internal Server Error** + JSON `{ "ERROR": "Server side error..." }`

## Example Pattern (C#)

```csharp
// correct
public Model.Models.Trip GetById(int id)
{
    var entity = _context.Trips.FirstOrDefault(t => t.Id == id);
    if (entity == null)
    {
        // Triggers 400 Bad Request in ExceptionFilter
        throw new UserException("Trip not found");
    }
    return _mapper.Map<Model.Models.Trip>(entity);
}
```

## Example Pattern (Flutter)

```dart
try {
    await tripProvider.getById(123);
} catch (e) {
    // Shows "Trip not found" to the user
    showToast(context, e.toString());
}
```
