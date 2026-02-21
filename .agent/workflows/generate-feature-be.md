---
description: Scaffold a backend feature (Entity, API, Service) following trip-ticket-main patterns
---

# generate-feature-be Workflow

Generate a backend feature implementation plan and code following `.agent/rules` and `easy-park` guidelines.

## Prerequisites

- Workspace open.
- `project-rules.md`, `Backend_Rules.md`, `API_Contract_Rules.md` exist.

## Instructions

### 1. Context Gathering & Reference Check

**Check Reference First (Rule #2):**

- Is there a similar feature in `trip-ticket-main`?
- If YES: Note the pattern to replicate.
- If NO: Ask the user the **Most Important Questions** (fields? relations?) if not provided in prompt.

### 2. Architecture Review

**Read:**

- `project-rules.md`
- `Backend_Rules.md`
- `API_Contract_Rules.md`

### 3. Generate Backend Plan

**Create `implementation_plan_be.md` internal artifact:**

- **Database**: List new Entities and properties.
- **API**: Define `InsertRequest`, `UpdateRequest`, `SearchObject`.
- **Service**: Define Interface and Implementation.
- **Controller**: Define Endpoints.

**STOP and `notify_user` to review the plan.**
_Only proceed after user approval._

### 4. Execute Implementation

1.  Create `Model/Requests/*` (DTOs).
2.  Create `Services/Database/[Entity].cs` (Entity).
3.  Create `Services/Interfaces/I[Entity]Service.cs`.
4.  Create `Services/Services/[Entity]Service.cs` (Implement `BaseCRUDService`).
5.  Create `API/Controllers/[Entity]Controller.cs` (Inherit `BaseCRUDController`).
6.  Register Service in `Program.cs`.

### 5. Verify

- Run `.NET build` to ensure no compilation errors.
