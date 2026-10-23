# LearnPath — Factual Interview Material (Verified from Code)
> **Purpose:** Fact base for a ~5 min natural interview explanation. **Do not copy as speech** — use this to build your own words.
> **Source of truth:** Code in `F:\Final Project\LearnPath` | Verified from `backend/`, `frontend/`, `documentation/PROJECT_INDEX.md` | Date: 2026-08-30
> **Stack:** React 18 + Vite + MUI 5 + Redux Toolkit + Router 6 + Axios | .NET 10 + EF Core + Identity + JWT + AutoMapper + FluentValidation | SQL Server 2022 | Nginx 80 → API 5000

---

## 1. What Problem LearnPath Solves (Fact Checked)

**Real problem from code:**
- Students have many resources but no order. Traditional lists let you jump anywhere.
- Result: beginners open advanced topics too early, fast learners waste time, admins can't enforce prerequisites.

**What LearnPath does instead (implemented):**
- Learning Path is a container (`Entities/LearningPath.cs`) that holds Modules.
- Modules can depend on other modules (`Entities/ModuleDependency.cs` with `ModuleId → DependsOnModuleId`). This forms a graph.
- System unlocks a module only when its prerequisites are completed. This is enforced both in the API and in the UI.

**Interview line you can use later (not the speech):**
> "I wanted the path to feel like a journey — basic → advanced — where the system knows what should come first and won't let you skip ahead."

---

## 2. Why This Approach (Factual Motivation)

- **Not a simple course list:** Code shows `LearningPathService.GetByIdAsync` builds `completedModuleIds` and calculates `isUnlocked = dagUnlocked && sequentialUnlocked` — so order matters.
- **Why graph, not list:** Lists are rigid. Graph lets one module depend on several prerequisites, or have no dependency and just follow order. Verified in `LearningPathService.cs:64-98` and `325-361`.
- **Project evolution (verified):** Started as paths+modules, then added Quiz Engine (`QuestionBank/Quiz/Attempt`), Audit Logging (`AuditLogService`), Role management (`RoleSeeder` + one `AdminOnly` policy), Classroom+Assignments, Community, AI Feedback. This matches your note that features were added gradually.

---

## 3. How the Learning Journey Actually Works (End-to-End)

1. **Admin creates path** → `POST api/v1/paths` → `LearningPathService.CreateAsync` — `CreatedById = userId`, `IsPublic/IsPublished`.
2. **Adds modules** → `POST api/v1/paths/{pathId}/modules` → `AddModuleAsync` checks duplicate title in same path.
3. **Connects dependencies** → `POST api/v1/paths/{pathId}/dependencies` with `{ModuleId, DependsOnModuleId}` → `AddDependencyAsync` builds adjacency from DB and calls `DagValidator.WouldCreateCycle`.
4. **Publishes modules** → `PublishModuleAsync` requires title + description + at least one content (ContentBody/ContentUrl/NotesHtml/Resources) and `IsArchived=false`.
5. **Student opens path** → `GET api/v1/paths/{id}` → backend returns `Modules[]` each with `IsCompleted, IsUnlocked` + `Dependencies[]` + `PathGraph` renders SVG (`frontend/src/components/learningPath/PathGraph/PathGraph.tsx`).
6. **Student sees:** Completed = green check, Unlocked = blue play, Locked = gray lock + 0.65 opacity + Chip "Locked" — `LearningPathDetailPage.tsx:26-136`. Clicking locked shows Snackbar `Please complete the previous module...` and does not navigate.
7. **Completes module** → `POST api/v1/progress/complete` or via quiz pass → `ProgressService.MarkCompleteAsync` creates `Progress {UserId, ModuleId, IsCompleted=true}` → `TryIssueCertificateAsync` checks if `completedCount == totalModules` → creates `Certificate`.
8. **Next modules automatically unlock** on next fetch because their prerequisites are now in `completedIds`.

**Simplest example to tell interviewer:**
> `Basic Java → OOP → Collections → Advanced Java` in one path. Set `OOP depends on Basic Java`. If student hasn't completed Basic Java, OOP shows Locked. After `Mark Complete` on Basic Java, OOP becomes `IsUnlocked=true` and the Graph edge lights up.

---

## 4. Main Features (Only Actually Implemented)

| Feature | What exists | Key files |
|---|---|---|
| **DAG Learning Path** | Paths, Modules, Dependencies, Publish/Archive/Reorder, Public/My lists, Graph view | `LearningPathService.cs`, `DagValidator.cs`, `ModuleDependency.cs` |
| **Progress + Certificate** | Mark complete, path progress %, auto certificate at 100% | `ProgressService.cs`, `CertificateController.cs` |
| **Quiz Engine** | Question bank JSON upload (5 MB), Quiz (Draft/Published/Archived), ModuleQuiz link, Attempt with random seed, evaluation | `QuestionBankService.cs`, `QuizService.cs`, `AttemptService.cs` |
| **AI Feedback** | Nvidia provider only (working), prompt → LLM → parser → `SubmissionAiFeedback` | `AiFeedbackService.cs`, `NvidiaProvider.cs`, `PromptBuilder.cs` |
| **Classroom + Assignment** | Create classroom (8-char invite code), join/leave, create assignments, submit file (up to 6 MB), grade, status flow | `ClassroomService.cs`, `FileValidationService.cs` |
| **Community** | Posts with CodeSnippet/tags, comments/replies, votes, Groups, Reports, Trending | `CommunityService.cs`, `CommunityRepository.cs` |
| **Auth + Roles** | Register/Login/Refresh/Revoke, JWT 60m/7d, Student/Admin | `AuthService.cs`, `JwtTokenGenerator.cs`, `RoleSeeder.cs`, `Program.cs:153` |
| **Audit Logging** | Append-only, all major actions logged, user history + SuperAdmin history | `AuditLogService.cs`, `AuditLog.cs`, `ApplicationDbContext.cs:62` |
| **Dashboard/Analytics** | Stats, weekly charts | `DashboardService.cs`, `AnalyticsService.cs`, `Recharts` |

**NOT / PARTIALLY IMPLEMENTED (mark honestly):**
- `GeminiProvider.cs` exists but **dead code** — `Program.cs:234` registers only `NvidiaProvider` as `IAiProvider`. No factory.
- Certificate URL is placeholder `"/certificates/{userId}/{pathId}"` — **not a real PDF file**.
- Notifications are polling via `GET /api/v1/notifications` — **no SignalR / WebSocket realtime**.
- Rate limit is **in-memory** `ConcurrentDictionary` per instance — not distributed (Redis needed for scale).
- `ADMIN_QUIZZES` route duplicated in `AppRoutes.tsx`.

---

## 5. Deep Dive — 4 Most Important Features

### 5.1 Module Dependency / DAG (Most Important for Story)

**In simple words:** A learning path is not a list. I connect modules with "you must finish this before that". Inside the code this connection is called a dependency, and all dependencies together form a graph. Because a later topic should not loop back to an earlier one, the graph must have no cycles — we call that a DAG.

**Why needed (verified):** Without it, a student could open `Collections` before `OOP` and get confused. The product goal in code is `IsUnlocked` derived from completed prerequisites.

**How modules are connected:**
- Table `ModuleDependencies` has two columns: `ModuleId` (the module that needs something) and `DependsOnModuleId` (the prerequisite). Both are foreign keys to `Modules`. Config in `Configurations/ModuleDependencyConfiguration.cs`.
- Example row: `ModuleId=3 (Collections), DependsOnModuleId=2 (OOP)` means Collections depends on OOP.

**Where stored:** `backend/Entities/ModuleDependency.cs`, `ApplicationDbContext.cs:17` `DbSet<ModuleDependency>`, applied via `ApplyConfigurationsFromAssembly`.

**How system checks if dependency exists:**
- `LearningPathService.GetByIdAsync:73` → `m.Dependencies.Select(d => d.DependsOnModuleId).ToList()` → `dependencyIds.All(dId => completedModuleIds.Contains(dId))` — all must be completed.
- If a module has **no explicit dependencies**, it falls back to **sequential order**: check previous module by `Order` — `sortedForUnlock.FindIndex` and `completedModuleIds.Contains(prevModuleId)` — `LearningPathService.cs:77-86`.

**How DAG is used:**
- Every fetch of a path recomputes `IsUnlocked` from the student's `Progress` table. No background job.
- `PathGraph.tsx` draws nodes and directed edges from `path.dependencies`.

**Cycle prevention:**
- `DagValidator.WouldCreateCycle(Dictionary<int,List<int>> adjacency, int from, int to)` does DFS from `to` to see if it can reach `from` via existing edges — `DagValidator.cs:5-25`. If yes → `throw ArgumentException("Adding this dependency would create a cycle.")` — `LearningPathService.cs:699`.
- `TopologicalSort` also exists (`DagValidator.cs:27-65`) with cycle detection via `inStack` — used for ordering, not for unlocking directly.

**How system decides unlocked:**
```
completedIds = all moduleIds where Progress.IsCompleted=true for this user
dagUnlocked = dependencyIds.All(id => completedIds.Contains(id))
sequentialUnlocked = if no dependencies then previous Order completed else true
isUnlocked = dagUnlocked && sequentialUnlocked
```

**Previous-module completion / quiz requirement:**
- `ProgressService.MarkCompleteAsync:31-42` also checks dependencies before allowing complete.
- For assignments: `ClassroomService.cs:79-92` hides description if `Grade <5` in previous assignment — `IsUnlocked = prevSubmission?.Grade >=5`.
- For quizzes: `AttemptService.StartAttemptAsync:39-66` does same unlock check before allowing attempt; `SubmitAttemptAsync:222` calls `MarkModuleCompleteFromQuizAsync` only if `Passed==true`.

**Backend prevents bypass:**
- `GetModuleContentAsync:359` → `if (!isUnlocked) throw UnauthorizedAccessException("Please complete the previous module...")` → `ExceptionMiddleware.cs:50` → `403 Forbidden`.
- Even if frontend tried to fetch `GET /api/v1/paths/{pathId}/modules/{moduleId}`, same check runs.
- Classroom and Attempt services have same guards.

**Frontend locked representation:**
- `LearningPathDetailPage.tsx:26` `ModuleCard` — border color green/blue/gray, bg, opacity 0.65, icon `CheckCircle/PlayArrow/LockRounded`, Chip `Done/Start/Locked`, click handler: if `!isUnlocked` → `Snackbar` message, else `navigate(/paths/{id}/modules/{id})`.

**What happens when student tries locked module:**
- Click → Snackbar bottom-center 3s — no navigation. Direct API call → `403` with `ApiResponse Fail("Please complete...")`.

**What happens after previous completed:**
- `progressService.markComplete(module.id)` → `dispatch(fetchPathById)` → backend recomputes → next module flips to `IsUnlocked=true`, Chip becomes "Start", border blue.

**Who is responsible:**
- `DagValidator.cs` (algorithm), `LearningPathService.cs` (all unlock logic), `ProgressService.cs` (certificate), `LearningPathController.cs` (routes `api/v1/paths`), `ApplicationDbContext.cs` (data), `PathGraph.tsx` + `LearningPathDetailPage.tsx` (UI).

**Follow-up you should expect:** "What is difference between DAG and sequential?" → Answer: DAG is explicit prerequisites you set; sequential is fallback when you don't set any — then we just enforce Order.

---

### 5.2 Quiz Engine (Second Most Important)

**Why it exists:** To test if student really understood a module before unlocking next. Code links `ModuleQuiz` — `Quiz` is separate from `QuestionBank`, so questions can be reused.

**Admin creates/manages quizzes:**
- `POST api/v1/quizzes` → `QuizService.CreateAsync` — needs `QuestionBankId, QuestionCount, TimeLimit, PassingPercentage, MaximumAttempts`. Checks `QuestionCount <= bank.QuestionCount` and if `DifficultyFilter` matches enough questions. Title unique. Status starts `Draft`.
- `QuizStatus`: `Draft → Published` (`PublishAsync` requires not Archived) → `Archived` → `Unpublish` back to Draft. Delete blocked if `ModuleQuizzes.Count>0`.
- `LinkToModuleAsync` requires quiz `Published` and caller owns path or is Admin — `QuizService.cs:211-252`.
- Question bank upload: `POST api/v1/questionbanks/upload` (Admin only, 6 MB limit, actually 5 MB in `JsonValidationService.cs:9`) → `QuestionBankService.UploadAsync` → `JsonValidationService.Validate` checks extension `.json`, size, JSON parse, `Title/Subject/Questions` required, each question needs `question, options>=2, correct_answer matches one option (case-insensitive), difficulty Easy/Medium/Hard, explanation, no duplicate question text` — `JsonValidationService.cs:12-115`. Same title+subject → new version `Version+1`. Frontend `AdminQuestionBanksPage.tsx:16-39` now shows `JSON_TEMPLATE` + `VALIDATION_RULES` dialog so admin knows format.

**How questions come from bank:**
- `QuestionBank` stores `StoredJson` + parses into `Questions` (each with `Options` where `IsCorrect` set by matching `CorrectAnswer` text) — `QuestionBankService.cs:270-304`.

**How student starts a quiz:**
- Frontend `QuizInstructions/Attempt pages` → `POST api/v1/quizzes/{quizId}/attempt?moduleId=X` → `AttemptService.StartAttemptAsync:25`. Checks quiz `Published`, `ModuleQuizzes` link exists, **unlock check** (same DAG+sequential as modules), checks existing attempts: if `InProgress/Created` → return same attempt, else if `Count >= MaximumAttempts` and already passed → block, else create `QuizAttempt {RandomSeed=Random.Shared.Next(), AttemptNumber, Status=InProgress}`.

**How questions are selected & randomized (actual mechanism):**
- `GetSelectedQuestionsAsync:351` loads `Questions.Include(Options).Where(QuestionBankId==quiz.Bank && Difficulty==filter) .OrderBy(Id)`.
- If `SelectionMode==Random` → `ShuffleList(allQuestions, seed).Take(QuestionCount)`. `ShuffleList` is **Fisher-Yates** with `new Random(seed)` — `for i=count-1 downto 1: j=rng.Next(0,i+1); swap(i,j)` — `AttemptService.cs:368-378`.
- Options shuffled per question with `ShuffleList(options, seed + QuestionId)` — deterministic per attempt, different per user/question.

**How answers are stored:**
- `PUT api/v1/attempts/{id}/answer` → `SaveAnswerAsync:134` upserts `StudentAnswer {QuizAttemptId, QuestionId, OptionId, AnsweredAt}`. If attempt was `Created` → set `InProgress`.

**How score is calculated:**
- `SubmitAttemptAsync:171` loads selected questions + `attempt.Answers`. For each question, finds `correctOption = Options.First(IsCorrect)` and counts if `answer.OptionId == correctOption.Id`. `Percentage = correctCount/total*100` (2 decimals), `TimeSpentSeconds = Now - StartedAt`. Sets `Score, Percentage, Passed`.

**How pass/fail decided:**
- `Passed = Percentage >= quiz.PassingPercentage` — `AttemptService.cs:210-213`. If passed, sets `Module.Status=Completed` and calls `ProgressService.MarkModuleCompleteFromQuizAsync` → may issue certificate.

**How attempts are tracked:**
- `QuizAttempt {UserId, QuizId, ModuleId, AttemptNumber, Status (Created/InProgress/Evaluated), Score, Percentage, Passed}`. Max attempts enforced per `quiz.MaximumAttempts`. Best attempt via `MaxBy(Percentage)`.

**How quiz connects to module:**
- Junction `ModuleQuizzes {ModuleId, QuizId, AssignedBy, Active}`. One module has at most one active quiz (code upserts). Quiz must be published before linking.

**How completing/passing affects progress:**
- Only **passing** triggers `MarkModuleCompleteFromQuizAsync`. Failing leaves module not completed → next module stays locked.

**Important validations:**
- Quiz title unique, QuestionCount >0 and <= bank count, DifficultyFilter count, quiz Published before link, module unlocked before start, attempt not already submitted, option belongs to question (checked via correct-answer match).

**Services involved:** `QuestionBankService`, `QuizService`, `AttemptService`, `ProgressService`, `JsonValidationService`, Controllers `QuestionBankController`, `QuizController`, `AttemptController`.

**Frontend flow:** `AdminQuestionBanksPage` (upload) → `QuizInstructionsPage` (shows time limit, passing %) → `AttemptPage` (renders `AttemptQuestionDto` shuffled options, `SaveAnswer` on click) → `ResultPage` (score, Passed) → `ReviewPage` (shows `CorrectOptionId` vs `SelectedOptionId`).

---

### 5.3 AI Assignment Feedback

**What is actually implemented (not exaggerated):**
- **NvidiaProvider is working**, `GeminiProvider.cs` exists but **not registered** — `Program.cs:234` does `AddHttpClient<IAiProvider,NvidiaProvider>` only. So only Nvidia Nemotron is live. Mark as **PARTIALLY IMPLEMENTED / single provider**.

**What triggers it:**
- **Manual by admin only** — `POST api/v1/submissions/{id}/ai-feedback` → `AiFeedbackService.GenerateAsync`. Checks `IsAdmin` via `UserClassrooms` where `Role==Admin` for that classroom. Students cannot trigger. **NOT automatic on submit.**

**What information is sent:**
- `ReadSubmissionTextAsync` reads file from disk via `FileStorageService`. Only **TXT** (read directly) and **PDF** (via `UglyToad.PdfPig` extracting `p.Text` per page) are supported. Other extensions → `InvalidOperationException("not supported for {ext}, use TXT or PDF")` — `AiFeedbackService.cs:198-231`.
- Sends `AiFeedbackRequestDto {SubmissionId, AssignmentQuestion=Title, AssignmentDescription, AssignmentRubric="", StudentSubmission=extractedText}` → `PromptBuilder.BuildPrompt(request)` builds structured prompt.

**What the AI service does:**
- `IAiProvider.SendAsync({Prompt, TimeoutSeconds})` via `NvidiaProvider` HTTP POST to `https://integrate.api.nvidia.com/v1/chat/completions` with model from `AiOptions` (e.g., `nvidia-nemotron-nano-9b-v2`). Config from env `LEARNPATH_NVIDIA_API_KEY` via `Program.cs:56`. If `ApiKey` missing → throw "API key is not configured".

**What comes back:**
- `providerResponse.Content` raw text → `AiResponseParser.Parse` extracts `AiFeedbackResponseDto {Summary, GrammarFeedback, RubricCoverage, MissingTopics, SuggestedScore{Percentage, Marks}, OverallRecommendation, Disclaimer}`. Checks `Summary` not empty else "incomplete response".

**Where stored:**
- `SubmissionAiFeedback {SubmissionId (unique), Summary, GrammarFeedback, RubricCoverage, MissingTopics, SuggestedScorePercentage/Marks, OverallRecommendation, Disclaimer, RawResponse, GeneratedAt}` — `ApplicationDbContext.cs:40`. Upsert: if exists update, else add.

**How student/admin sees it:**
- **Admin-only** — `GetAsync` also checks `IsAdmin`. Student sees AI feedback only **after admin publishes evaluation**: `ClassroomService.GetMySubmissionAsync:595` only attaches `AiFeedback` if `submission.PublishedAt != null`. So flow: Admin Generate → Grade → Publish → then student fetch includes `AiFeedback`.

**Validation/error handling:**
- Admin check, ApiKey missing, no extractable text, provider `Success=false` → log warning + throw `InvalidOperationException(providerResponse.ErrorMessage)`, incomplete summary → throw "incomplete response, try again". Errors bubble via `ExceptionMiddleware` → `400/500`.

**Classes:** `AiFeedbackService`, `PromptBuilder`, `NvidiaProvider`, `AiResponseParser`, `AiOptions`, `SubmissionAiFeedback`, `IAiProvider`.

**What is interface only:** Gemini provider interface prepared but not wired; no auto-trigger on submission upload; no student self-service.

---

### 5.4 Audit Logging (Hard Part — Explain From Start)

**In simple words:** Audit log is a diary. Every important action in the app writes one line — who did what, to which thing, when. And once written, no one can change or delete it.

**Why needed:** To know who created a path, who changed a quiz, who graded a submission, especially for Admin to review history. Without it you can't prove what happened.

**What an audit log represents (actual entity):**
`Entities/AuditLog.cs:3` — `Id, Timestamp (UtcNow at creation), UserId, Username, Role, ActionType (enum AuditAction), EntityType ("User"/"LearningPath"/"Module"/"Quiz"...), EntityId, Description ("Module 'OOP' published..."), OldValue, NewValue, AdditionalData ("PathId: 5")`.

**What information is stored:** All 11 fields above. `Timestamp` auto set in constructor `DateTime.UtcNow`.

**How an action becomes a record:**
1. User makes request → Controller calls Service (e.g., `LearningPathService.PublishModuleAsync`)
2. Service does business logic + `SaveChangesAsync`
3. Immediately after, calls `await _auditLog.LogAsync(AuditAction.MODULE_PUBLISHED, "Module", module.Id.ToString(), $"Module '{title}' published...", additionalData: $"PathId: {pathId}")` — `LearningPathService.cs:554`
4. `AuditLogService.LogAsync:23-49` resolves actor from `IHttpContextAccessor.HttpContext.User` (NameIdentifier, Email, Role) unless passed explicitly, then `AddAsync(new AuditLog(...))` + `SaveChangesAsync`.

**Which service handles it:** `Services/Audit/AuditLogService.cs` injected as `IAuditLogService` into 12+ services: `AuthService, LearningPathService, ClassroomService, ProgressService, QuizService, AttemptService, QuestionBankService, CommunityService, AiFeedbackService, AdminService` etc.

**How different parts use it — representative examples (not hundreds):**
- `AuthService.RegisterAsync:52` → `USER_CREATED` with `User/Id/registered`
- `LearningPathService.AddDependencyAsync:715` → `DEPENDENCY_ADDED` with `ModuleDependency/ModuleId:DependsOn`
- `ClassroomService.CreateAsync:167` → `CLASSROOM_CREATED` with `Classroom/Id/LearningPathId`
- `AttemptService.StartAttemptAsync:104` → `QUIZ_STARTED` with `Quiz/Id/ModuleId`
- `AiFeedbackService.GenerateAsync:145` → `AI_FEEDBACK_GENERATED`
- All include `userId` resolved from current JWT.

**How history is retrieved/displayed:**
- **My history:** `GET api/v1/history/me` → `AuditLogService.GetMyHistoryAsync(userId)` filters `Where(UserId==myId)` with `Page/PageSize (max 100)`, filters by `ActionType, FromDate, ToDate, Search`, sorting newest first.
- **Admin history (SuperAdmin only):** `GET api/v1/history/admin` → `GetAdminHistoryAsync` checks `IsSuperAdmin` via `Users.IsSuperAdmin` — `AuditLogService.cs:89-113`. Returns all logs with filters plus `UserId/Role/EntityType`. Also resolves `EntityExists` by batch querying per entity type (`LearningPath, Classroom, Group, Post, Quiz, User`) — `ResolveEntityExistenceAsync:236`.
- Frontend: `historyService` → `HistoryPage` / `AdminPage` audit tab (check).

**How system identifies user/action/entity:**
- User via `ClaimTypes.NameIdentifier` (User Id from JWT) + `ResolveActor` fallback to HttpContext. Action via `AuditAction` enum (e.g., `LEARNING_PATH_CREATED = 10`). Entity via `EntityType` string + `EntityId` string.

**Important design decision:**
- **Append-only immutability** — `ApplicationDbContext.cs:62-75` overrides `SaveChanges/SaveChangesAsync` to check `ChangeTracker.Entries<AuditLog>().Any(Modified|Deleted)` → throw `InvalidOperationException("Audit logs are append-only...")`. No one, not even SuperAdmin code path, can edit/delete history.

**Simple flow to tell:**
> USER ACTION → service does the work → calls AuditLogService.LogAsync → new AuditLog saved → later I or SuperAdmin open History page → GET /history/me or /history/admin → see list with time, who, what.

---

## 6. Authentication / Role Management (Only Actually Implemented)

**Registration/login flow (verified):**
- `POST api/v1/auth/register` → `AuthService.RegisterAsync` → check `FindByEmailAsync` exists → `Map<User>(dto)` → `UserManager.CreateAsync(user,password)` hashes via Identity PBKDF2 → `AddToRoleAsync(user,"Student")` → `LogAsync USER_CREATED` → `BuildAuthResponseAsync` → generate `AccessToken` (HS256, 60m, claims sub/email/jti/firstName/lastName + Role) via `JwtTokenGenerator.GenerateAccessToken` + `GenerateRefreshToken` (64 random bytes base64) → save `RefreshToken {UserId, Token, ExpiresAt=Now+7days}` → return `{AccessToken, RefreshToken, UserId, Email, FirstName, LastName, Roles}`.
- `POST api/v1/auth/login` → find by email → `CheckPasswordAsync` → check `UserStatus` (Deleted/Inactive/Invalid) → get roles → log `LOGIN` or `LOGIN_FAILED` with reason → same `BuildAuthResponseAsync`.
- **Password rules** `Program.cs:111` — `RequireDigit, RequireLowercase, RequireUppercase, RequiredLength=8, RequireNonAlphanumeric=false, RequireUniqueEmail=true` + lockout `5 attempts → 5 min`.

**ASP.NET Identity:** `IdentityDbContext<User>` in `ApplicationDbContext`, `AddIdentity<User,IdentityRole>` with EF stores, manages users/roles/password hashing — `Program.cs:111`.

**JWT:** `JwtSettings` (`Secret/Issuer/Audience/ExpiryMinutes=60`), `AddAuthentication JwtBearer` with `ValidateIssuer/Audience/Lifetime/IssuerSigningKey` and `ClockSkew=Zero` — `Program.cs:136-149`. Tokens are stateless.

**Auth vs Authz:** Authentication = who you are (JWT proves). Authorization = what you can do (`[Authorize]`, `[AllowAnonymous]`, `AddPolicy("AdminOnly", p => p.RequireRole("Admin"))`).

**How backend knows who user is:** `JwtTokenGenerator` puts `sub=user.Id`, `email`, `ClaimTypes.Role` into token. Middleware validates, sets `HttpContext.User`. Controllers use `User.FindFirstValue(ClaimTypes.NameIdentifier)!` for `userId`.

**How roles get different access:**
- **Student** default on register. **Admin** seeded by `RoleSeeder` + `AdminSeeder` (`admin@learnpath.com / Admin@123`). Roles assigned via `UserManager.AddToRoleAsync`.
- Backend: `[Authorize(Roles="Admin")]` on `AdminController`, `QuestionBankController` upload, `HistoryController` admin; `AdminOnly` elsewhere; services also check `IsAdminAsync` via `UserRoles` join.
- Frontend: `useAuth.ts:26` `roles.includes(ROLES.ADMIN)`, `AdminRoute.tsx`/`ProtectedRoute.tsx` guard `AppRoutes.tsx` (34 lazy routes).

**JWT lifecycle:** Access 60m, Refresh 7d, rotation — `RefreshAsync` finds token `!IsRevoked && ExpiresAt>UtcNow`, marks old `IsRevoked=true`, saves, returns new pair. `RevokeAsync` revokes all user tokens. Frontend `apiClient.ts:20-86` queues 401s with `isRefreshing+failedQueue`, calls `POST /api/v1/auth/refresh` via raw `axios`, updates `tokenUtils`, retries.

---

## 7. Classroom + Assignment (Real Workflow Verified)

1. **Admin creates classroom** → `POST api/v1/classrooms` with `LearningPathId` → `ClassroomService.CreateAsync` checks path exists, creates `Classroom {InviteCode=Guid(8).ToUpper(), CreatedById}` and auto-joins the creator as a member.
2. **Students join** → `POST api/v1/classrooms/{id}/join` with invite code or `POST /join` via code → `JoinAsync` finds by `InviteCode`, checks not already member, and creates a `UserClassroom` membership without a separate classroom role.
3. **Admin creates assignment** → `POST api/v1/classrooms/{id}/assignments` → `CreateAssignmentAsync` requires platform `Admin` plus ownership of the classroom (`CreatedById`) and creates `Assignment {Title, Description, DueDate, ClassroomId}`.
4. **Student submits** → `POST api/v1/submissions` multipart `IFormFile` → `UploadSubmissionAsync:439` checks `EnsureAssignmentUnlockedAsync` (previous `Grade>=5`), `FileValidationService.Validate`, `FileStorageService.StoreAsync` to `uploads/assignments/`, handles existing `SubmittedAgain` vs `Submitted`, sets `IsLate=DueDate<Now`, status `Submitted`. If DB fails → deletes stored file. Can delete own `Submitted` via `DeleteSubmissionAsync`.
5. **Admin grades** → `GET /submissions?assignmentId` list, `TransitionStatusAsync` (Submitted→UnderReview→Reviewed), `GradeSubmissionAsync` sets `Grade/Feedback/Status=Graded`, `PublishEvaluationAsync` sets `PublishedAt` (requires `Graded`).
6. **AI feedback (if txt/pdf)** → admin calls `POST /submissions/{id}/ai-feedback` after publish → stored as above.
7. **Student sees** → `GetMySubmissionAsync` returns `Status, Grade, Feedback, AiFeedback` only if `PublishedAt != null`.

---

## 8. Backend + Frontend Integration (High-Level with Real Example)

**Generic flow:** React component → Redux thunk → `*Service.ts` → `apiClient` (Axios, Bearer) → Nginx:80 `/api/*` → ASP.NET Core Middleware (Exception→Logging→RateLimit→Cors→Auth) → Controller (`[Authorize]`) → Service → `ApplicationDbContext` (EF Core) → SQL Server (37 tables) → `ApiResponse<T>` (success/message/data) → Axios → Redux slice → Component re-render (MUI).

**Example 1 — Student opens Module:**
`LearningPathDetailPage.tsx` `ModuleCard` click → `navigate(/paths/5/modules/10)` → `LessonPage` dispatches `pathService.getModuleContent(5,10)` → `apiClient.get("/paths/5/modules/10")` adds `Authorization: Bearer <60m token>` → `LearningPathController.GetModuleContent` → `LearningPathService.GetModuleContentAsync` (checks `IsUnlocked`) → EF query `Modules.Include(Dependencies).Include(Progresses.Where(userId))` → if locked → throw → `ExceptionMiddleware` → `403 ApiResponse.Fail(...)` → frontend `catch` shows Snackbar. If unlocked → `200 Ok(ApiResponse<ModuleResponseDto>)` → render.

**Example 2 — Question bank upload:**
`AdminQuestionBanksPage` file input → `questionBankService.upload(file)` builds `FormData` → `apiClient.post("/questionbanks/upload", formData)` → `QuestionBankController.Upload` (`[Authorize(Roles="Admin")]`, `[RequestSizeLimit(6MB)]`) → `QuestionBankService.UploadAsync` → `JsonValidationService.Validate` → if errors → return `QuestionBankUploadResult{Success=false,Errors}` → Controller `400 BadRequest(errors)` → frontend shows `errors.join("; ")` in Alert.

---

## 9. Major Problems I Faced (Strongest for 5-Min Story)

Select 3-4 of these — each is Problem → what needed → what was hard → what changed → Result.

**P1 — Confusing error handling (you listed)**
- Needed: user understands what went wrong.
- Hard: every service threw different exceptions, frontend got raw SQL/validation text.
- Changed: `ApiResponse<T>` wrapper + `ExceptionMiddleware` mapping + `InvalidModelStateResponseFactory` to return same envelope + `QuestionBank` returns `ValidationError[]` with `QuestionIndex/Reason/Detail`.
- Result: Single format, frontend always shows human message.

**P2 — Deletion leaving orphan data**
- Needed: delete path without breaking FK.
- Hard: SQL Server `EnableRetryOnFailure` requires execution strategy + transaction; cascade vs restrict mix caused `547 FK violation`.
- Changed: `LearningPathService.DeleteAsync` uses `CreateExecutionStrategy().ExecuteAsync` + `BeginTransactionAsync` + explicit 7-step delete in FK-safe order + checks `classroomCount/certificateCount` before.
- Result: Safe atomic delete, clear error if still referenced.

**P3 — DAG was not enforcing lock (you listed)**
- Needed: student cannot jump ahead.
- Hard: DAG logic existed but UI showed all modules, API allowed bypass via direct call, assignments had separate sequential rule not connected to DAG.
- Changed: Added dual check `dagUnlocked && sequentialUnlocked` in `LearningPathService` + `AttemptService` + `ClassroomService.EnsureAssignmentUnlocked`, threw `UnauthorizedAccessException` → `403`, frontend `ModuleCard` uses `isUnlocked` to disable + Snackbar, assignment hides `description` if locked.
- Result: True prerequisite enforcement end-to-end.

**P4 — Question bank correct file rejected & no guidance (you listed)**
- Needed: admin knows correct JSON shape.
- Hard: `JsonValidationService` was strict (exact `correct_answer` match, difficulty case-insensitive but limited to `easy/medium/hard`, explanation required, duplicate detection) but frontend gave generic "Upload failed".
- Changed: Backend returns `List<ValidationError>` with per-question index. Frontend `AdminQuestionBanksPage.tsx` adds `JSON_TEMPLATE` + `VALIDATION_RULES` (8 rules) with syntax-highlighted block and copy button, shows `serverMsg.errors.join("; ")`.
- Result: Admin can self-correct format, fewer failed uploads.

**P5 — Community felt like plain comments**
- Needed: feel like real post+code sharing.
- Hard: initial post was `Title+Content` only, hard to discover.
- Changed: Added `CodeSnippet/ProgrammingLanguage/Tags/Category/LearningPathId/GroupId`, votes (`PostVote/CommentVote` toggle logic `±1/±2`), trending `CalculateTrendingScore = score / (ageHours+2)^1.5`, filters `search/tag/category/sort/filter`, pagination via `CommunityRepository.PagedResult`, `requestId + AbortSignal` to fix race on fast filter.
- Result: Searchable, social-like feed.

**P6 — Audit logging was hard (you noted)**
- Needed: who did what, tamper-proof.
- Hard: ensuring every service logs consistently and history can't be edited/deleted, plus resolving who the actor is when service called without explicit userId.
- Changed: Central `AuditLogService` with `ResolveActor` from `HttpContext.User`, every service injects it, `ApplicationDbContext.GuardAuditLogImmutability` blocks `Modified/Deleted` on `AuditLog`, history split into `GetMyHistoryAsync` (user filter enforced) vs `GetAdminHistoryAsync` (SuperAdmin only, with `EntityExists` batch check).
- Result: Complete, append-only history.

Pick the strongest 3-4 that fit your personal contribution (see below).

---

## 10. What I Personally Contributed (Be Honest)

**Needs personal clarification** — code doesn't record who typed what. From evolution note, you built from scratch and added Quiz Engine, Audit Logging, Role management later.

**How to say:** "I built LearnPath from scratch. Early I did paths/modules + auth. As it grew I personally added the DAG unlocking with sequential fallback, the quiz engine with random seed shuffling, and the audit logging with append-only guard. I also integrated AI feedback via Nvidia and the classroom assignment flow. Community and some analytics I did with guidance but I can explain the flow."

**If asked "what was hardest for you personally?"** pick the ones you actually coded — e.g., audit immutability and DAG cycle check.

---

## 11. What I Learned (Keep Human)

- Designing a graph is easy on paper, hard in code — need both explicit dependencies and a simple fallback order.
- One API format (`ApiResponse`) saves days of frontend error handling.
- Transactions matter — without ordered delete, database leaves orphans.
- Showing the expected file format in the UI reduces support more than backend validation alone.
- Centralizing cross-cutting logic (AuditLogService, ExceptionMiddleware, RateLimit) keeps controllers thin.

---

## 12. Overall Architecture — Simple Interview Level

```
React (MUI + Redux + Router + Axios)  ←→  Nginx (80) proxies /api → ASP.NET Core (5000)
                                                |
                                                Middleware: Exception | Logging | RateLimit 50/min (X-RateLimit-*) | CORS | Auth
                                                |
                                         Controllers (api/v1/*) + FluentValidation + ApiResponse
                                                |
                                         Services (23) → DbContext (30 DbSets + Identity) → SQL Server (37 tables)
                                                |
                                         JWT HS256 60m + Refresh 7d, AutoMapper, Identity lockout 5/5m
```
- Frontend and backend talk only via REST JSON over HTTPS with Bearer token.
- All business rules live in Services; Controllers only validate and delegate.

---

## 13. Likely Follow-Ups & Short Answers

- **Why DAG not linear?** → Linear forces everyone same order; DAG lets A depend on B+C, or no dependency so you can start in parallel, yet still prevent cycles.
- **How do you randomize but still review?** → Store `RandomSeed` per attempt, use it to `ShuffleList` both times — same seed gives same question and option order on review (`seed + QuestionId`).
- **Why not let frontend alone lock modules?** → Frontend can be bypassed; backend must enforce with `403` so API call directly also fails.
- **What if token stolen?** → Access short 60m, refresh rotated (old revoked), stored hashed, `ClockSkew=Zero` for strict expiry; ideally add HTTPS + httpOnly cookie (current uses Bearer header + `tokenUtils` localStorage).
- **What does audit `EntityExists` do?** → Shows in admin history if the referenced path/post still exists or was deleted — `ResolveEntityExistenceAsync` batch queries per type.

---

## 14. Verification Checklist (All Supported by Code)

- [ ] DAG adjacency + `WouldCreateCycle` DFS — `DagValidator.cs:5`
- [ ] `IsUnlocked` dual check — `LearningPathService.cs:74`
- [ ] 403 on locked module — `LearningPathService.cs:360`
- [ ] Progress → Certificate at 100% — `ProgressService.cs:203`
- [ ] Question bank 5 MB + 8 rules — `JsonValidationService.cs:9` + `AdminQuestionBanksPage.tsx:30`
- [ ] Quiz seed + Fisher-Yates — `AttemptService.cs:368`
- [ ] AI only TXT/PDF + Nvidia only — `AiFeedbackService.cs:198` + `Program.cs:234`
- [ ] Audit append-only — `ApplicationDbContext.cs:62`
- [ ] Classroom 8-char code + sequential assignment lock — `ClassroomService.cs:1114` + `1058`
- [ ] Rate limit 50/min headers — `RateLimitingMiddleware.cs:13`
- [ ] Frontend locked UI (opacity/Chip/Snackbar) — `LearningPathDetailPage.tsx:26`

> Do NOT invent: no career recommendation, no roadmap generation, no chatbot, no real PDF certificate, no Gemini live, no SignalR.

