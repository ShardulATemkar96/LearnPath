# LEARNPATH PROJECT INTERVIEW PREPARATION — Fresher Level
> **Verified:** 2026-08-28 from codebase | **Root:** `D:\FInal Project\LearnPath` | **Stack:** React 18 + Vite + MUI 5 + Redux Toolkit + Router 6 + Axios | .NET 10 + EF Core + Identity + JWT + AutoMapper + FluentValidation | SQL Server 2022
> **Source of truth:** `documentation/PROJECT_INDEX.md` → Controllers/Services/Entities → This file

---

## 1. Project Introduction

### 30-45 Second Introduction
> "LearnPath is a graph-based learning platform I built. Users can create learning paths, add modules with dependencies, and students progress step-by-step. Only when you complete prerequisite modules does the next module unlock. It also has classrooms with assignments, quizzes, progress tracking, certificates, and a community for discussion. Frontend is React, backend is ASP.NET Core, database is SQL Server."

### 60-90 Second Explanation
> "LearnPath helps students learn in a structured way. An admin creates a learning path — for example, 'Web Development' — and adds modules like HTML, CSS, JavaScript. They can set dependencies, like JavaScript depends on HTML. Students join, complete modules one by one. The system checks dependencies using a DAG — directed acyclic graph — so you can't start JavaScript before HTML. There's also classrooms where admins create assignments, students submit files, and get grades. Quizzes are linked to modules, and you must pass the quiz to unlock the next module. Progress is tracked, and when you finish all modules you get a certificate. There is also a community with posts, groups, and AI feedback for submissions."

### Detailed Explanation (when interviewer says "explain in detail")
> "LearnPath has three main parts. Frontend is React with MUI, Redux Toolkit, and Axios. Backend is ASP.NET Core with 17 controllers and 23 services. Database is SQL Server with 41 entities and 37 tables via EF Core. Authentication is JWT with access token 60 minutes and refresh token 7 days, plus roles Admin/Student. The main technical feature is the DAG for module dependencies — we use `DagValidator.WouldCreateCycle` with DFS to prevent cycles, and we check both DAG and sequential order to decide if a module is unlocked. Classrooms use invite codes, UserClassroom many-to-many. Submissions have file validation and a status flow. Quizzes have a question bank, attempt with random seed shuffle, and evaluation. Progress auto-issues certificate at 100%. Community has posts, votes, groups, moderation, and trending. All actions are audit-logged and logs are append-only."

**Key Points to remember:** Graph-based = DAG dependencies, not linear; 3-tier React → ASP.NET Core → SQL Server; JWT 60m/7d; Interviewer will ask DAG, Auth, and one feature deep.

---

## 2. Project Story / Problem

**What problem does LearnPath solve?**
Normal online courses are just a list of videos. No check if you finished prerequisites. LearnPath enforces order — you must complete prerequisites before unlocking next topic. This gives a real structured learning journey.

**Who is it for?**
- **Students:** Learn step-by-step, submit assignments, take quizzes
- **Admins (Creators):** Create paths/modules, manage classrooms, grade submissions
- **Admin:** Manage users, groups, reports, see audit history

**Why this project?** (prepare your personal answer)
Don't invent. Say: "I wanted to build a full-stack project that covers React, .NET, database, auth, and a real algorithm — DAG cycle detection. It shows frontend, backend, and database working together."

**What makes it interesting?**
DAG graph logic + file submissions + quiz engine + community + AI feedback in one app. Not just CRUD — there is real business logic.

---

## 3. Main Features (Actually Implemented — Verified)

| Feature | Verified Files |
|---|---|
| Auth + RBAC | `AuthController.cs`, `AuthService.cs`, `JwtTokenGenerator.cs`, `RoleSeeder.cs` |
| Learning Path + DAG | `LearningPathService.cs:699`, `DagValidator.cs`, `ModuleDependency.cs` |
| Module publish/archive/reorder | `LearningPathService.cs:528-680` |
| Progress + Certificate | `ProgressService.cs`, `CertificateController.cs` |
| Classroom + Invite Code | `ClassroomService.cs:1114`, `UserClassroom.cs` |
| Assignment + Submission (6MB) | `FileValidationService.cs`, `FileStorageService.cs`, `uploads/assignments/` |
| Quiz + QuestionBank + Attempt | `QuizService.cs`, `AttemptService.cs:171-249`, `JsonValidationService.cs` |
| AI Feedback (Nvidia) | `AiFeedbackService.cs`, `NvidiaProvider.cs`, `PromptBuilder.cs` |
| Community (posts/comments/votes/groups) | `CommunityService.cs`, `CommunityRepository.cs` |
| Analytics/Dashboard | `AnalyticsService.cs`, `DashboardService.cs` + Recharts |
| Admin + Audit Log (append-only) | `AuditLogService.cs`, `ApplicationDbContext.cs:62-75` |
| Notifications (poll, no SignalR) | `NotificationService.cs` |
| Theming light/dark | `palette.ts`, `ThemeModeContext.tsx` |

**Not fully implemented / Risk:** GeminiProvider dead code, RateLimit in-memory only, backend tests blocked.

---

## 4. Architecture & Overall Flow

```
Browser: React 18 (MUI 5 + Redux Toolkit + Router 6 + Axios)
  ↓  VITE_API_BASE_URL → http://localhost:5000/api/v1 (fallback)
Nginx :80 — /api/* → ASP.NET Core :5000
  ├─ Middleware: Exception → Logging → RateLimit(50/min, Headers X-RateLimit-*) → CORS(LearnPathCors) → Auth
  ├─ Controllers [Authorize] / [AllowAnonymous] + FluentValidation + ApiResponse<T> {success,message,data,errors}
  ├─ Services (23) → DbContext (30 DbSets + Identity) / CommunityRepository → SQL Server 37 tables
  └─ JWT HS256 60m/7d, ClockSkew=Zero, AutoMapper, Identity lockout 5/5m
```

**Request Example — Student opens module:**
`LessonPage.tsx` → `pathService` → `apiClient` (adds Bearer) → `GET /api/v1/paths/5/modules/10` → `JwtBearer` validates → `LearningPathController` → `LearningPathService.GetModuleContentAsync()` → checks `completedModuleIds` + DAG + sequential → throws 403 if locked → `ExceptionMiddleware` converts to `400/403/404/409` ApiResponse → React shows content or "complete previous module".

**File:** `backend/Program.cs:285-303` middleware order, `frontend/src/services/apiClient.ts:11-90` interceptors, `backend/Middleware/ExceptionMiddleware.cs:47-66` status mapping.

---

## 5. MUST KNOW QUESTIONS (Top 20-30 — Prepare These First)

### Q1. What is LearnPath?
**Priority:** MUST KNOW | **Difficulty:** Basic
**Answer:** "LearnPath is a graph-based learning platform where admins create learning paths with modules, set dependencies between modules, and students unlock modules step-by-step after completing prerequisites. It also has classrooms, assignments, quizzes, certificates, and a community."
**If Asked Deeper:** Explain DAG briefly — Directed Acyclic Graph, no cycles.
**Key Points:** Structured journey, DAG, 3 roles, full-stack.

### Q2. What tech stack did you use and why?
**Priority:** MUST KNOW | **Difficulty:** Basic
**Answer:** "Frontend React with Vite, MUI, Redux Toolkit, Axios. Backend ASP.NET Core with EF Core, Identity, JWT. Database SQL Server. React is component-based and popular, .NET is performant and has built-in Identity/JWT, SQL Server integrates well with EF Core."
**If Asked Deeper:** FluentValidation for validation, AutoMapper for DTO mapping.
**Key Points:** Don't say "I used because it's best" — say "fits well, good integration".

### Q3. Explain your architecture.
**Priority:** MUST KNOW | **Difficulty:** Basic
**Answer:** "3-tier. React SPA talks via REST to ASP.NET Core API at `/api/v1`, controllers call services, services use EF Core DbContext to hit SQL Server. JWT for auth, middleware for exception/rate-limit/CORS. Nginx proxies `/api` to backend."
**File:** `Program.cs:284-303`
**Key Points:** Frontend → API → Service → EF Core → SQL.

### Q4. How does login work?
**Priority:** MUST KNOW | **Difficulty:** Basic
**Answer:** "User posts email+password to `POST /api/v1/auth/login`. `AuthService.LoginAsync` finds user via `UserManager`, checks password with `CheckPasswordAsync`, checks `UserStatus` (Deleted/Inactive/Invalid), gets roles, calls `JwtTokenGenerator.GenerateAccessToken` (HS256, sub/email/jti/roles, 60m) + `GenerateRefreshToken` (64 random bytes). Saves refresh token 7 days in `RefreshTokens` table, returns `AuthResponseDto` with both tokens. Frontend `authSlice` loginThunk saves via `tokenUtils.setTokens`, `apiClient` adds Bearer header."
**Files:** `AuthController.cs:29-42`, `AuthService.cs:64-110`, `JwtTokenGenerator.cs:20-44`, `apiClient.ts:11-18`, `authSlice.ts:21-32`
**If Asked Deeper:** Refresh rotation — old token revoked on refresh.

### Q5. How does JWT authentication work?
**Priority:** MUST KNOW | **Difficulty:** Basic
**Answer:** "Access token 60 minutes, refresh token 7 days. Sent as `Authorization: Bearer <token>`. Backend `AddJwtBearer` validates issuer/audience/lifetime/signingKey with `ClockSkew=Zero`. On 401, `apiClient` queues requests (`isRefreshing` + `failedQueue`), calls `POST /auth/refresh` with both tokens, gets new pair, retries queue. If refresh fails, clears tokens and redirects to /login."
**Files:** `Program.cs:136-149`, `JwtTokenGenerator.cs`, `apiClient.ts:20-90`
**Key Points:** HS256, 60m/7d, Bearer, 401 queue.

### Q6. How do you handle roles / authorization?
**Priority:** MUST KNOW | **Difficulty:** Basic
**Answer:** "Two roles: Admin, Student seeded by `RoleSeeder`. On register, `AddToRoleAsync(user, "Student")`. Policies `AdminOnly` in `Program.cs:153-155`. Controllers use `[Authorize(Roles="Admin")]` or `[Authorize]`. Frontend `useAuth` checks `roles.includes(ROLES.ADMIN)` and `AdminRoute`/`ProtectedRoute` guard routes."
**Files:** `RoleSeeder.cs`, `Program.cs:153`, `useAuth.ts:26-28`
**Key Points:** IdentityRole, default Student, policy builder.

### Q7. What is DAG and how did you implement it?
**Priority:** MUST KNOW | **Difficulty:** Basic
**Answer:** "DAG is Directed Acyclic Graph — modules are nodes, dependencies are edges `ModuleId → DependsOnModuleId`. No cycles allowed. Before adding dependency, `DagValidator.WouldCreateCycle(adjacency, from, to)` does DFS from `to` to see if it can reach `from`. If yes, throw 'would create cycle'. We also have `TopologicalSort` for ordering. Unlock logic checks all dependencies are completed + sequential fallback."
**Files:** `DagValidator.cs:5-25`, `LearningPathService.cs:684-721`, `Entities/ModuleDependency.cs`
**If Asked Deeper:** Show DFS visited set, recursion `neighbors.Any(n => Dfs(...))`.
**Key Points:** DFS, cycle prevention, adjacency dictionary.

### Q8. How does module unlocking work?
**Priority:** MUST KNOW | **Difficulty:** Normal
**Answer:** "In `LearningPathService.GetByIdAsync` we build `completedModuleIds` set. For each module, `dagUnlocked = dependencyIds.All(dId => completedModuleIds.Contains(dId))`. If no dependencies, `sequentialUnlocked` checks previous module by `Order`. `isUnlocked = dagUnlocked && sequentialUnlocked`. `GetModuleContentAsync` throws `UnauthorizedAccessException` if not unlocked, which middleware turns to 403."
**File:** `LearningPathService.cs:64-98`, `325-361`
**Key Points:** Two checks: DAG + sequential, 403 if locked.

### Q9. How is progress tracked and certificate issued?
**Priority:** MUST KNOW | **Difficulty:** Basic
**Answer:** "Progress table has `UserId + ModuleId + IsCompleted`. `ProgressService.MarkCompleteAsync` checks dependencies again, creates/updates Progress, saves, then calls `TryIssueCertificateAsync` — if all modules in path are completed and no certificate exists, creates `Certificate` with `CertificateUrl = /certificates/{userId}/{pathId}`."
**File:** `ProgressService.cs:22-86`, `203-241`
**Key Points:** 100% triggers certificate, once per path.

### Q10. How did you design the database? Important tables?
**Priority:** MUST KNOW | **Difficulty:** Normal
**Answer:** "`ApplicationDbContext : IdentityDbContext<User>` has 30 DbSets, 37 tables. Core: User → LearningPath 1:N → Module 1:N → ModuleDependency self-ref, ModuleResource/Tag/Objective. Module → Progress (User+Module). Classroom → UserClassroom M2M → Assignment → Submission → SubmissionAiFeedback. Quiz hierarchy: QuestionBank → Quiz → Question → Option, ModuleQuiz link, QuizAttempt → StudentAnswer. Community: Post → Comment (ParentCommentId self) → Votes. Plus RefreshToken, Certificate, AuditLog, Notification."
**File:** `ApplicationDbContext.cs:12-41`
**Key Points:** Identity 7 tables + 30 DbSets, M2M UserClassroom, self-ref dependencies.

### Q11. How do classroom invite codes work?
**Priority:** MUST KNOW | **Difficulty:** Basic
**Answer:** "On create, `GenerateInviteCode()` does `Guid.NewGuid().ToString("N")[..8].ToUpper()` — 8 char code. Creator auto-joined as Admin. Students join via `POST classrooms/{id}/join` with code, checked `AnyAsync` duplicate, adds `UserClassroom` Role Student. Admin can remove member, but not another admin. Leave fails if Admin."
**File:** `ClassroomService.cs:1114-1115`, `213-237`
**Key Points:** 8 char, auto Admin, join checks duplicate.

### Q12. How does assignment submission work?
**Priority:** MUST KNOW | **Difficulty:** Normal
**Answer:** "Student calls `UploadSubmissionAsync` with `IFormFile`. First checks `EnsureAssignmentUnlockedAsync` — assignments unlock sequentially Grade>=5. Validates file via `FileValidationService`, stores via `FileStorageService.StoreAsync` to `uploads/assignments/` with unique name. If existing submission exists and status is UnderReview/Reviewed/Graded, block replace. Sets `IsLate = DueDate < now`, status Submitted or SubmittedAgain, saves DB, on DB failure deletes stored file. Audit logged."
**File:** `ClassroomService.cs:439-532`, `1040-1060`
**Key Points:** 6MB, sequential unlock, status guard, late flag, cleanup on fail.

### Q13. Explain quiz attempt lifecycle.
**Priority:** MUST KNOW | **Difficulty:** Normal
**Answer:** "Quiz has Draft/Published/Archived, linked via `ModuleQuizzes` (must be Published to link). `AttemptService.StartAttemptAsync` checks module unlocked, checks `MaximumAttempts` and if already passed, creates `QuizAttempt` with `RandomSeed = Random.Shared.Next()`, status InProgress. `SaveAnswerAsync` upserts `StudentAnswer`. `SubmitAttemptAsync` gets questions via `GetSelectedQuestionsAsync` (filtered by Difficulty, shuffled by seed), counts correct `Option.IsCorrect`, calculates `Percentage`, `Passed = >= PassingPercentage`, if passed calls `MarkModuleCompleteFromQuizAsync` → unlocks next module + certificate. Review shows correct/selected."
**Files:** `QuizService.cs:211-252`, `AttemptService.cs:25-118`, `171-308`
**Key Points:** RandomSeed shuffle, Percentage, auto progress on pass.

### Q14. How does frontend handle auth state and 401?
**Priority:** MUST KNOW | **Difficulty:** Normal
**Answer:** "Redux `authSlice` has `loginThunk/registerThunk` that call `authService` and `tokenUtils.setTokens`. `useAuth` exposes `user/isAuthenticated/roles/isAdmin`. `apiClient` request interceptor adds Bearer. Response interceptor on 401 (except login/register) queues with `failedQueue` if `isRefreshing`, otherwise sets `_retry`, calls `POST /auth/refresh` via raw `axios` (not apiClient to avoid loop), saves new tokens, retries original, or on fail clears and `window.location.href='/login'`."
**Files:** `apiClient.ts:11-90`, `authSlice.ts:21-50`, `useAuth.ts`
**Key Points:** Queue prevents multiple refresh calls, skip login 401.

### Q15. What is EF Core and how did you use it?
**Priority:** MUST KNOW | **Difficulty:** Basic
**Answer:** "EF Core is ORM to talk to SQL Server with C# objects, no raw SQL. `ApplicationDbContext` with `DbSet` for each table, 31 `IEntityTypeConfiguration` for config, `ApplyConfigurationsFromAssembly`. Queries with LINQ `Where/Include/ThenInclude/Select`, `SaveChangesAsync`. Migrations 11 files manage schema. `EnableRetryOnFailure` for transient errors, explicit transaction for LearningPath delete via `CreateExecutionStrategy`."
**File:** `Program.cs:105-108`, `ApplicationDbContext.cs:42-46`, `LearningPathService.cs:223-305`
**Key Points:** ORM, DbSet, LINQ, Migrations, retry.

### Q16. How do you handle errors? What is ApiResponse?
**Priority:** MUST KNOW | **Difficulty:** Basic
**Answer:** "All APIs return `ApiResponse<T> {success,message,data,errors}`. Service throws `UnauthorizedAccessException` → 403, `KeyNotFoundException` → 404, `ArgumentException` → 400, `DbUpdateException` FK547 → 409, else 500. `ExceptionMiddleware` maps and writes JSON. Frontend shows `message`. Validation also via FluentValidation + `InvalidModelStateResponseFactory` that returns same envelope."
**Files:** `ExceptionMiddleware.cs:47-66`, `Program.cs:179-200`, `Common/ApiResponse.cs`
**Key Points:** Uniform envelope, exception mapping.

### Q17. What is the file upload limit and how is it enforced?
**Priority:** MUST KNOW | **Difficulty:** Basic
**Answer:** "6MB. `FileValidationService.Validate(IFormFile)` checks extension, `FileSize`, `MimeType` against `UploadSettings`. `QuestionBankController` upload also 6MB via `JsonValidationService`. Stored with `FileStorageService` to disk `uploads/assignments/`."
**File:** `FileValidationService.cs`, `FileStorageService.cs`, `Configuration/UploadSettings`
**Key Points:** 6MB, whitelist extension, stored outside DB.

### Q18. What does the Community feature do?
**Priority:** MUST KNOW | **Difficulty:** Basic
**Answer:** "Posts with title/content/codeSnippet/tags/category/GroupId, comments with ParentCommentId self-ref for replies, votes toggle (upvote/downvote/withdraw, score +/-1 or +/-2), groups with roles Owner/Member/Banned, join/leave/ban, reports Pending/Resolved/Dismissed, trending via `score / (ageHours+2)^1.5`. Pagination via `CommunityRepository` PagedResult. Voted posts update `Score`. Banned users blocked via `EnsureActiveMembershipAsync`."
**Files:** `CommunityService.cs:248-298`, `1096-1100`, `1139-1186`

### Q19. How does AI feedback work?
**Priority:** SHOULD KNOW but often asked | **Difficulty:** Normal
**Answer:** "Admin-only `AiFeedbackService.GenerateAsync` reads submission text (TXT directly, PDF via `PdfPig`, else error), builds prompt via `PromptBuilder`, sends via `IAiProvider` which is `NvidiaProvider` registered as `AddHttpClient<IAiProvider,NvidiaProvider>`, parses JSON via `AiResponseParser`, saves to `SubmissionAiFeedback` (summary/grammar/rubric/missingTopics/suggestedScore). Only TXT/PDF supported, API key from env `LEARNPATH_NVIDIA_API_KEY`."
**Files:** `AiFeedbackService.cs:43-158`, `Program.cs:234`, `NvidiaProvider.cs`, `AiOptions`
**Key Points:** Provider pattern, TXT/PDF only, admin only.

### Q20. What is audit log and why append-only?
**Priority:** MUST KNOW | **Difficulty:** Basic
**Answer:** "Every important action calls `IAuditLogService.LogAsync(AuditAction.*, entity, description)`. `ApplicationDbContext.GuardAuditLogImmutability()` checks `ChangeTracker.Entries<AuditLog>().Any(Modified|Deleted)` and throws 'append-only cannot be modified or deleted' before SaveChanges. Ensures history can't be tampered."
**File:** `ApplicationDbContext.cs:62-75`
**Key Points:** Append-only, audit trail.

---

## 6. SHOULD KNOW QUESTIONS

### Q21. Why REST? Why not GraphQL?
**Priority:** SHOULD KNOW | **Difficulty:** Normal
**Answer:** "REST is simple, standard for .NET, fits CRUD for paths/modules/classrooms. Each resource has URL `api/v1/paths`, HTTP verbs. GraphQL would add complexity not needed for this app size. Plus Swagger easily documents REST."
**Key Points:** Simplicity, Swagger, fresher scope.

### Q22. How does the frontend PathGraph show dependencies?
**Priority:** SHOULD KNOW | **Difficulty:** Normal
**Answer:** "Frontend `utils/graphUtils.ts` mirrors `DagValidator` logic for graph visualization, `PathGraph` component draws nodes/edges. Backend sends `Dependencies` array, frontend builds adjacency. Exact file `src/components/learningPath/PathGraph` (check)."

### Q23. Explain Redux usage.
**Priority:** SHOULD KNOW | **Difficulty:** Normal
**Answer:** "Store has 7 slices: auth/path/classroom/community/analytics/dashboard/notification. Thunks call `*Service.ts` which uses `apiClient`. Slices handle pending/fulfilled/rejected. Selectors in `selectors/`. Community slice uses `requestId + AbortSignal` to fix race conditions on fast filter changes."
**Files:** `redux/store.ts`, `slices/authSlice.ts`, `slices/communitySlice.ts`, `services/apiClient.ts`

### Q24. How does FluentValidation work?
**Priority:** SHOULD KNOW | **Difficulty:** Basic
**Answer:** "12 validators in `Validators/`, registered via `AddValidatorsFromAssemblyContaining<Program>()`. Auto validates DTOs before controller runs. On fail, `InvalidModelStateResponseFactory` returns `ApiResponse.Fail` with messages instead of default ValidationProblemDetails."
**File:** `Program.cs:206`

### Q25. What is AutoMapper for?
**Priority:** SHOULD KNOW | **Difficulty:** Basic
**Answer:** "Maps Entity ↔ DTO without manual code. `AuthService` maps `RegisterRequestDto` to `User` via `_mapper.Map<User>(dto)`. Registered via `AddAutoMapper(cfg=>{}, Assembly)`."
**File:** `Program.cs:176`, `AuthService.cs:41`

### Q26. How is rate limiting implemented?
**Priority:** SHOULD KNOW | **Difficulty:** Normal
**Answer:** "`RateLimitingMiddleware` uses `ConcurrentDictionary<ip, (Count, WindowStart)>` with `MAX_REQUESTS=50` per 1 minute window (`AddOrUpdate`). Sets headers `X-RateLimit-Limit/Remaining`, returns 429 if exceeded, skips `/api/v1/health`. In-memory only — not distributed."
**File:** `RateLimitingMiddleware.cs:10-50`
**If Asked Deeper:** For multiple servers need Redis.

### Q27. Explain submission status flow.
**Priority:** SHOULD KNOW | **Difficulty:** Normal
**Answer:** "NotSubmitted → Submitted → UnderReview → Reviewed → Graded → Published. Also SubmittedAgain, ReturnedForResubmission. Admin `TransitionStatusAsync` only allows Submitted->UnderReview and UnderReview->Reviewed. Grading sets Grade+Feedback+Graded, publishing sets `PublishedAt` so student sees AiFeedback."
**Files:** `ClassroomService.cs:764-820`, `984-1038`, `Entities/Submission.cs: SubmissionStatus enum`

### Q28. How does quiz random selection work?
**Priority:** SHOULD KNOW | **Difficulty:** Normal
**Answer:** "`GetSelectedQuestionsAsync` filters by `QuestionBankId` and `DifficultyFilter`, orders by Id, if `SelectionMode==Random` shuffles with `new Random(seed)` Fisher-Yates `ShuffleList` seeded by `attempt.RandomSeed`, takes `QuestionCount`. Same for options shuffled by `seed + QuestionId` so each attempt is deterministic."
**File:** `AttemptService.cs:351-378`

### Q29. Why SQL Server? Why not MongoDB?
**Priority:** SHOULD KNOW | **Difficulty:** Basic
**Answer:** "Data is highly relational — LearningPath→Module→Dependency→Progress→Certificate, strict FKs. SQL Server gives ACID, transactions for LearningPath delete, EF Core migrations. Mongo would lose relationships and transactions complexity."
**Key Points:** Relational, FK integrity, transactions.

---

## 7. POSSIBLE QUESTIONS

### Q30. How does `ClockSkew = Zero` affect JWT?
**Priority:** POSSIBLE | **Difficulty:** Normal
**Answer:** "By default JWT allows 5 min clock skew. We set Zero so token expires exactly at 60m, no extra tolerance. More strict."
**File:** `Program.cs:148`

### Q31. What is Dependency Injection? Example?
**Priority:** POSSIBLE | **Difficulty:** Basic
**Answer:** "DI means services are created by container, injected via constructor. Example `AuthService` gets `UserManager`, `JwtTokenGenerator`, `IMapper`, `DbContext`, `IAuditLogService` via constructor, registered as `AddScoped<IAuthService, AuthService>`."
**File:** `Program.cs:209-225`

### Q32. How is password stored?
**Priority:** POSSIBLE | **Difficulty:** Basic
**Answer:** "Not plain text. `UserManager.CreateAsync(user, password)` hashes with Identity's PBKDF2. Config: `RequireDigit/Upper/Lower 8 chars, Lockout 5m after 5 fails`."
**File:** `Program.cs:111-121`, `AuthService.cs:42`

### Q33. What is CORS and how configured?
**Priority:** POSSIBLE | **Difficulty:** Basic
**Answer:** "`AddCors` policy `LearnPathCors` WithOrigins from `AllowedOrigins` config split by comma, AllowAnyHeader/Method/AllowCredentials. Applied via `app.UseCors`."
**File:** `Program.cs:158-169`

### Q34. How is theme switching done?
**Priority:** POSSIBLE | **Difficulty:** Basic
**Answer:** "`theme/palette.ts` has `lightPalette/darkPalette`, `getAppTheme(mode)` returns MUI theme, `ThemeModeContext.tsx` stores `learnpath-theme-mode` in localStorage, `SettingsPage.tsx` toggle Night Mode, `provider.tsx` wraps with `ThemeProvider` + `CssBaseline`."
**Files:** `theme/palette.ts`, `ThemeModeContext.tsx`

### Q35. How do you handle .env secrets?
**Priority:** POSSIBLE | **Difficulty:** Normal
**Answer:** "`Program.cs` loads `.env` via `DotNetEnv.Env.Load`, maps `LEARNPATH_NVIDIA_API_KEY/JWT_SECRET/DB_CONNECTION` to config keys, adds via `AddInMemoryCollection`, validates missing vars throws `InvalidOperationException`. Never committed."
**File:** `Program.cs:40-98`

---

## 8. DEEP FOLLOW-UP (Only if interviewer probes deeper)

### Q36. Would your rate limiter work with multiple servers?
**Answer:** "No, it's `ConcurrentDictionary` in memory per instance. With 3 pods, each allows 50/min = 150 total. Fix is Redis or `AspNetCoreRateLimit` with distributed store."
**File:** `RateLimitingMiddleware.cs:10`

### Q37. Refresh token rotation — what if attacker steals refresh token?
**Answer:** "We revoke old token immediately `stored.IsRevoked=true` before issuing new. If attacker reuses revoked token, `FirstOrDefaultAsync(Token== && !IsRevoked && ExpiresAt>UtcNow)` fails → 401. But need also detection to revoke family. Current code doesn't have family detection — flag as improvement."

### Q38. How does `GuardAuditLogImmutability` work internally?
**Answer:** "Overrides `SaveChanges/SaveChangesAsync`, checks `ChangeTracker.Entries<AuditLog>().Any(Modified|Deleted)` and throws `InvalidOperationException` before `base.SaveChanges`."
**File:** `ApplicationDbContext.cs:48-75`

### Q39. Explain transaction in `DeleteAsync` for LearningPath.
**Answer:** "SQL Server has `EnableRetryOnFailure`, so must use execution strategy: `var strategy = _context.Database.CreateExecutionStrategy(); await strategy.ExecuteAsync(async()=>{ using var tx = BeginTransactionAsync(); ... 7 deletes in FK order: StudentAnswers → QuizAttempts → ModuleDependencies → ModuleQuizzes → Progresses → Objectives/Resources/Tags → Modules → Path; SaveChanges x2; Commit; })`. Prevents partial delete."
**File:** `LearningPathService.cs:223-305`

### Q40. Why Fisher-Yates shuffle with seed?
**Answer:** "Deterministic randomization: same `RandomSeed` gives same order for review. `for i=count-1 downto 1: j=rng.Next(0,i+1); swap(i,j)`. Seed is `attempt.RandomSeed` + `QuestionId` for options, so order stable per attempt but different per user."
**File:** `AttemptService.cs:368-378`

### Q41. What is `TopologicalSort` for? When used?
**Answer:** "Orders DAG nodes so dependencies come before dependents. Uses DFS with `visited` + `inStack` for cycle detection, pushes to stack. Not directly called in unlocking now but available for validating full path order and future features."
**File:** `DagValidator.cs:27-65`

---

## 9. Frontend Questions

**Q: How is routing protected?**
`AppRoutes.tsx` has 34 lazy routes wrapped in `ProtectedRoute` (checks isAuthenticated), `AdminRoute`, and `GuestRoute` (redirects if already logged). Lazy via `React.lazy`.

**Q: How do you call APIs?**
`apiClient` axios instance baseURL `VITE_API_BASE_URL ?? localhost:5000/api/v1`, `withCredentials:true`, 14 services (`auth/path/classroom/quiz/...`) wrap calls. Redux thunks dispatch and update slice.

**Q: How is error shown?**
`ApiResponse` message displayed via MUI Snackbar/Alert, 401 handled globally, 403 shows "Please complete previous module".

**Q: State vs URL state?**
Auth/progress in Redux, current pathId/moduleId in URL params `useParams`, community filters in query + Redux.

---

## 10. Backend Questions

**Q: Controller → Service → Repository?**
Controllers thin (validate, delegate), Services business + audit, DbContext direct (only Community uses Repository `CommunityRepository` for paging). `Program.cs:229` registers repository.

**Q: Middleware order?** `Exception→Logging→RateLimit→Cors→Auth→Authorization→Controllers` — Exception first to catch all, RateLimit before Auth to protect even unauth endpoints.

**Q: How is validation done?**
FluentValidation 12 validators + manual checks: duplicate title, cycle, dependency exists, publish needs content/title/description, `MarkInvalid` needs reason.

---

## 11. Database Questions

**Q: Explain ModuleDependency table.**
`ModuleDependency {ModuleId, DependsOnModuleId}` composite FK to Module, self-ref many-to-many. No payload, just edge.

**Q: Why `UserClassroom` not just ClassroomId in User?**
Many-to-many: one user in many classrooms, one classroom many users, with extra payload `Role, JoinedAt`.

**Q: What is `ApplyConfigurationsFromAssembly`?**
31 `IEntityTypeConfiguration` classes configure keys, relationships, deletes; auto-applied in `OnModelCreating`.

**Q: Migrations?**
11 migrations + Snapshot, applied on startup via `await db.Database.MigrateAsync()` in `Program.cs:309`.

---

## 12. Authentication & Authorization — Deep

**Q: Registration flow?**
`AuthService.RegisterAsync`: check `FindByEmailAsync` exists → throw, `Map<User>(dto)`, `CreateAsync(user,password)` → `AddToRoleAsync(Student)` → `Audit USER_CREATED` → `BuildAuthResponseAsync` returns tokens.

**Q: Refresh flow?**
`RefreshTokenAsync`: `Include(User)` find token `IsRevoked==false && ExpiresAt>UtcNow` else 401, `IsRevoked=true`, Save, check UserStatus not Deleted/Inactive/Invalid, `BuildAuthResponseAsync` creates new pair.

**Q: What if token expires?**
Access 60m expiry → 401 → apiClient refresh with refresh 7d → new access. If refresh expired/revoked → 401 → logout.

**Q: Password lockout?**
Identity `MaxFailedAccessAttempts=5`, `DefaultLockoutTimeSpan 5m`. Not manually checked in Login but Identity tracks via `CheckPasswordAsync` increment.

**Q: Protected APIs?**
All except `POST register/login/refresh` and `GET paths/public` require `Authorization`. `GET my` / `POST revoke` require `[Authorize]`.

---

## 13. Important Feature Deep Dives

### Learning Path Deep Dive
Create → Add Modules (title unique per path) → Add Dependencies (cycle check adjacency built from `existingDeps.GroupBy(ModuleId).ToDictionary`) → Publish (needs title/desc/content) → GetById checks unlock. Delete blocked if classrooms/certificates exist, else 7-step cascade delete in transaction.

### Progress Deep Dive
`MarkCompleteAsync` also checks dependencies, else 403. `MarkModuleCompleteFromQuizAsync` called on quiz pass → same cert logic. `GetPathProgressAsync` builds summary `ProgressPercent = completed/total*100`, `IsCertificateEligible = 100%`.

### Submission Deep Dive
See Q12 + status transitions `TransitionStatusAsync` (Admin only, only Submitted↔UnderReview, UnderReview↔Reviewed), `GradeSubmissionAsync` sets `Graded`, `PublishEvaluationAsync` requires Graded then sets `PublishedAt` → triggers AiFeedback visible to student.

### Community Deep Dive
Trending `CalculateTrendingScore` = `score / (ageHours+2)^1.5` — newer high-score rank higher. Pin: only group posts, only group Owner/Admin, auto-unpins others. Ban: sets `Role=Banned`, checks in `EnsureActiveMembershipAsync`.

---

## 14. WHY Questions

- **Why ASP.NET Core?** Performant, cross-platform, built-in DI/Middleware/Identity/JWT/Swagger, standard for .NET.
- **Why React?** Component model, MUI for fast UI, Redux Toolkit for auth/community state, Vite for fast dev.
- **Why SQL Server?** Relational integrity, EF Core migrations, transactions, 37 tables with FKs.
- **Why EF Core?** No raw SQL, LINQ, change tracking, migrations, easy Include/ThenInclude.
- **Why JWT?** Stateless, no server session, scales, works with SPA + mobile.
- **Why REST not gRPC?** Simplicity, browser friendly, Swagger docs, fresher scope.
- **Why DAG?** Need prerequisites, cycle prevention critical; DAG models real learning dependencies better than linear list.
- **Why 60m/7d?** Short access limits exposure, long refresh reduces login frequency, rotation balances security.

---

## 15. HOW Questions

- **How does login reach DB?** React form → authService.login → apiClient POST → AuthController → AuthService → UserManager → EF Core Users table → hash compare → generate JWT → return → Redux save.
- **How does module unlock get data?** `Include(p.Modules.ThenInclude(m.Dependencies)).ThenInclude(Progresses.Where(userId))` single query → build hashset → compute unlocked.
- **How does file reach disk?** `IFormFile.OpenReadStream()` → `FileStorageService.StoreAsync` generates unique filename → `uploads/assignments/{assignmentId}/{userId}_{guid}.{ext}` → `FileStream`.
- **How does quiz eval happen?** Load `Questions.Include(Options)`, shuffle via seed, compare `StudentAnswer.OptionId` vs `Options.First(IsCorrect).Id` → score → percentage.
- **How does certificate generate?** After Progress save, count completed vs total → if equal and not already issued → insert Certificate.

---

## 16. What-If Questions

- **What if auth fails?** Login wrong email → 401 "Invalid credentials" + audit LOGIN_FAILED. Wrong password same. Deleted/Inactive/Invalid status → 401 specific message.
- **What if token expired?** JwtBearer fails validation → 401 → apiClient interceptor tries refresh → if refresh invalid → logout to /login.
- **What if record not found?** `FirstOrDefaultAsync ?? throw KeyNotFoundException` → ExceptionMiddleware 404 with ApiResponse Fail message.
- **What if invalid data?** `ArgumentException` → 400, FluentValidation → 400 with errors array.
- **What if prerequisite not completed?** `UnauthorizedAccessException("Please complete previous...")` → 403. Same for assignment locked Grade<5.
- **What if API fails (500)?** ExceptionMiddleware logs `LogError` with method+path, returns 500 "An unexpected error occurred" without leaking stack.
- **What if file too big?** `FileValidationService` returns `IsValid false` → 400, QuestionBank 6MB `JsonValidationService` similar.
- **What if cycle creation?** `WouldCreateCycle` true → 400 "would create cycle".
- **What if classroom delete has dependencies?** Throws 400 with count message.
- **What if rate limit hit?** 429 + headers `X-RateLimit-Remaining 0`, log warning.

---

## 17. Personal Contribution Questions

**Needs personal clarification — don't invent:**

- Interviewer: "What did you personally work on?"
  **Honest answer:** "I built this as a fresher full-stack project. I implemented [pick 2-3 you can defend, e.g., DAG unlocking + Auth + Classroom or Progress+Certificate]. For other parts like AI feedback/community, I understood the code and can explain but I focused on ..."

- "Team or solo?" — If solo, say solo academic project. If team, name your modules.

- Mark unknown: If you didn't write `NvidiaProvider` or `CommunityRepository`, say "I integrated it and understand the flow via `IAiProvider` pattern, but need to recall details" — better than bluffing.

---

## 18. Challenges & Technical Decisions

**Verified trade-offs:**

- **DAG vs simple Order:** Chose DAG + sequential fallback because pure DAG allowed gaps but sequential ensures Module1→Module2 even without explicit deps. Code `if(!dependencyIds.Any()) check prev Order` — prepare to defend.
- **Delete cascade explicit vs ON DELETE CASCADE:** Chose explicit 7-step remove in code regardless of DB cascade to be schema-independent. Explain transaction + execution strategy.
- **In-memory RateLimit:** Simple but not distributed — honest about limitation and Redis fix.
- **File storage on disk vs blob:** Chose disk `uploads/` for simplicity, but note production needs S3/Azure Blob.
- **Challenge: 401 queue race:** Fixed via `isRefreshing + failedQueue` — without queue multiple 401s trigger multiple refresh calls. Explain.
- **Challenge: Community pagination race:** Fixed via `requestId + AbortSignal` in `communitySlice`.

**If can't establish from code:** Say "I need to prepare personal explanation for why we chose HS256 not RS256 — HS256 simpler for single server."

---

## 19. Security Questions

- **Is JWT secret strong?** From env `LEARNPATH_JWT_SECRET`, must be long random; checked via `GetEnv` validation throws if missing.
- **Are passwords hashed?** Yes via Identity PBKDF2, never plain.
- **Is audit log tamper-proof?** Yes `GuardAuditLogImmutability` blocks Modified/Deleted.
- **Weak area: CORS `AllowCredentials` + `WithOrigins` split by comma — must ensure no `*` with credentials. Current code splits `AllowedOrigins` config.
- **Weak area: No refresh token family revoke detection — if stolen token reused after rotation, only old one fails, but attacker could keep using new stolen one.**
- **Weak area: File upload — need to validate mime vs extension, not just extension, and scan content; current `FileValidationService` does extension/size/mime but not content scan.**
- **AI key exposure?** No — via `.env` and `AddInMemoryCollection`, diagnostic prints only `✓ loaded` not value.

---

## 20. Performance Questions

- **N+1?** Avoided via `Include/ThenInclude` single query for path with 5 Includes, but `GetUserProgressAsync` loops `foreach pathId BuildPathSummary` — could be optimized with single query.
- **Pagination?** Community uses `PagedResult` with `Skip/Take`, posts trending takes `max(pageSize*10,100)` then ranks in memory — ok for small data but not for large scale.
- **Trending in DB vs memory?** Current does `Take(100)` then `CalculateTrendingScore` in memory — fine for demo, for scale compute in DB or cache.
- **Indexing?** Need indexes on `Module.LearningPathId`, `Progress.UserId+ModuleId`, `RefreshToken.Token`, `Post.GroupId` — check `Configurations/` 31 files.

---

## 21. Testing Questions

- **What testing exists?** `backend.Tests/` xUnit + Moq + InMemory + FluentAssertions (16 files): `DagValidatorTests.cs`, `Services/*`, `AdminControllerTests.cs`, `Helpers/DbContextFactory.cs`, CI runs `dotnet test`.
- **Known failure:** `testingErrorReport.txt` `FileLoadException` Application Control policy blocked assembly — tests currently blocked on this machine, `dotnet test` fails before running. Mention and fix is policy exception.
- **Frontend tests?** `vitest.config.ts` present but coverage low.
- **What to say?** "I wrote DagValidator tests for cycle detection and service tests with InMemory DB, but CI currently blocked by FileLoadException — I'm fixing policy."

---

## 22. Deployment Questions

- **How to run?** `docker-compose.yml` → `sql:2022`, `api:5000`, `frontend:80`. `docker-compose up` → backend runs `Database.MigrateAsync()` + `RoleSeeder` + `AdminSeeder admin@learnpath.com / Admin@123` on startup.
- **Frontend deploy?** `Dockerfile` + `nginx.conf` SPA fallback + `/api/` → `backend:5000`, `vite.config.ts` proxy.
- **CI/CD?** `.github/workflows/ci.yml` runs `dotnet test + npm build`, `cd.yml` for deploy.
- **Env?** `.env.example` template, required `LEARNPATH_NVIDIA_API_KEY/JWT_SECRET/DB_CONNECTION`.

**Risk:** `apiClient.ts` fallback `localhost:5000` breaks docker if `VITE_API_BASE_URL` not set — must set to `/api/v1` for nginx proxy.

---

## 23. Scalability Questions (Fresher level)

- **Current limits:** Single SQL instance, in-memory rate limit, file on disk, no SignalR for notifications (poll), no caching.
- **If 10k users?** Add Redis for rate limit + cache progress, move files to S3, add pagination everywhere, use `AsNoTracking` for reads, add indexes, consider read replica.
- **Stateless API helps?** Yes, JWT stateless so can scale horizontally behind load balancer, except rate limiter needs Redis.

---

## 24. Interview Risk Areas — Honest Flags

| Area | Risk | Why Interviewer Will Ask | What to Say |
|---|---|---|---|
| GeminiProvider dead code | File exists but `Program.cs` registers only `NvidiaProvider` | "You said AI feedback, but why two providers?" | "Gemini was experiment, removed via DI, only Nvidia active — I'd clean it." |
| RateLimit 50 vs docs 100 | Middleware 50, docs say 100, in-memory only | "Is your rate limit correct/distributed?" | "Actual is 50/min, in-memory, not distributed — need Redis, docs outdated." |
| Backend tests FileLoadException | `testingErrorReport.txt` blocked | "Did you test?" | "Tests written but blocked by Application Control policy — fixing." |
| Hardcoded `localhost` fallback | `apiClient.ts:5` fallback breaks docker | "Will it work in prod?" | "Yes if env set, but fallback is risk — should default to `/api/v1`." |
| Duplicate `ADMIN_QUIZZES` route | `AppRoutes.tsx` twice | "Why two routes?" | "Duplicate, should remove one — bug." |
| Upload 6MB everywhere? | Assignment 6MB, QuestionBank 6MB | "Is 6MB enough for videos?" | "For docs/json yes, for video would need larger + chunked upload." |
| Certificate URL not PDF | `/certificates/{userId}/{pathId}` not real file | "Where is PDF?" | "It's a placeholder URL, not generated PDF — improvement is PDF generation." |
| Notification no SignalR | Bell polls, not realtime | "Is notification realtime?" | "No, polling — realtime would need SignalR/WebSocket." |
| Module delete checks dependents | `Dependents.Any()` blocks delete | "What if dependency?" | "Correctly blocks with message — need to remove deps first." |

**Don't hide weaknesses — interviewer respects honesty + fix plan.**

---

## 25. Things I Must Understand (Final Revision Must)

- [ ] JWT flow: register → login → Bearer → 401 queue → refresh → retry → logout
- [ ] DAG DFS: `WouldCreateCycle(adjacency, from, to)` DFS from `to` to `from`
- [ ] Unlock: `dagUnlocked && sequentialUnlocked` → 403
- [ ] Progress → Certificate 100% check
- [ ] Invite code 8 char + UserClassroom M2M
- [ ] Submission 6MB + IsLate + status flow + file cleanup on DB fail
- [ ] Quiz shuffle seed + evaluation Pass%
- [ ] ApiResponse envelope + ExceptionMiddleware mapping
- [ ] Audit append-only guard
- [ ] RateLimit 50/min headers + 429
- [ ] `Program.cs` env loading + DI + middleware order
- [ ] Frontend `apiClient` interceptors + `useAuth` + `ProtectedRoute`

---

## 26. Final Revision Checklist

**Day before interview:**
1. Recite 30-sec intro without reading
2. Draw architecture on paper: React → Nginx → ASP.NET → EF Core → SQL
3. Explain DAG on paper with 3 modules A→B, B→C, try add C→A cycle
4. Run `POST /api/v1/auth/login` via Swagger, copy token, call `GET /api/v1/paths/my` with Bearer
5. Open `DagValidator.cs:5` and explain DFS line by line
6. Open `apiClient.ts:20` and explain failedQueue
7. List 3 risks and fixes (rate limiter, tests, Gemini dead code)
8. Prepare personal contribution story (2 features you own + 1 you understand)

**Common fresher mistake:** Saying "I used JWT because it's secure" — say "stateless, no server session, fits SPA".

**Final Goal:** You can explain project naturally, answer why/how/what-if, defend actual code, admit limits, and show fix plan. That's better than pretending perfect.

---
*Based on actual code — Index → Files → Implementation → Questions. Never guess beyond this file.*
