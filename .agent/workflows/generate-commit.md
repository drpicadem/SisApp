---
description: Create a conventional commit for staged changes
---

# generate-commit Workflow

Create a conventional commit for staged changes and push to the remote repository.

## Instructions

1.  **Check Staged Files**:
    - Run `git status` to see what files are staged for commit.
    - _Constraint_: If no files are staged, **STOP** and ask the user to stage files first.

2.  **Analyze Changes**:
    - Run `git diff --cached` to understand the specific code changes.

3.  **Generate Commit Message**:
    - Format: `<type>: <subject>` (e.g., `feat: add user service`)
    - **Types**:
      - `feat`: New features
      - `fix`: Bug fixes
      - `docs`: Documentation
      - `style`: Formatting
      - `refactor`: Code restructuring
      - `test`: Adding/updating tests
      - `chore`: Maintenance, dependencies
    - **Rules**:
      - Concise subject (max 72 chars).
      - Imperative mood ("add" not "added").
      - Include body if complex.

4.  **Propose to User**:
    - Show the analyzed changes (summary) and the proposed commit message.
    - Ask: "Do you want me to commit and push with this message?"

5.  **Execute**:
    - _On Approval_: Run `git commit -m "<message>"` and `git push`.
