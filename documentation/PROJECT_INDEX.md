# LearnPath — Project Navigation Index

> **Root:** `D:\FInal Project\LearnPath` | **Verified:** 2026-08-28 from codebase | **Workflow:** `Question → §4/§8 Lookup → Feature → Files → Inspect Only Those Files`
> **Stacks:** React 18 + Vite + MUI 5 + Redux Toolkit + Router 6 + Axios | .NET 10 + EF Core + Identity + JWT + AutoMapper + FluentValidation | SQL Server 2022

---

## 1. Project Structure (meaningful only)

```
backend/                          # .NET 10 API (LearnPath.API.csproj, net10.0)
├── Program.cs (316) + appsettings.json/.Development.json + Dockerfile
├── Controllers/      17 controllers (api/v1/*)
├── Services/         23 services (Admin/Ai/Analytics/Auth/Attempt/Audit/Classroom/Community/Dashboard/LearningPath/Notification/Progress/QuestionBank/Quiz/Submission/User/Validation)
├── Data/             ApplicationDbContext.cs + Seeders/RoleSeeder.cs, 11 Migrations + Snapshot
├── Entities/         41 entities, Configurations/ 31 IEntityTypeConfiguration, Repositories/
├── DTOs/             28, Interfaces/, Mappings/, Middleware/ (Exception/RateLimit/Logging), Validators/12, Swagger/, Algorithms/Graph/
└── uploads/assignments/

backend.Tests/                    # xUnit + Moq + InMemory + FluentAssertions (16 files)
├── Algorithms/DagValidatorTests.cs, Services/*, Controllers/AdminControllerTests.cs, Helpers/DbContextFactory.cs

frontend/                         # React Vite SPA
├── src/app/          provider.tsx (Redux+Theme+Router), store.ts
├── src/pages/        36 lazy pages (auth/classroom/community/dashboard/learningPaths/quiz/admin/etc)
├── src/components/   ~34 (community PostCard/CommentItem, classroom, dashboard, learningPath PathGraph, quiz, analytics)
├── src/redux/        store.ts 7 reducers, slices/7 (auth/path/classroom/community/analytics/dashboard/notification), selectors/7
├── src/services/     14 apiClient + auth/path/classroom/quiz/questionBank/progress/community/notification/certificate/analytics/admin/user/history
├── src/routes/       AppRoutes.tsx 34 lazy routes, ProtectedRoute/AdminRoute/GuestRoute
├── src/theme/        palette.ts (light/dark), index.ts getAppTheme(), typography.ts + context/ThemeModeContext.tsx
├── src/hooks/        useAuth, useDebounce, useCommunity, useGroups
├── src/constants/    routes.ts (40), roles.ts | layouts/4 | types/ | utils/tokenUtils | validations/
└── vite.config.ts, vitest.config.ts, nginx.conf (SPA fallback + /api/→backend:5000), Dockerfile

documentation/  PROJECT_INDEX.md (this) + interview-preparation/
features/       15 markdown (01_Authentication…15_API_Platform) | interview-preparation/dotnet/
.github/        workflows/ci.yml (dotnet test + npm build) / cd.yml
docker-compose.yml (sql:2022, api:5000, frontend:80), README.md, PROJECT_ANALYSIS.md
```

**Ignore:** `frontend/node_modules/`, `frontend/dist/`, `backend/bin/`, `backend/obj/`, `backend/.vs/`, `graphify-out/`

---

## 2. Architecture

```
Browser: React (MUI + Redux + Router + Axios VITE_API_BASE_URL? http://localhost:5000/api/v1)
  ↓
Nginx (:80) — /api/* proxy → ASP.NET Core (:5000)
  ├─ Middleware: Exception→Logging→RateLimit(50/min)→Cors(LearnPathCors)→Auth
  ├─ Controllers [Authorize] + FluentValidation + ApiResponse<T> {success,message,data,errors}
  ├─ Services (business + AuditLog guard) → DbContext direct / CommunityRepository
  ├─ EF Core: 30 DbSets + Identity + 31 Configurations → SQL Server (37 tables)
  └─ JWT HS256 60m/7d, AutoMapper, Identity lockout 5/5m
```

| Tier | Location | Role |
|------|----------|------|
| UI | `frontend/src/pages/`+`components/` | Pages, hooks, lazy load |
| State | `frontend/src/redux/` | Toolkit thunks, selectors |
| Transport | `frontend/src/services/apiClient.ts` | Bearer, 401 queue (`isRefreshing`/`failedQueue`) |
| API | `backend/Controllers/` | `api/v1/` |
| Domain | `backend/Services/*/` | Auth/LearningPath/Classroom/Quiz/Attempt/Progress/Community/Ai… |
| Data | `backend/Data/ApplicationDbContext.cs` | IdentityDbContext<User>, GuardAuditLogImmutability |
| Config | `backend/Program.cs` | DI, JWT `ClockSkew=Zero`, CORS, Seeders |

---

## 3. Feature → File Mapping

**Primary = direct impl; Supporting = deeper dive**

```
Authentication (Login/Register/Refresh/Revoke)
 Primary: Controllers/AuthController.cs, Services/Auth/AuthService.cs, Authentication/Jwt/JwtTokenGenerator.cs
 Support: DTOs/Auth/*, Mappings/AuthMappingProfile, Program.cs JWT, frontend LoginPage.tsx/RegisterPage.tsx, services/authService.ts, utils/tokenUtils.ts, redux/authSlice

RBAC (Admin/Student)
 Primary: Data/Seeders/RoleSeeder.cs, Program.cs AddAuthorizationBuilder (AdminOnly)
 Support: Controllers/AdminController.cs, Services/Admin/AdminService.cs, constants/roles.ts, routes/AdminRoute.tsx

Learning Path + DAG
 Primary: Controllers/LearningPathController.cs, Services/LearningPath/LearningPathService.cs, Algorithms/Graph/DagValidator.cs
 Support: Entities/LearningPath.cs, Module.cs, ModuleDependency.cs, utils/graphUtils.ts, components/PathGraph.tsx, pages/LearningPathsPage.tsx

Classroom + Enrollment
 Primary: Controllers/ClassroomController.cs, Services/Classroom/ClassroomService.cs
 Support: Entities/Classroom.cs, UserClassroom.cs, Assignment.cs, frontend ClassroomPage.tsx/ClassroomDetailPage.tsx

Assignment + Submission + Files
 Primary: Controllers/SubmissionController.cs, Services/Submission/FileValidationService.cs+FileStorageService.cs
 Support: Entities/Assignment.cs, Submission.cs (SubmissionAiFeedback), uploads/assignments/, components/AssignmentCard.tsx

Quiz + QuestionBank + Attempt
 Primary: Controllers/QuizController.cs, QuestionBankController.cs, AttemptController.cs, Services/Quiz/QuizService.cs, QuestionBankService.cs, Attempt/AttemptService.cs
 Support: Validators/JsonValidationService.cs (6MB upload), Entities/QuestionBank/Quiz/Question/Option/QuizAttempt/StudentAnswer, pages/quiz/*Attempt/Result/Review*

Progress + Certificate
 Primary: Controllers/ProgressController.cs, CertificateController.cs, Services/Progress/ProgressService.cs
 Support: Entities/Progress.cs, Certificate.cs, pages/LessonPage.tsx

AI Feedback
 Primary: Services/Ai/AiFeedbackService.cs → PromptBuilder.cs → NvidiaProvider.cs → AiResponseParser.cs
 Support: Configuration/AiOptions.cs, Program.cs AddHttpClient<IAiProvider,NvidiaProvider>, GeminiProvider.cs (dead code)

Community (Posts/Comments/Votes/Groups/Moderation)
 Primary: Controllers/CommunityController.cs (30+ routes), Services/Community/CommunityService.cs, Repositories/CommunityRepository.cs
 Support: Entities/Post/Comment/PostVote/CommentVote/Group/GroupMember/Report, frontend pages/community/*, redux/communitySlice.ts

Analytics/Dashboard
 Primary: Controllers/AnalyticsController.cs, DashboardController.cs, Services/AnalyticsService.cs, DashboardService.cs
 Support: pages/AnalyticsPage.tsx/DashboardPage.tsx, components/analytics/*Charts

Admin + Audit Log
 Primary: Controllers/AdminController.cs, HistoryController.cs, Services/Admin/AdminService.cs, Services/Audit/AuditLogService.cs
 Support: Entities/AuditLog.cs (GuardAuditLogImmutability in DbContext), pages/admin/*

Notification
 Primary: Controllers/NotificationController.cs, Services/NotificationService.cs, Entities/Notification.cs
 Support: components/dashboard/Topbar.tsx, services/notificationService.ts, redux/notificationSlice

Theming
 Primary: theme/palette.ts (lightPalette/darkPalette), theme/index.ts getAppTheme(), context/ThemeModeContext.tsx
 Support: app/provider.tsx ThemedApp, pages/SettingsPage.tsx (Night Mode)

Settings/Profile + Change Password
 Primary: Controllers/UserController.cs PUT me/password, Services/User/UserService.cs, pages/SettingsPage.tsx (Change Password Collapse), pages/ProfilePage.tsx
 Support: services/userService.ts changePassword()
```

---

## 4. Quick Lookup (keyword → files)

```
JWT / login / Bearer / refresh
→ AuthController.cs, AuthService.cs, JwtTokenGenerator.cs, Program.cs:136, apiClient.ts, tokenUtils.ts

DagValidator / WouldCreateCycle / TopologicalSort / ModuleDependency
→ DagValidator.cs, LearningPathService.cs, ModuleDependency.cs, graphUtils.ts, PathGraph.tsx

Classroom / inviteCode / UserClassroom / enrollment
→ ClassroomController.cs, ClassroomService.cs, Classroom.cs, UserClassroom.cs

Assignment / Submission / uploads / FileStorage
→ SubmissionController.cs, FileValidationService.cs, FileStorageService.cs, Assignment.cs, Submission.cs

Quiz / QuestionBank / QuizAttempt / StudentAnswer / questionbanks/upload
→ QuestionBankController.cs, QuizController.cs, AttemptController.cs, QuizService.cs, AttemptService.cs, JsonValidationService.cs

AI feedback / Nvidia / PromptBuilder / Gemini
→ AiFeedbackService.cs, PromptBuilder.cs, NvidiaProvider.cs, AiResponseParser.cs, AiFeedbackSection.tsx

Community / Post / Group / Report / vote / pin
→ CommunityController.cs, CommunityService.cs, CommunityRepository.cs, Post.cs, Group.cs

Progress / Certificate / unlock
→ ProgressService.cs, CertificateController.cs, Progress.cs, Certificate.cs

Admin / AuditLog / GuardAuditLogImmutability
→ AdminController.cs, HistoryController.cs, AuditLogService.cs, ApplicationDbContext.cs

RateLimit / 429 / X-RateLimit
→ RateLimitingMiddleware.cs

ApiResponse / ExceptionMiddleware / 409/403/404
→ Common/ApiResponse.cs, Middleware/ExceptionMiddleware.cs

Theme / Night Mode / darkPalette
→ palette.ts, index.ts getAppTheme, ThemeModeContext.tsx, SettingsPage.tsx

Change Password / users/me/password
→ UserController.cs, UserService.cs, userService.ts, SettingsPage.tsx

Analytics / Dashboard / WeeklyBarChart / PathProgressChart
→ AnalyticsController.cs, DashboardController.cs, WeeklyBarChart.tsx, PathProgressChart.tsx
```

---

## 5. Important API Mapping (Controller → Service)

| Controller (`api/v1/`) | Service | Key Routes |
|---|---|---|
| Auth `auth` | AuthService | `POST register|login|refresh (Anon)`, `POST revoke` |
| User `users` | UserService | `GET me`, `PUT me`, `PUT me/password` |
| LearningPath `paths` | LearningPathService | `GET public(Anon)`, `GET my`, `GET {id}`, `POST|PUT|DELETE`, `POST {pathId}/modules`, `POST {pathId}/dependencies` |
| Quiz / Attempt / QuestionBank | QuizService, AttemptService, QuestionBankService | `POST questionbanks/upload (6MB)`, `POST modules/{id}/quiz`, `POST quizzes/{id}/attempt`, `PUT attempts/{id}/answer` |
| Classroom `classrooms` | ClassroomService | `GET|POST`, `GET {id}`, `POST {id}/join`, `POST {id}/assignments` |
| Submission `submissions` | FileValidation/Storage | `POST`, `GET {id}`, `POST {id}/grade|status` |
| Community `community` | CommunityService | `GET posts|groups|reports`, `POST posts|comments|groups`, votes/bans/pins |
| Progress / Certificate / Dashboard / Analytics / Notification / History / Admin / Health | respective Services | `POST progress/complete`, `GET certificates|dashboard|analytics|notifications`, `GET history`, `GET admin/*` |

---

## 6. Database Mapping

**DbContext:** `backend/Data/ApplicationDbContext.cs` = `IdentityDbContext<User>` + 30 `DbSet<>` + `GuardAuditLogImmutability()` + `ApplyConfigurationsFromAssembly`

**Entities:** `backend/Entities/` (41)  **Configs:** `backend/Configurations/` (31)  **Migrations:** `backend/Migrations/` (11 + Snapshot)

```
User ─┬─ RefreshToken 1:N
      ├─ LearningPath 1:N ── Module 1:N ─┬─ ModuleDependency (From→To self-ref, DAG)
      │                                  ├─ ModuleResource/Tag/Objective
      │                                  └─ ModuleQuiz ── Quiz ── Question ── Option
      ├─ Progress (User+Module)
      ├─ Classroom (owner) ── UserClassroom M2M ── Assignment ── Submission ── SubmissionAiFeedback
      ├─ Certificate (User+LearningPath)
      ├─ Post ── Comment (ParentCommentId self) ── PostVote/CommentVote, Group ── GroupMember
      ├─ Report, Notification, AuditLog (append-only)
      └─ AspNet* Identity (7 tables)

QuizAttempt → StudentAnswer; Assignment.ClassroomId; Post.GroupId; Seeder → RoleSeeder (Admin/Student) + AdminSeeder admin@learnpath.com / Admin@123
```

---

## 7. Frontend Mapping (Feature → Page → Service → State)

```
Auth            → LoginPage/RegisterPage → authService → authSlice + useAuth + tokenUtils
Learning Path   → LearningPathsPage/LearningPathDetailPage/LessonPage → pathService → pathSlice
Classroom       → ClassroomPage/ClassroomDetailPage/AssignmentDetailPage → classroomService → classroomSlice
Quiz/Attempt    → QuizInstructions/Attempt/Result/Review → quizService + questionBankService → (attempt thunks)
Community       → CommunityPage/PostPage/Groups/GroupDetail/Moderation → communityService → communitySlice + useCommunity/useGroups
Analytics       → AnalyticsPage/DashboardPage → analyticsService → analyticsSlice/dashboardSlice + Recharts
Admin           → AdminPage/Users/History/etc → adminService/historyService → (direct)
Theming         → SettingsPage (Night Mode) → ThemeModeContext (learnpath-theme-mode) → getAppTheme()
Profile/Settings→ ProfilePage / SettingsPage → userService.changePassword → (no slice, direct)
```

Routes: `frontend/src/routes/AppRoutes.tsx` 34 lazy + `ProtectedRoute`/`AdminRoute` ; Constants `routes.ts` (40)

---

## 8. Important Classes / Methods (navigation help only)

```
JwtTokenGenerator.cs → Generate(User) :HS256, sub/email/jti/Role, 64b refresh
AuthService.cs → RegisterAsync(), LoginAsync(), RefreshAsync(), RevokeAsync() (checks UserStatus.Deleted, Password, Audit)
DagValidator.cs → WouldCreateCycle(modules, dependencies, from, to), TopologicalSort()
LearningPathService.cs → CreateAsync(), AddModuleAsync(), AddDependencyAsync() + DagValidator guard, Publish/Archive
AttemptService.cs → StartAttemptAsync(), SaveAnswerAsync(), SubmitAttemptAsync() (eval)
AiFeedbackService.cs → GenerateFeedbackAsync() → PromptBuilder → IAiProvider (NvidiaProvider) → AiResponseParser
CommunityService.cs → GetPostsAsync(), GetGroupPostsAsync(), VotePostAsync(), BanUserFromGroupAsync()
ProgressService.cs → MarkModuleCompleteAsync() (unlock dependents via Dag + Certificate on 100%)
ApplicationDbContext.cs → GuardAuditLogImmutability(), OnModelCreating
apiClient.ts → request interceptor Bearer, response 401 queue isRefreshing+failedQueue
AuthSlice → loginThunk, registerThunk ; CommunitySlice → fetchPosts/fetchGroupPosts/fetchGroups (requestId + AbortSignal)
```

---

## 9. Dependencies (cross-feature)

```
Certificate → Progress 100% → Module + DAG → User
AI Feedback → Submission → Assignment → Classroom
Quiz Attempt → QuestionBank → Quiz → Module → LearningPath
Assignment/Submission → Classroom → UserClassroom
Community Post → Group (GroupId) → GroupMember
Analytics/Dashboard → Progress + QuizAttempt + Classroom aggregates
AuditLog → All Services via IAuditLogService (append-only guard)
Notification → User (bell in Topbar, no SignalR)
Theme → All pages via MUI `getAppTheme` + CssBaseline
```

---

## 10. Source of Truth

```
JWT generation        → Authentication/Jwt/JwtTokenGenerator.cs
JWT config            → Program.cs:136-150 + appsettings.json Jwt:Secret/Issuer/Audience/ExpiryMinutes (60)
Password hashing      → Identity UserManager (Program.cs Identity 8len/Digit/Lower/Upper, Lockout 5m)
DB config / CORS      → Program.cs (UseSqlServer + RetryOnFailure, LearnPathCors), appsettings.json ConnectionStrings
DAG logic             → Algorithms/Graph/DagValidator.cs + Services/LearningPath/LearningPathService.cs + utils/graphUtils.ts
File upload (6MB)     → Services/Submission/FileValidationService.cs + FileStorageService.cs + Configuration/UploadSettings + uploads/assignments/
AI provider           → Services/Ai/NvidiaProvider.cs (Program.cs AddHttpClient<IAiProvider,NvidiaProvider>), AiOptions (Nvidia)
Community paging      → Repositories/CommunityRepository.cs (PagedResult, PostFilter/Sort) + Services/Community/CommunityService.cs
Frontend auth state   → redux/slices/authSlice.ts + utils/tokenUtils.ts + hooks/useAuth.ts + app/provider.tsx
Theme                 → theme/palette.ts + theme/index.ts:getAppTheme + context/ThemeModeContext.tsx (learnpath-theme-mode)
```

---

## 11. Status / Issues ⚠️

| Area | Status | Note |
|---|---|---|
| JWT/Auth/RBAC | ✅ | HS256, 60m/7d, RoleSeeder ok |
| DAG | ✅ | DagValidator wouldCreateCycle+TopologicalSort |
| Classroom/Submission/File | ✅ | upload 6MB + uploads/assignments |
| AI Feedback | 🟡 | Nvidia only; GeminiProvider.cs dead code (no factory) |
| Certificate | ✅ | via Progress 100% |
| RateLimit | ⚠️ | 50/min (docs say 100), in-memory only |
| Backend tests | 🔴 | `testingErrorReport.txt` FileLoadException Application Control policy blocked assembly |
| Hardcoded api fallback | ⚠️ | `apiClient.ts` localhost fallback breaks docker if env unset |
| Duplicate route | ⚠️ | AppRoutes `ADMIN_QUIZZES` twice (Admin+Admin) |
| Theme | ✅ | light/dark functional, persisted |

---

## 12. Interview Relevance (HIGH → inspect first)

```
Authentication HIGH — JWT vs Refresh, Bearer, 401 queue, RoleSeeder, UserStatus, lockout
Learning Path HIGH — DAG WouldCreateCycle/TopologicalSort, ModuleDependency self-ref, EF Configurations, draft→published
Classroom HIGH — inviteCode, UserClassroom M2M, role Owner/Member, member removal
Assignment/Quiz HIGH — QuestionBank JSON 6MB validation, Quiz publish/archive, Attempt lifecycle
Progress HIGH — DAG unlock, 100% Certificate trigger
AI Feedback MEDIUM — provider pattern, PromptBuilder, parser, SubmissionAiFeedback
Community HIGH — pagination, Post/Group, votes, moderation, requestId race fix
Analytics MEDIUM — Recharts, Dashboard aggregates
Admin/Audit MEDIUM — AdminOnly policy, GuardAuditLogImmutability, history
```

---

## 13. Efficiency

> **Target:** `Question → §4 lookup → §3 files → targeted Read → answer` — do not scan `node_modules/bin/obj/graphify-out`.

```
Example: "Where is JWT?"      → §4 → AuthController.cs + JwtTokenGenerator.cs + Program.cs:136
Example: "Module dependency?" → §4 → DagValidator.cs + LearningPathService.cs + ModuleDependency.cs + PathGraph.tsx
Example: "Invite code?"       → §4 → ClassroomController.cs + ClassroomService.cs + UserClassroom.cs
```

*Paths relative to `D:\FInal Project\LearnPath`; verify §8 by opening primary file first.*

