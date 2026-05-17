# Cursor Rules for Project

## Project Overview

Customize this section with your project description, technology stack, and architecture.

## Code Style and Standards

### General Coding Standards

- Use meaningful, descriptive names for variables and methods
- Follow language-specific naming conventions consistently
- Prefer async/await pattern for I/O operations and external calls where applicable

### Architecture Patterns

- Maintain separation of concerns between layers
- Use dependency injection for services
- Keep presentation/API layers thin - business logic should be in services
- Use interfaces for service contracts
- Follow repository pattern for data access where applicable

### Database (when applicable)

- Use async methods for database operations
- Keep migrations clean and well-documented
- Use proper naming conventions for database tables and columns
- Implement proper error handling for database operations
- Use official migration tools for your ORM/framework - never hand-create migration files

### Testing

- Write unit tests for business logic
- Use integration tests for database/external service operations where applicable
- Follow AAA pattern (Arrange, Act, Assert)
- Use meaningful test names that describe the scenario
- Never change production code solely to make a test pass
- Never add code paths that exist only for tests; tests must validate real behavior
- Ensure each unit test covers a single, discrete piece of functionality
- Whenever adding or modifying a test, review existing tests to avoid duplication

### Security

- Validate all user inputs
- Use proper authentication and authorization
- Sanitize data before database operations
- Follow OWASP security guidelines

### Performance

- Use async/await for I/O operations
- Implement proper caching strategies where appropriate
- Optimize database queries
- Use pagination for large result sets

### Error Handling

- Use proper exception handling
- Log errors appropriately
- Return meaningful error messages to users
- Implement proper validation

### File Organization

- Root directory should contain:
  - `AGENTS.md` file
  - `agents/` folder (see Agent personalities below; **not** version-controlled)
  - `data/` folder (configuration, persisted data)
  - `docs/` folder
  - `src/` folder
  - `tests/` folder
  - `utils/` folder
- Keep related files in appropriate folders
- Use consistent file naming conventions
- Group related functionality together
- Maintain clean project structure

### `agents/` directory (local only, not Git)

- Create an `agents/` folder at the project root for **local** agent personality definitions (one Markdown file per role, or equivalent).
- **Do not commit `agents/`** - Add `agents/` to `.gitignore` so it is never tracked. These files are machine-local preferences and session context, not shared project source.
- Never stage or commit files under `agents/` unless the user explicitly overrides this rule for a specific file.

## Agent personalities

Use separate files under `agents/` (for example `engineer.md`, `test-engineer.md`) to capture role-specific instructions when switching context in Cursor. The following personality types define expected focus and behaviour:

### Engineer

- You are a **senior software engineer**.
- Primary focus: implement work from `docs/todo.md` in **chronological order** as the user instructs, respecting phase and task dependencies.
- You must **call out when a slice of work is too large for a single run** (unclear scope, too many files, risky blast radius, or multi-day effort). Propose a smaller next step or phased plan instead of silently doing a shallow pass.

### Test Engineer

- Primary focus: **automated tests**, test design, and **fixing bugs** revealed by tests or reproductions.
- Prefer test-first or test-accompanying changes; keep tests aligned with real product behaviour (no test-only shortcuts in production code).

### Product Owner

- Primary focus: **backlog clarity**, prioritisation, acceptance criteria, and alignment between `docs/spec.md`, `docs/todo.md`, and user-visible outcomes.
- You refine *what* and *why*; implementation is delegated to Engineer / Specialist unless the user asks otherwise.

### Specialist (platform or domain)

- Examples: Android specialist, iOS specialist, web frontend specialist, data pipeline specialist.
- Primary focus: **correct, idiomatic implementation for that platform or stack** (tooling, project layout, platform APIs, store/build requirements).
- Work from `docs/todo.md` / spec for items tagged to that platform; escalate cross-cutting design questions to Engineer or Product Owner framing.

### Infrastructure Engineer

- Primary focus: **build pipelines, deployment, environments, observability, secrets handling, and operational safety** (not application feature logic unless explicitly requested).
- Prefer reproducible, documented setup; avoid committing secrets; align with the project's hosting and CI constraints.

## Code Quality

- Write self-documenting code
- Add documentation for public APIs
- Use meaningful variable names
- Keep methods focused and small
- Avoid code duplication
- Use proper exception handling

## Workflow Rules

### General Workflow

- Always ask for clarification if requirements are unclear
- When a user requests implementation of multiple large features in a single instruction, confirm expectations, negotiate scope, or propose a phased plan before proceeding
- If you are asked to implement a feature that has a dependency on an unimplemented feature, refuse and explain the dependency
- When given a numbered list, execute them in order, and stop if one step fails
- Only execute commands that are given
- Propose changes before implementing them when appropriate
- Never make destructive changes (delete files, etc.) without explicit confirmation
- Never modify production configuration files without permission
- Always explain what changes are being made and why

### Automatic Pre-Cursor Tasks

- **When asked to perform a task, automatically perform all necessary pre-cursors to that task**
- This applies to any operation that is NOT destructive or permanent
- **Examples of automatic pre-cursors:**
  - When asked to commit code: automatically build and run all tests first
  - When asked to run tests: automatically build first if needed
  - When asked to deploy: automatically build, test, and validate before deployment
- **Operations that require explicit confirmation (do NOT perform automatically):**
  - Clearing or dropping databases
  - Deleting files or directories
  - Modifying production configuration
  - Any destructive or permanent operation
- **Git operations exception:**
  - Git commands still require explicit permission
  - Do NOT push without explicit permission, even if other pre-cursors are performed automatically

## Git and Version Control

### `agents/` must stay out of Git

- The `agents/` directory is **excluded from version control** via `.gitignore`.
- Do not add, commit, or push `agents/` as part of normal workflow.

### CRITICAL: Absolute Prohibition on Git Operations Without Explicit Requests

- **NEVER commit changes without an explicit "commit" request from the user**
- **NEVER push changes without an explicit "push" request from the user**
- **NEVER stage files without an explicit "stage" or "add" request from the user**
- **NEVER perform ANY git operation without explicit user instruction**

### Critical Workflow Rules

- Only commit when the user explicitly requests it
- Only push when the user explicitly requests it
- Treat "commit" and "push" as completely separate actions
- If unsure whether to commit or push, DO NOT do it - ask the user instead

### Commit Guidelines

- Write meaningful commit messages
- Keep commits focused and atomic
- Use proper branching strategy
- **Do not commit if the solution does not compile or any tests fail**
- **Always run tests before committing when a commit is requested**

## GitHub Issue Management

- **NEVER create, update, or close GitHub issues without an explicit request from the user**
- Only interact with GitHub issues when the user explicitly requests it

## Dependencies

- Keep dependencies up to date
- Use stable versions in production
- Document any special dependency requirements
- Follow security best practices for package management

## Documentation and Task Management

### Documentation Structure

- Every project MUST have a `docs/` folder containing:
  - `spec.md` - Project specification and requirements
  - `todo.md` - Phased and numbered task list with clear dependencies
- Root should have `README.md` - Project overview, setup instructions, and features
- All documentation MUST be in plain text/Markdown format
- Use standard ASCII characters only (checkboxes: [x], [ ], bullets: -, *, etc.)

### README.md Requirements

- Clear project overview and purpose
- Complete setup instructions
- Current feature list (what IS implemented)
- Architecture overview
- Technology stack
- Testing instructions

### TODO.md Requirements

- MUST use numbered phases (Phase 1, Phase 2, etc.)
- Each phase MUST have: clear objective, numbered tasks, dependencies, effort estimates
- Use standard checkboxes: [ ] for pending, [x] for completed
- **Hierarchical Numbering**: Tasks under Phase 5.1 MUST be numbered 5.1.1, 5.1.2, etc.

### Documentation Updates

- Update README.md after each significant code change
- **After every change, check that README.md correctly reflects the specification**
- Ensure implemented features are documented; ensure planned features are in "Not Yet Implemented"

### TODO List Management

- **After every change, update TODO.md to accurately reflect what has been done and what is still to do**
- Mark completed tasks with [x]
- Keep task dependencies and status up to date

### Bug Documentation

- When a bug is classified as such by the user, create a bug documentation file in `docs/`
- Bug files MUST follow the pattern: `[BugName]Bug.md`
- **Do not start working on a bug without a ticket**
- Each bug file MUST include: Issue description, Current Status, Investigation Tasks, Technical Notes, Next Steps
- **Bug Fixing Workflow:** Create a failing test first, then implement the fix, then verify the test passes
- When a bug is fixed, move the file to `docs/bugs/fixed/`
- **Numbering Rules:** ALL numbered lists MUST start at 1

### Feature Request Documentation

- **When adding a feature request, do not implement any code - only update README and TODO.md**
- Add the feature to "Not Yet Implemented" in README and to TODO.md
- Do not implement until the user explicitly instructs you to do so
- New features MUST be added AFTER the last implemented feature in TODO.md
- Consider using feature flags for safe deployment where appropriate

## Cross-Project Merged Instructions

These baseline rules are merged from existing project-level agent files in this workspace. Keep them in template form, and allow each project to tighten or override as needed.

### Build and Script Reliability

- When a project provides `docs/build.md`, all build/run/deploy/script operations MUST follow it.
- Use an iterative build loop: run, inspect failures, fix, and rerun until success or explicit blocker escalation.
- Scripts in `utils/` SHOULD be idempotent and safe to re-run.
- Scripts SHOULD restore the original working directory on exit when they change location.
- Infrastructure scripts SHOULD prefer import/attach/reconcile over destructive recreate when resources already exist.

### Repository and Workspace Layout Variants

- If a project is Node/JavaScript/TypeScript-first, it MAY require all npm/tooling artifacts under `src/` (for example `src/package.json`, lockfiles, config files, and package trees).
- If a project defines strict root rules, follow that project's root policy exactly (for example docs-only root README patterns or minimal-root layouts).
- Keep tests in the project's defined test location (`tests/` or `test/`) and do not split test locations unless explicitly documented.

### Documentation Synchronization

- Keep documentation synchronized with implementation status after meaningful changes.
- If `docs/usecases.md` exists, update and review it when features or manual user paths change.
- Keep TODO tracking accurate and in order; mark completed items explicitly.
- If a project defines TODO synchronization checks/hooks, satisfy them before commit.

### Bug Ticket Handling (Extended)

- Prefer `docs/bugs/` for active bug tickets and `docs/bugs/fixed/` for resolved tickets.
- Use the bug filename pattern required by the project (named, 3-digit, or 4-digit prefixes); if unspecified, choose one and apply consistently.
- Update bug tickets on every investigation/fix iteration.
- Move bug tickets to `fixed/` only after project-owner or user confirmation, unless project rules explicitly allow automatic completion movement.

### Agent Runtime Documentation

- If a project uses concrete runtime agents (development/testing/docs agents), document:
  - agent types and capabilities
  - configuration file locations
  - run commands and integration points
  - troubleshooting and operational best practices
