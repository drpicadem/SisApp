---
description: Analyze and fix errors across the stack, then update rules to prevent recurrence.
---

# fix-error Workflow

Analyze the error logs, find the root cause across Backend and Frontend, apply a fix following project rules, and update rules to prevent future occurrences.

## Instructions

### 1. Context & Analysis

1.  **Read Error Rules**: Review `.agent/rules/error-rules.md` to understand the expected error handling behavior.
2.  **Analyze Input**: Read the provided error log or description.
3.  **Search Codebase**:
    - Use `grep_search` to find the error message or code path in both:
      - `ŠišApp.API/` and `ŠišApp.Services/` (Backend)
      - `frontend/lib/` (Frontend - assuming standard structure)
    - Identify the full call stack (Controller -> Service -> Database) and the Frontend invocation.

### 2. Diagnosis

- Is this a **User Error** (400) or **System Error** (500)?
- Does the Backend throw `UserException` where appropriate?
- Does the Frontend catch and display the error using `UserFriendlyException` / `SnackBar`?
- **Root Cause**: Determine exactly why the error occurred.

### 3. Propose Fix

**Create `fix_plan.md` internal artifact:**

- **Backend Changes**: Files to modify (e.g., add validation, fix null check, map exception).
- **Frontend Changes**: Files to modify (e.g., improve error catching, fix data sending).
- **Rule Compliance**: Confirm the fix aligns with `error-rules.md`.

**STOP and `notify_user` to review the plan.**
_Only proceed after user approval._

### 4. Apply Fix

1.  Apply Backend fixes.
2.  Apply Frontend fixes.

### 5. Post-Mortem (Rule Update)

**CRITICAL STEP**:

- Ask yourself: "Why did this error happen? usage of specific pattern? Missing rule?"
- **Propose a Rule Update**:
  - Draft a new rule or update an existing one in `.agent/rules/` to prevent this specific class of error.
  - _Example_: "Added rule to `Backend_Rules.md`: Always check for null before accessing `City.Country`."
- **Ask User**:
  - "I have fixed the error. Shall I add this new rule to `.agent/rules/[file].md` to prevent recurrence?":
  - [Insert Rule Proposal]

### 6. Verify

- Run the application (or tests) to confirm the error is gone.