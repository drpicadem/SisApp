---
description: Standardize, simplify, and refactor code to follow project rules.
---

# refactor-code Workflow

Refactor a specific file, folder, or module to adhere to `.agent/rules`, simplify logic, and standardize naming.

## Instructions

### 1. Analysis

1.  **Identify Target**:
    - User specifies a file, folder, or concept.
    - _Action_: List all relevant files using `ls` or `find_by_name`.
2.  **Check Rules**:
    - Review `project-rules.md` (General).
    - Review `Backend_Rules.md` or `Frontend_Rules.md` depending on the target.
    - Review `API_Contract_Rules.md` if DTOs are involved.
3.  **Audit Code**:
    - Read the code.
    - Identify:
      - **Rule Violations**: Naming (PascalCase vs camelCase), Layering (Service logic in Controller?).
      - **Complexity**: Long methods (> 50 lines), deep nesting, duplicate logic.
      - **Simplification Ops**: Can multiple functions be combined? Can a large function be split?

### 2. Proposal

**Create `refactor_plan.md` internal artifact:**

- **Current State**: Briefly describe the issues (e.g., "Controller contains DB logic", "Method `process` is 200 lines").
- **Proposed Changes**:
  - **Rename**: `oldName` -> `NewName`.
  - **Extract**: Move logic from X to Y.
  - **Simplify**: Refactor loop/condition.
- **Verification**: How to ensure nothing broke (Run build, existing tests).

**STOP and `notify_user` to review the plan.**
_Only proceed after user approval._

### 3. Execution

1.  **Apply Renames**: Use IDE tools or careful find/replace.
2.  **Extract Methods**: Move logic to private headers or new Service methods.
3.  **Standardize**: Apply naming conventions from rules.
4.  **Cleanup**: Remove unused imports, comments, or dead code.

### 4. Verify

- **Build Check**: Run `.NET build` or `flutter pub get` to ensure no syntax errors.
- **Rule Check**: Verify against strict rules one last time.
