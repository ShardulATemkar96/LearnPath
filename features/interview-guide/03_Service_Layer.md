# 03 – Service Layer

> Verified against `backend/Services/Quiz/QuizService.cs`, `backend/Services/Attempt/AttemptService.cs`, `backend/Services/QuestionBank/QuestionBankService.cs`, `backend/Services/Validation/JsonValidationService.cs`, `backend/Services/Classroom/ClassroomService.cs`, `backend/Services/Auth/AuthService.cs`, and their interfaces under `backend/Interfaces/Services/`. All services are registered in `backend/Program.cs` (lines 208–226).

---

## 1. What Is It?

A service layer is the middle layer between the API controllers and the database.

In LearnPath:

```
Controller (HTTP + auth attributes)
   → Service (business logic, rules, validation, orchestration)
   → EF Core / DbContext (SQL) → Database
```

Controllers stay thin — they only read the request, call a service, and wrap the result in `ApiResponse<T>`. The **real business rules live in services** (classes like `QuizService`, `QuestionBankService`, `ClassroomService`, `AuthService`).

Each service is registered as an **interface + implementation** (DI):
```csharp
builder.Services.AddScoped<IQuizService, QuizService>();
builder.Services.AddScoped<IAuthService, AuthService>();
...
```

---

## 2. Why Is It Used?

- **Separation of concerns** — business logic is not mixed with HTTP concerns.
- **Reuse** — for example `AuditLogService` and `ProgressService` are reused by many other services.
- **Testability** — logic can be tested without HTTP.
- **Consistency** — every action enforces the same rules (e.g. quiz creation always checks the question bank, ownership is always checked inside the service).
- **Security** — services carry ownership/role checks, so a controller can never skip them.

---

## 3. How Are the Services Used in LearnPath?

The most important services (all under `backend/Services/`):

| Service | File | Purpose |
|---|---|---|
| `QuizService` | `Services/Quiz/QuizService.cs` | quiz CRUD, publish/archive, link to modules |
| `AttemptService` | `Services/Attempt/AttemptService.cs` | start attempt, save answers, submit, score, review |
| `QuestionBankService` | `Services/QuestionBank/QuestionBankService.cs` | JSON upload, versioning, archive, delete |
| `JsonValidationService` | `Services/Validation/JsonValidationService.cs` | validates uploaded question-bank JSON |
| `ClassroomService` | `Services/Classroom/ClassroomService.cs` | classrooms, memberships, assignments, submissions |
| `AuthService` | `Services/Auth/AuthService.cs` | register, login, refresh, logout |

Below are the per-service details.

---

## 4. How It Works Internally + 5. Important Internal Logic

### 4.1 Quiz Service — `QuizService`

**Creation — `CreateAsync(dto, userId)`**
1. Checks the question bank exists (`QuestionBanks`).
2. Validates `QuestionCount > 0`.
3. Validates `QuestionCount <= bank.QuestionCount` (can't ask more questions than the bank has).
4. If a `DifficultyFilter` is set, counts matching questions and rejects if fewer than requested.
5. Checks the quiz title is unique.
6. Creates a `Quiz` with `Status = Draft`.
7. Logs `QUIZ_CREATED` audit.
8. Returns it through `GetByIdAsync`.

**Update — `UpdateAsync`**
Same validations, plus title uniqueness excluding itself. Logs `QUIZ_UPDATED` with `oldValue`/`newValue`.

**Publishing — `PublishAsync` / `UnpublishAsync` / `ArchiveAsync`**
- `PublishAsync` → `Status = Published`, sets `PublishedAt`; cannot publish an *archived* quiz.
- `UnpublishAsync` → only from `Published` back to `Draft`.
- `ArchiveAsync` → `Status = Archived`.

**Delete — `DeleteAsync`**
Loads `ModuleQuizzes`; if the quiz is assigned to any module, it is REFUSED ("Unlink it first") — because deleting it would break the module quiz assignment.

**Link quiz to module — `LinkToModuleAsync(moduleId, quizId, userId)`**
- Module must exist; user must own the path (or be admin).
- Quiz must be owned by user and be **published**.
- Upserts the `ModuleQuiz` link (replaces the existing link if any), records `QUIZ_LINKED_TO_MODULE`.
- `UnlinkFromModuleAsync(moduleId)` removes it.

**Ownership rule — `GetOwnedQuizAsync`**
Admins can act on any quiz; everyone else only on quizzes they created (`quiz.CreatedById != userId → UnauthorizedAccessException`).
`IsAdminAsync` is a manual join of `UserRoles` + `Roles` looking for role name "Admin".

### 4.2 Attempt Service — `AttemptService` (execution of a quiz)

**Start — `StartAttemptAsync(quizId, moduleId, userId)`**
1. Quiz must be `Published`.
2. There must be an active `ModuleQuiz` link for that module+quiz.
3. Loads existing attempts (descending by `AttemptNumber`).
4. If an attempt is still `InProgress`/`Created` → resume it (do not create a new one).
5. Enforces `MaximumAttempts`: if the best previous attempt already `Passed == true` → "You have already passed this quiz."; otherwise if count reached → "Maximum attempts reached."
6. Creates a `QuizAttempt`:
   - `AttemptNumber = existingCount + 1`
   - `RandomSeed = Random.Shared.Next()` ← **the randomization seed**
   - `Status = InProgress`
7. Logs `QUIZ_STARTED`. Marks the module `Status = QuizAttempted`.
8. Builds the attempt response (questions + shuffled options).

**Question selection + randomization — `GetSelectedQuestionsAsync(quiz, seed)`**
- Query questions in the bank (entity includes `Options`), apply `DifficultyFilter` if set.
- Order by `Id` for determinism.
- If `SelectionMode == Random` → **shuffle the whole list with the attempt's seed, then take `QuestionCount`** (both selection AND ordering are randomized).
- Otherwise → just take the first `QuestionCount` in id order.
- `ShuffleList<T>` is a **Fisher–Yates** shuffle: `var rng = new Random(seed);` then swap from the end to the start. Because the seed is per-attempt, every attempt gets a different (reproducible) order.
- Options are shuffled per question with `seed + questionId`, so option order differs too but is stable for scoring.

**Saving answers — `SaveAnswerAsync(attemptId, dto, userId)`**
- Attempt must be owned and `InProgress`/`Created`.
- Upserts the `StudentAnswer` for `(QuizAttemptId, QuestionId)`.

**Submit + scoring — `SubmitAttemptAsync(attemptId, userId)`**
- Re-selects the exact same questions using the stored `RandomSeed` (this is why correctness is possible even though options were shuffled).
- For each question: find the saved answer, get `Options.First(o => o.IsCorrect)`, compare ids → count correct answers.
- `TimeSpentSeconds = (DateTime.UtcNow - StartedAt).TotalSeconds`.
- Writes:
  - `Status = Evaluated`
  - `Score = correctCount`
  - `Percentage = round(correct/total*100, 2)`
  - `Passed = Percentage >= quiz.PassingPercentage`
- If passed → module `Status = Completed` + `ProgressService.MarkModuleCompleteFromQuizAsync`.
- Audits `QUIZ_COMPLETED` and then `QUIZ_PASSED` or `QUIZ_FAILED`.

**Result & review — `GetReviewAsync(attemptId, userId)`**
- Needs `attempt.Status >= Submitted`.
- Rebuilds questions from the seed; for each: your selected option, the correct option, `Explanation`, and `IsCorrect`.
- Options are displayed shuffled with `seed + questionId`, with `DisplayOrder`.

### 4.3 Question Bank Service — `QuestionBankService`

**Upload — `UploadAsync(userId, fileName, fileStream)`**
1. Reads the file into a string.
2. Calls `JsonValidationService.Validate(fileName, fileSize, jsonContent)`.
3. If errors → returns a failed result ("No data was saved") with the error list.
4. Deserializes into `QuestionBankUploadModel` (case-insensitive).
5. **Versioning:** looks for an existing bank with same `Title` + `Subject`; if found, `version = existing.Version + 1`, otherwise `1`.
6. Saves the bank row including **`StoredJson` = the raw JSON** (so the source file is kept).
7. `QuestionCount = model.Questions.Count`, `Status = Draft`.
8. `ParseQuestions(model, bank)` converts JSON questions into `Question` + `Option` entities.
9. Logs `QUESTION_BANK_UPLOADED` (or `..._VERSION_UPLOADED` for new versions).

**ParseQuestions (data transformation)**
- `Difficulty` string is mapped: `easy/medium/hard` → enum, default `Easy`.
- For each option, `IsCorrect = option.Trim().Equals(correctAnswer.Trim(), OrdinalIgnoreCase)` — so the "correct answer field" is matched by text against the options, and the matching option is flagged.
- `DisplayOrder = i` preserves file order.

**Other operations**
- `SearchAsync(title, subject, tag)` — filters with `Contains`.
- `GetStoredJsonAsync(id)` — used by "download" endpoint.
- `UploadVersionAsync(id, ...)` — creates a NEW row with `Version + 1` (history preserved), keeps title/subject/tags.
- `ArchiveAsync` / `RestoreAsync` — status flip `Active ↔ Archived`.
- `DeleteAsync` — **refused** if any quiz uses the bank ("Archive or remove them first").

### 4.4 JSON Validation — `JsonValidationService`

**Called from:** `QuestionBankService.UploadAsync` and `UploadVersionAsync` (`_validator.Validate(fileName, fileSize, jsonContent)`).

**Validation steps (in order):**
1. **Extension** — must be `.json`, else fail immediately.
2. **Size** — must be ≤ 5 MB (`MaxFileSize = 5 * 1024 * 1024`).
3. **JSON parsing** — deserialize into `QuestionBankUploadModel`; a `JsonException` becomes "Invalid JSON format."
4. **Null check** — parsed model must not be null ("empty or could not be parsed").
5. **Required fields** — `Title`, `Subject`, and a non-empty `Questions` array.
6. **Per-question checks**:
   - question text present,
   - at least 2 options,
   - `CorrectAnswer` present,
   - `CorrectAnswer` must match one of the options (case-insensitive),
   - `Difficulty` must be `easy/medium/hard`,
   - `Explanation` present,
   - **no duplicate questions** (case-insensitive `HashSet<string>`).

**When invalid JSON is received:** the validation returns a list of `ValidationError { Reason, Detail, QuestionIndex }`. The service does NOT save anything — the controller turns it into a 400 with readable messages like `"Question 3: Correct answer does not match any option."`

### 4.5 Classroom Service — `ClassroomService`

**Create — `CreateAsync(dto, userId)`**
- Learning path must exist.
- Creates `Classroom` with `InviteCode = Guid.NewGuid().ToString("N")[..8].ToUpper()` (first 8 hex chars, uppercased).
- **Auto-joins the creator as `Instructor`** in `UserClassrooms`.
- Logs `CLASSROOM_CREATED`.

**Join — `JoinAsync(inviteCode, userId)`**
- Finds classroom by invite code; rejects if the user is already a member; adds membership with `Role = "Student"`.
- Logs `CLASSROOM_JOINED`.

**Memberships & roles**
- `GetByIdAsync` requires membership, else unauthorized.
- `LeaveAsync`: an **Instructor cannot leave** ("Transfer ownership first.").
- `RemoveMemberAsync`: instructor-only; cannot remove another instructor.
- `MarkInvalidAsync`: instructor-only; only **students** can be marked invalid; the Super Admin can never be marked invalid; requires a reason; sets `User.Status = Invalid` + `InvalidReason`; logs `USER_MARKED_INVALID` (old=Active, new=Invalid).

**Permissions helpers**
- `GetOwnedClassroomAsync(id, userId)` — creator-only for update/delete.
- `EnsureInstructorAsync` — membership role must be `Instructor` (used for assignment update/delete, grading, etc.).
- `EnsureAdminAsync` — must be a member AND have the `Admin` role (used for assignment creation).

**Assignments**
- Created here with `Title`, `Description`, `DueDate`; CRUD + audit.

**Submissions (also here)**
- `UploadSubmissionAsync`: file validated by `FileValidationService`, stored by `FileStorageService`; **`IsLate = assignment.DueDate < DateTime.UtcNow`**. Cannot replace a submission that is `UnderReview/Reviewed/Graded`. Status becomes `Submitted`, or `SubmittedAgain` if it was `ReturnedForResubmission`.
- `DeleteSubmissionAsync`: only allowed when `Submitted`/`SubmittedAgain` ("Can only cancel a submitted submission.").
- `TransitionStatusAsync`: only to `UnderReview` (from Submitted/SubmittedAgain) or `Reviewed` (from UnderReview) — strict state machine.
- `GradeSubmissionAsync`: instructor; sets grade+feedback, status `Graded`.
- `CompleteSubmissionAsync`: requires a grade; then `Grade >= 5 ? Graded : ReturnedForResubmission` (5 is the pass mark).
- `PublishEvaluationAsync`: only from `Graded`; releases results + AI feedback to the student.

### 4.6 Authentication Service — `AuthService`

**Register — `RegisterAsync(dto)`**
- Rejects if email already registered.
- Maps DTO → `User`, `_userManager.CreateAsync(user, dto.Password)` (Identity hashes the password).
- On failure, surfaces Identity's errors.
- Adds the `Student` role automatically.
- Logs `USER_CREATED`.
- Returns an auth response (access + refresh token).

**Login — `LoginAsync(dto)`**
- Find by email; unknown email → `LOGIN_FAILED` audit + generic "Invalid credentials."
- `CheckPasswordAsync` — wrong password → `LOGIN_FAILED` audit.
- **Account status gating:** `Deleted`, `Inactive`, `Invalid` are all rejected with specific messages (each audited).
- Gets roles, logs `LOGIN`, returns tokens.

**Refresh — `RefreshTokenAsync(dto)`**
- Finds a non-revoked, non-expired refresh token.
- **Rotates:** marks the used token `IsRevoked = true`, then issues a fresh pair.
- Blocks deleted/inactive/invalid users.
- Returns `BuildAuthResponseAsync(user)`.

**Logout — `RevokeTokenAsync(userId)`**
- Revokes all of the user's refresh tokens; logs `LOGOUT`.

**Password validation**
- Delegated to ASP.NET Identity policy (configured in `backend/Program.cs`: digit, lowercase, uppercase, min length 8).

---

## 6. Frontend Side (per service flow)

### Quiz flow
```
QuizInstructionsPage → quizService.startAttempt(quizId, moduleId)
   → GET /quiz/:attemptId (QuizAttemptPage) → quizService.getAttempt
   → saveAnswer on each selection → quizService.saveAnswer (PUT)
   → countdown timer (setInterval, 1s) in QuizAttemptPage → auto-submit at 0
   → quizService.submitAttempt → result state → navigate to /quiz/result/:attemptId
   → QuizReviewPage → quizService.getReview → review with explanations
```

### Question bank / JSON upload flow
```
AdminQuestionBanksPage → questionBankService.upload(file) — FormData, Content-Type undefined
   → POST /questionbanks/upload → JsonValidationService.Validate
   → on success → bank card added; on 400 → error list rendered per question
   → search/download/version/archive/restore from questionBankService
```

### Classroom flow
```
ClassroomPage → classroomService.getMy
ClassroomDetailPage → getById (members + assignments)
Join → classroomService.join(inviteCode)
Create → classroomService.create
AssignmentDetailPage → uploadSubmission / getMySubmission / verify / grade / complete / publish
```

### Auth flow
```
LoginPage/RegisterPage → authSlice (loginThunk/registerThunk) → authService
   → apiClient stores tokens in localStorage (tokenUtils)
   → 401 → interceptor auto-refresh → retry
   → logoutThunk → authService.logout() → redirect /login
```

---

## 7. Backend Side

- **Controllers** (thin): `QuizController`, `AttemptController`, `QuestionBankController`, `ClassroomController`, `AuthController` under `backend/Controllers/`.
- **DTOs:** requests/responses under `backend/DTOs/` (e.g. `CreateQuizDto`, `SubmitResponseDto`, `QuestionBankUploadModel`).
- **Interfaces:** `backend/Interfaces/Services/` (e.g. `IQuizService`, `IAttemptService`, `IClassroomService`, `IAuthService`, `IValidationService`).
- **Validation:** FluentValidation validators auto-registered via `AddValidatorsFromAssemblyContaining<Program>()`; manual business checks happen inside each service.
- **DB access:** services use `ApplicationDbContext` (EF Core) directly — there is no extra repository layer for these (only `ICommunityRepository` is registered, for community).

---

## 8. Database Side (per service)

- **Quiz:** `Quizzes` (title, bank id, question count, difficulty filter, selection mode, time limit, passing %, max attempts, status) + `ModuleQuizzes` (module↔quiz link).
- **Attempts:** `QuizAttempts` (seed, attempt number, score, percentage, passed, status, times) + `StudentAnswers` (attempt, question, selected option).
- **Question bank:** `QuestionBanks` (title, subject, version, stored json, status) → `Questions` → `Options`.
- **Classroom:** `Classrooms` (invite code) + `UserClassrooms` (user, role, joined at) + `Assignments` + `Submissions` (+ uploaded files on disk under `/uploads`).
- **Auth:** `Users`, `Roles`, `UserRoles` (Identity) + `RefreshTokens`.

---

## 9. Security / Authorization

- `[Authorize]` / `[Authorize(Roles = "...")]` on controllers plus **service-level ownership checks**:
  - Quiz: owner-or-admin (`GetOwnedQuizAsync`).
  - Attempts: only the owner can view/save/submit their own attempt (`attempt.UserId != userId` → unauthorized).
  - Question bank upload: `[Authorize(Roles = "Admin")]`; search/get for `Admin,Instructor`.
  - Classroom: membership required; instructor/admin-gated actions via `EnsureInstructorAsync` / `EnsureAdminAsync`.
  - Auth endpoints: `[AllowAnonymous]` on register/login/refresh; `[Authorize]` on revoke; account status blocks login.
- Claims come from the JWT (`ClaimTypes.NameIdentifier`); roles from `Identity`.

---

## 10. Simple Real Example

An instructor uploads a bank, creates a quiz, publishes it, and a student takes it.

1. Instructor uploads `java.json` → `QuestionBankService.UploadAsync` → `JsonValidationService.Validate` passes → bank v1 saved with parsed `Question`/`Option` rows.
2. Instructor creates a quiz: 5 questions from that bank, `SelectionMode=Random`, `PassingPercentage=70`, `MaximumAttempts=2`. `QuizService.CreateAsync` validates counts and saves it as `Draft`, then publishes it.
3. Student starts the quiz attached to module M3 → `AttemptService.StartAttemptAsync` picks seed `ABC`, shuffles 5 questions (Fisher–Yates) and shuffles each question's options, returns them.
4. Student answers; each click calls `SaveAnswerAsync` (upsert). Timer hits zero → auto `SubmitAttemptAsync`.
5. Server re-selects the same questions with seed `ABC`, computes correct count, `Percentage = correct/total*100`, `Passed = Percentage >= 70`.
6. Passed → module marked completed, `ProgressService` called, `QUIZ_COMPLETED` + `QUIZ_PASSED` audited.
7. Student opens the review page → `GetReviewAsync` shows selected vs correct vs explanation.
8. If they re-take it, a new attempt gets a new seed and a different question/option order.

---

## 11. Interview Answer

"In LearnPath I kept the controllers thin and put all business logic in a service layer. For example, `QuizService` handles quiz creation, publishing and archive with validation, `AttemptService` handles starting an attempt, saving answers, scoring and review. When a student starts a quiz I store a random seed on the attempt; on submit I re-select the same questions using that seed, shuffle the options, count how many match the correct option, calculate a percentage, and mark passed if it's at least the quiz's passing percentage. `QuestionBankService` handles uploading JSON banks that are validated by `JsonValidationService`, which checks the file extension, size, JSON structure, and each question's options and correct answer. `ClassroomService` handles invite codes, instructor/student roles, assignments and submissions with a strict status flow. And `AuthService` handles registration, login, refresh-token rotation and logout using ASP.NET Identity. Every service also writes audit log entries through `AuditLogService`."

---

## 12. Follow-Up Questions

1. **Why a service layer and not logic in the controller?**
   Controllers stay about HTTP; services own rules so the same logic is used everywhere, is testable, and security checks can't be skipped.

2. **How do you prevent a student from seeing the seed or answers?**
   The API never returns correct flags during an attempt — the attempt response only contains question text and shuffled options; correctness is only computed server-side on submit.

3. **How does randomization stay fair/reproducible?**
   A per-attempt `RandomSeed` is stored. Fisher–Yates shuffles the question list and each question's options with `seed (+ questionId)`. On submit the server replays the same selection, so scoring is always consistent with what the student saw.

4. **What happens if a student retries the quiz mid-attempt?**
   `StartAttemptAsync` returns the existing `InProgress` attempt instead of creating a duplicate, and site fetches resume from saved `StudentAnswer` rows.

5. **How is the pass mark applied?**
   `Passed = Percentage >= quiz.PassingPercentage`. Passing also marks the module completed and feeds `ProgressService`.

6. **What stops a quiz being deleted while assigned?**
   `QuizService.DeleteAsync` refuses if `ModuleQuizzes` count > 0 — same pattern as deleting a question bank still used by quizzes.

7. **Where is JSON validation implemented?**
   `backend/Services/Validation/JsonValidationService.cs`, used by `QuestionBankService` before anything is saved; invalid files produce per-question errors and nothing is stored.

8. **How do classroom permissions work?**
   Three levels: creator-only (`GetOwnedClassroomAsync`), membership+Instructor (`EnsureInstructorAsync`), membership+Admin role (`EnsureAdminAsync`), all enforced inside the service.

---

## 13. Functionality (confirmed by source)

**QuizService:** create/update with bank-count + difficulty-count validation, unique titles, draft/publish/unpublish/archive/delete (delete blocked while linked), link/unlink quizzes to modules (published-only, owns-path check), admin bypass via `IsAdminAsync`, owner checks, audit on every transition.

**AttemptService:** resume active attempts, `MaximumAttempts` enforcement (incl. "already passed" guard), seed-based random question selection + Fisher–Yates option shuffle, answer upsert, submit with score/percentage/passed, module completion + progress integration, audit (`QUIZ_STARTED`, `QUIZ_COMPLETED`, `QUIZ_PASSED`/`QUIZ_FAILED`), review with correct answers and explanations.

**QuestionBankService:** JSON upload with versioning, stored raw JSON, question/option parsing (difficulty mapping + correct-answer-by-text matching), search, version upload, archive/restore, delete guard, download, audit.

**JsonValidationService:** `.json` extension + 5 MB limit + parse + required fields + per-question rules (≥2 options, correct answer present and matching, difficulty in easy/medium/hard, explanation, no duplicates), structured `ValidationError` list, nothing saved on failure.

**ClassroomService:** create with 8-char invite code, auto-instructor membership, join/leave (instructor cannot leave), member removal, mark-invalid flows (students only, reason required, super admin protected), assignments CRUD, submission upload with file validation/storage + late detection + status rules, submission status state machine, grading, complete (grade ≥ 5), publish evaluation, audit on each step.

**AuthService:** register (auto Student role), login (Identity password check + account-status gating + failed-login audit), refresh-token rotation with 7-day expiry, logout (revoke all tokens + `LOGOUT` audit), JWT building with roles, password policy from Identity config.