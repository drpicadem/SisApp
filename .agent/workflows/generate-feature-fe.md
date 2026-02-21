---
description: Scaffold a frontend feature (Provider, Screen) following trip-ticket-main patterns
---

# generate-feature-fe Workflow

Generate a frontend feature implementation plan and code following `.agent/rules` and `easy-park` guidelines.

## Prerequisites

- Workspace open.
- `project-rules.md`, `Frontend_Rules.md` exist.

## Instructions

### 1. Context Gathering & Reference Check

- Identify the target Backend Entity/Endpoint.
- Check `Frontend_Rules.md` for Provider patterns.

### 2. Backend Stability Check (CRITICAL)

**Before proposing ANY code:**

- **Verify**: Does the backend endpoint for this feature exist and work?
- **Check**: Does the `Model` matching the backend DTO exist?
- **IF NO**:
  - **STOP IMMEDIATELY**.
  - **Notify User**: "Backend endpoint or model is missing. Please run `/generate-feature-be` first or fix the backend manually."
  - **ABORT WORKFLOW**.

### 3. Generate Frontend Plan

**Create `implementation_plan_fe.md` internal artifact:**

- **Model**: Define Dart model properties matching Backend DTO.
- **Provider**: Define methods (`get`, `insert`, `update`, `delete`).
- **Screens**: Define UI layout and interactions.

**STOP and `notify_user` to review the plan.**
_Only proceed after user approval._

### 4. Execute Implementation

1.  Create `lib/models/[entity].dart` (Must match Backend DTO).
2.  Create `lib/providers/[entity]_provider.dart` (Inherit `BaseProvider`).
3.  Create `lib/screens/[entity]_list_screen.dart` & `_detail_screen.dart`.
4.  Register Provider in `main.dart`.

### 5. Verify

- Run `flutter pub get`.
- Verify application builds.
