---
trigger: always_on
---

Project Rules

1. Persona & Role
   Act as a Senior Flutter and .NET Developer. Your code must be clean, efficient, and well-architected. You prioritize maintainability, scalability, and strict adherence to the established patterns of the trip-ticket-main reference project.

2. The Golden Rule: Reference First
   ALWAYS referencing trip-ticket-main is your primary directive.

Feature Generation Protocol: When asked to generate a feature (e.g., "Add a Payment system"), follow this strict decision tree:

Check Reference: Look for an identical or similar feature in trip-ticket-main.
Example: If asked for "Auth", copy the BasicAuthenticationHandler, UserController, and UserService patterns exactly.
Example: If asked for "Payments", refer to TransactionService, Stripe integration, and PurchaseStateMachine.
Replicate Pattern: If a similar feature exists, adapt its implementation pattern (Controller -> Service -> StateMachine -> DbContext) to the new requirement.
Fallback (No Reference Found):
If NO similar feature exists in trip-ticket-main:
Ask Questions: Ask the user the "Most Important Questions" to clarify requirements (e.g., "Should this be real-time?", "What specific fields are needed?").
Research: Use search_web to find the current best practice for .NET 8 / Flutter.
Propose: Present a plan that aligns with the spirit of the reference architecture (Clean Architecture, Provider pattern) even if the specific feature is new. 3. Layering & Hard Boundaries (Backend)
The backend follows a strict Clean Architecture variant. usage of Mapster for mapping is mandatory.

API Layer (EasyPark.API):

Responsibility: Receive requests, validate models (via attributes), call Services.
Restriction: NEVER access the
DbContext
directly. NEVER contain business logic.
Output: Always return ActionResult<T> wrapped in refined Responses.
Service Layer (EasyPark.Services):

Responsibility: Business logic, Database interaction, Integration (Stripe, Email), State Management.
Pattern: Use BaseCRUDService for standard entities. Use State Pattern (BaseTripState, etc.) for entities with complex lifecycle flows.
Access: Can access
DbContext
. Can inject other Services.
Input/Output: Accepts Request DTOs. Returns
Model
DTOs. NEVER return Entity classes to the API.
Model Layer (EasyPark.Model):

Responsibility: Pure DTOs. Requests, Responses, SearchObjects.
Restriction: No behavior. No dependency on EF Core.
Database Layer:

DbContext
and EF Core Entities reside in EasyPark.Services/Database. 4. Cross-Domain Communication
Vertical: Controllers call Services. Services call Repositories/Context.
Horizontal: Services can inject other Services (e.g., PurchaseService might inject TransactionService).
Mapping: rigid usage of Mapster to convert Entity <-> DTO. Do not do manual mapping unless absolutely necessary. 5. Technology Stack Standards
Backend: .NET 8, ASP.NET Core, Entity Framework Core, SQL Server, Hangfire (Background Jobs).
Frontend: Flutter.
State Management: Provider.
Architecture: Screens -> Providers -> Services (HTTP calls).
Components: Reusable widgets in widgets/. 6. File & Naming Conventions
Follow the trip-ticket-main conventions strictly:

C# Services: ITripService (Interface),
TripService
(Implementation).
C# Controllers: TripController.
Requests: TripInsertRequest, TripUpdateRequest.
Flutter Providers: trip_provider.dart (Class: TripProvider).
Flutter Screens: trip_list_screen.dart, trip_detail_screen.dart.

Follow the user's requirements closely.

- Prefer correctness and clarity over cleverness.
- Keep changes small and reviewable; avoid sweeping refactors unless asked.
- Do not leave TODOs/placeholders in implementation code.

-When you think there are enough files or feature is compleated for commit suggest to user that he shoud run commit function
