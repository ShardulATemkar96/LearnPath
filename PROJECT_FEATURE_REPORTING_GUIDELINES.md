# Project Feature Reporting Guidelines

## Purpose

This document defines the standard for generating technical documentation
for the major features of this project.

Every feature report must explain not only WHAT the feature does, but also
HOW it works internally based on the actual source code.

The report is intended for:
- Project documentation
- Developer understanding
- Project presentation
- Viva preparation
- Technical interviews

---

# 1. Source of Truth

Always inspect the actual project source code before generating a report.

The source code is the primary source of truth.

Rules:

- Do not guess implementation details.
- Do not assume a feature works in a particular way.
- Do not describe functionality that does not exist.
- Do not invent APIs, algorithms, database operations, or security rules.
- If something is partially implemented, clearly state that.
- If something is missing, clearly state that.
- Mention the actual implementation used by the project.

Do not modify any source files while generating the report.

This is a READ-ONLY documentation task.

---

# 2. Level of Detail

The report should be detailed enough to understand the feature internally,
but should NOT explain every line of code.

Focus on:

- Main functionality
- Important workflows
- Business logic
- Algorithms
- Calculations
- Data flow
- Database operations
- Validation
- Authorization
- State changes
- Important frontend/backend interaction
- Special mechanisms

Avoid:

- Explaining trivial getters/setters
- Explaining obvious imports
- Explaining every small helper function
- Copying large sections of source code
- Documenting unrelated features

The goal is to explain the architecture and important implementation logic.

---

# 3. Standard Feature Report Structure

Every major feature report MUST follow this structure.

## 1. Feature Name

State the name of the feature.

## 2. Purpose

Explain:

- What problem the feature solves.
- Why the feature exists.
- What role it plays in the overall application.

## 3. What the User Can Do

Explain the major actions available to:

- Student
- Instructor
- Admin

Only mention roles that actually have access to the feature.

## 4. Feature Workflow

Explain the complete flow:

User Action
→ Frontend
→ API Request
→ Controller
→ Service
→ Repository / EF Core
→ Database
→ Response
→ Frontend Update

Explain the important steps in simple technical English.

## 5. How It Works Internally

This is the MOST IMPORTANT section.

Explain the actual internal mechanisms used by the feature.

Depending on the feature, cover relevant mechanisms such as:

- Algorithms
- Randomization
- Selection logic
- Filtering
- Sorting
- Ranking
- Calculations
- Scoring
- Validation
- State transitions
- Data transformation
- Business rules
- Authorization
- Authentication
- Error handling
- Persistence
- Caching
- Pagination
- File handling
- Token handling
- AI processing
- Graph processing
- Any other feature-specific mechanism

For every important mechanism explain:

1. What happens?
2. How does it happen?
3. Where does it happen?
4. Which important class/method/file performs it?

### Example

Do NOT write:

> Questions are randomized.

Instead explain:

- Where the questions are retrieved.
- How eligible questions are selected.
- Whether the question set, order, or both are randomized.
- What actual randomization mechanism is used.
- Which method performs the randomization.
- Which file contains the implementation.
- What happens after randomization.

Only describe what is confirmed by the actual source code.

---

# 6. Frontend Implementation

Explain the important frontend architecture.

Cover:

- Pages
- Components
- Hooks
- API/service layer
- Redux/state management
- Important UI logic

Use this table:

| Layer | File Path | Responsibility |
|---|---|---|
| Page | `full/path` | Main feature page |
| Component | `full/path` | Feature UI |
| Service | `full/path` | API communication |
| State | `full/path` | State management |

Only include important files.

---

# 7. Backend Implementation

Explain the important backend architecture.

Cover:

- Controllers
- DTOs
- Services
- Repositories
- Validators
- Important business logic

Use:

| Layer | File Path | Responsibility |
|---|---|---|
| Controller | `full/path` | API endpoints |
| DTO | `full/path` | Request/response data |
| Service | `full/path` | Business logic |
| Repository | `full/path` | Data access |
| Validator | `full/path` | Validation |

Only include important files.

---

# 8. Database Implementation

Explain:

- Main entities/tables involved.
- Important fields.
- Primary keys.
- Foreign keys.
- Relationships.
- Important database changes.
- What data is created, updated, or deleted.

Use:

| Entity/Table | Purpose | Important Relationship |
|---|---|---|
| `TableName` | Purpose | Relationship |

Do not document unrelated tables.

---

# 9. Security & Authorization

Explain the actual security implementation.

Cover where applicable:

- Authentication
- JWT
- Role-based authorization
- Ownership checks
- Admin permissions
- Instructor permissions
- Student permissions
- Resource access restrictions
- Validation against unauthorized operations

Only document mechanisms actually present in the code.

---

# 10. Important Business Rules

List the important rules controlling the feature.

Examples:

- Who can perform an action.
- When an action is allowed.
- Validation requirements.
- Limits.
- Score calculation.
- Passing criteria.
- Ownership rules.
- Status transitions.
- Attempt restrictions.
- Moderation rules.

Do not invent rules.

---

# 11. Example of Internal Execution

Provide ONE realistic end-to-end example.

Use:

User Action
→ Frontend
→ API
→ Controller
→ Service
→ Database
→ Response
→ UI

Explain what actually happens internally at each important step.

The example must use the actual implementation.

---

# 12. Files Involved

List only the important files required to understand the feature.

Use full relative paths.

### Frontend

- `frontend/src/...`

### Backend

- `backend/...`

### Database

- `backend/Migrations/...`

Do not list every indirectly related file.

---

# 13. Functionality

At the END of the report, provide a concise list of the functionality
currently provided by the feature.

Example:

- Create quiz
- Configure quiz
- Select questions
- Randomize questions
- Attempt quiz
- Evaluate answers
- Calculate score
- Determine pass/fail
- Review answers
- Track attempts

Only list functionality actually implemented.

---

# 4. Feature-Specific Internal Logic

The report must adapt to the feature.

Do not mechanically explain the same concepts for every feature.

For example:

### Quiz Engine

Explain:

- Question-bank selection
- Question selection
- Question randomization
- Option ordering if implemented
- Attempt creation
- Answer submission
- Answer validation
- Score calculation
- Passing percentage
- Attempt tracking
- Result generation
- Review generation

### Learning Path

Explain:

- Module ordering
- Module dependencies
- Graph/DAG logic
- Dependency validation
- Progress tracking
- Completion logic
- Prerequisite checking

### Community

Explain:

- Group membership
- Post creation
- Voting
- Score calculation
- Sorting
- Trending calculation
- Comment nesting
- Moderation
- Pinning
- Reporting

### Authentication

Explain:

- Login flow
- Password validation
- JWT generation
- Refresh-token handling
- Authorization
- Role handling
- Token expiration
- Logout behavior

These are examples only.

Always inspect the source code and document the implementation actually present.

---

# 5. Accuracy Rules

If the implementation differs from the expected design:

Document the ACTUAL implementation.

Do not silently "correct" it in the report.

If something appears incomplete:

> Status: Partially implemented.

If something is missing:

> Status: Not implemented.

If implementation cannot be confirmed:

> Implementation could not be confirmed from the available source code.

---

# 6. Writing Style

Use:

- Simple English
- Short paragraphs
- Clear headings
- Tables where useful
- Bullet points
- Technical terms when necessary
- Actual class/method names when useful

Avoid:

- Marketing language
- Excessive theoretical explanations
- Unnecessary repetition
- Huge blocks of source code
- Generic textbook explanations

The report should sound like documentation of a real implemented software
system.

---

# 7. Code References

When explaining an important mechanism, mention:

- Class name
- Method name
- File path

Example:

> Question selection is performed inside `QuizService.StartAttemptAsync()`
> in `backend/Services/Quiz/QuizService.cs`.

Do not copy the complete method unless a very small code fragment is
necessary to explain a complex mechanism.

---

# 8. No Unnecessary Work

Do NOT:

- Modify source code.
- Refactor code.
- Fix bugs.
- Create new features.
- Generate diagrams unless explicitly requested.
- Generate tests.
- Create unrelated documentation.
- Analyze unrelated modules.
- Produce unnecessary reports.

Only analyze the requested feature and the source code directly required
to understand it.

---

# 9. Final Quality Check

Before producing the report, verify:

- Is every major claim supported by the source code?
- Are algorithms explained correctly?
- Are calculations explained correctly?
- Are randomization mechanisms identified correctly?
- Are API flows accurate?
- Are database relationships accurate?
- Are permissions accurate?
- Are file paths correct?
- Are missing features clearly identified?
- Is unnecessary code-level detail avoided?
- Is the Functionality section at the end?