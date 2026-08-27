# LearnPath — OpenCode Project Rules

## 1. Project Scope

The only project/workspace to work on is:

`F:\Final Project\LearnPath`

All scanning, investigation, coding, testing, building, debugging, and file changes must remain inside this directory.

Do not scan, modify, create, delete, or refactor anything outside this project directory.

Before starting work, verify that the project root is:

`F:\Final Project\LearnPath`

---

## 2. Follow the Current Task

The user's current prompt defines the actual work to be performed.

Follow the requested task exactly.

Do not add unrelated features, improvements, refactoring, documentation, architecture changes, or cleanup unless they are required for the requested task.

---

## 3. Reuse Existing Functionality First

Before creating anything new:

1. Scan the existing implementation.
2. Find existing services, controllers, APIs, entities, repositories, components, hooks, Redux state, validation, authorization, utilities, and other relevant functionality.
3. Understand how the existing functionality works.
4. Reuse it whenever possible.

Do not create duplicate functionality when an existing implementation can be extended.

For example, if the project already has:

- progress/completion logic
- quiz completion logic
- authorization
- classroom services
- assignment services
- notification/toast system
- API error handling
- state management
- locking/access validation
- existing UI components

extend or reuse those mechanisms instead of creating parallel systems.

Only introduce a new mechanism when the existing architecture genuinely cannot support the requirement.

---

## 4. Do Not Invent Unnecessary Architecture

Do not introduce:

- unnecessary new services
- duplicate APIs
- duplicate database tables
- duplicate state management
- unnecessary libraries
- unnecessary dependencies
- unnecessary design patterns
- unnecessary abstractions
- unnecessary refactoring

The goal is to solve the requested problem using the project's existing architecture and conventions.

---

## 5. Minimal-Change Rule

Make the smallest correct changes required to implement the requested feature or fix.

Do not modify unrelated files simply because they could be improved.

Do not rewrite working functionality.

Preserve existing:

- authentication
- authorization
- database behavior
- API conventions
- frontend architecture
- state management
- validation
- error handling
- business rules
- UI patterns

unless the current task specifically requires a change.

---

## 6. Understand Before Changing

Do not immediately start editing code.

First inspect the relevant implementation and understand:

- where the functionality currently exists
- how data flows
- how frontend and backend communicate
- what existing business rules are used
- what state is stored
- what APIs are called
- what database entities are involved

Then determine the smallest appropriate change.

Do not guess when the existing code can provide the answer.

---

## 7. Existing Project Is the Source of Truth

Always verify behavior against the actual LearnPath codebase.

Do not assume that a feature exists merely because it is mentioned in the task.

Do not assume that a feature does not exist merely because it is not obvious.

Search the project first.

When implementing a change, follow the patterns already established in the project.

---

## 8. Debugging / Development Loop

For bugs and behavioral changes, use an iterative loop:

```text
Inspect
↓
Understand existing implementation
↓
Reproduce the current behavior
↓
Trace the actual flow
↓
Identify the root cause
↓
Implement the minimal fix
↓
Build / test
↓
Reproduce the original workflow again
↓
Verify the expected behavior
↓
If still failing → investigate again
↓
Repeat until verified

Do not stop simply because the project compiles.

Do not declare success because an error message temporarily disappears.

The original user workflow must be tested again.

9. Root Cause Over Symptom Fixes

When fixing a bug, fix the actual cause.

Do not merely:

hide an error message
suppress an exception
catch and ignore an exception
hardcode a value
reload the page
reset all state blindly
bypass validation
disable a feature
return fake data
add a temporary workaround

unless the existing architecture specifically requires it.

10. Backend and Frontend Validation

When a feature involves access restrictions or business rules, do not rely only on frontend behavior.

If appropriate to the existing architecture, verify the rule at the backend as well so users cannot bypass it through:

direct URLs
manually changing route parameters
direct API calls
manipulated frontend state

Reuse existing authorization and validation mechanisms whenever possible.

11. UI Changes

When the task requires UI changes:

preserve the existing application's design language
reuse existing components where possible
make the UI clear and practical
ensure important information is readable
avoid cramped layouts
avoid unnecessary redesign
make locked/disabled states visually understandable when required
ensure error messages are meaningful to the user

Do not introduce a completely different UI style unless explicitly requested.

12. Data and Business Rules

Do not invent business rules.

Use the rules explicitly defined by the current task and the rules already implemented in the project.

If an existing feature already determines completion, eligibility, status, or access, reuse that logic.

Do not create another independent definition of the same business rule.

13. Testing

After making changes:

Build the affected project(s).
Run relevant existing tests.
Test the actual user workflow related to the change.
Verify that existing functionality still works.
If a test fails, investigate and continue the loop.

Do not modify tests merely to make them pass unless the test itself is genuinely outdated because of the requested behavior change.

Do not skip verification just because the change appears small.

14. No Unnecessary Work

Do only the work required by the current task.

Do not add:

extra features
unrelated bug fixes
unnecessary refactoring
large documentation
unnecessary reports
unnecessary comments
unnecessary cleanup
unrelated UI improvements

If something is outside the requested scope, leave it alone.

15. Documentation / Reports

Do not automatically create a report after every task.

Unless explicitly requested, after completing the work provide only a short summary containing:

what was fixed/implemented
verification performed
any remaining issue

Detailed documentation or reports should only be created when explicitly requested.

16. Existing Files and Configuration

Before creating a new file:

Search for an existing file that already serves the same purpose.
Check whether the functionality can be added to an existing file.
Create a new file only when it is genuinely appropriate.

Do not create duplicate files with similar responsibilities.

17. Dependencies

Do not add a new package/library unless:

it is genuinely required
the existing project cannot reasonably provide the functionality
the new dependency fits the existing architecture

Prefer existing project dependencies and native framework capabilities.

18. Database Changes

Do not modify the database schema unless the requested functionality actually requires it.

Before creating a new entity/table/column:

inspect the existing model
inspect existing relationships
check whether the required data already exists
reuse existing fields where appropriate

Do not create duplicate data structures.

If a schema change is genuinely required, follow the project's existing Entity Framework Core migration conventions.

19. API Changes

Before creating a new endpoint:

search for an existing endpoint providing the same functionality
inspect existing controller/service patterns
reuse existing DTOs where appropriate
follow existing API naming and response conventions

Do not create duplicate endpoints unnecessarily.

20. Frontend State

Before creating new state:

check existing Redux slices
selectors
hooks
component state
existing API/query state
route parameters

Reuse existing state management where appropriate.

Do not introduce another state mechanism for data that already has an established source of truth.

21. Error Handling

Use the project's existing error-handling mechanism.

Do not introduce a separate error-handling system for an individual feature unless genuinely required.

Errors should be fixed at their source rather than hidden from the user.

22. Security

Do not weaken existing security mechanisms to make a feature work.

Preserve existing:

authentication
authorization
JWT handling
role checks
validation
access restrictions
backend enforcement

Never solve a frontend access problem by removing backend protection.

23. Scope Control During Exploration

While scanning the project, you may inspect files necessary to understand the requested feature.

However, exploration does not mean modifying unrelated areas.

Read broadly when necessary to understand dependencies.

Modify narrowly.

24. Completion Rule

A task is complete only when:

The requested functionality has been implemented.
Existing functionality required by the task still works.
Relevant tests/builds succeed.
The actual user workflow has been verified.
No unnecessary unrelated changes were introduced.

If verification fails, continue the debugging/development loop.

Do not claim completion prematurely.

25. Priority Order

When deciding how to implement something, follow this priority:

Existing LearnPath functionality
Existing project architecture and conventions
Existing framework capabilities
Minimal extension of existing functionality
New implementation only when genuinely necessary

Always prefer the simplest correct solution that fits the existing project.