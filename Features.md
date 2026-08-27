LEARNPATH — MAJOR FEATURE INVENTORY
1. Authentication & Account Management
Register/login, JWT access tokens, refresh-token rotation, logout/revoke, profile view/update, change password.
Roles: Student, Instructor, Admin
Files: backend/Controllers/AuthController.cs, Services/Auth/AuthService.cs, Authentication/Jwt/JwtTokenGenerator.cs, Controllers/UserController.cs, Services/User/UserService.cs, frontend/services/authService.ts, redux/slices/authSlice.ts, pages/auth/*
Status: Fully implemented
2. Role-Based Authorization & Security Infrastructure
ASP.NET Core Identity roles (Admin/Instructor/Student/Super Admin), JWT bearer + role claims, AdminOnly/InstructorOrAdmin policies, seeded roles and Super Admin, CORS, .env secret config.
Roles: Platform-wide
Files: backend/Program.cs, Data/Seeders/RoleSeeder.cs, Authentication/Jwt/*, appsettings.json, frontend route guards (ProtectedRoute, AdminRoute, InstructorRoute, GuestRoute)
Status: Fully implemented
3. Learning Path Engine (DAG-based)
Author paths/modules (video/PDF, resources, objectives, tags, difficulty, duration), publish/unpublish/archive/reorder, inter-module dependencies with DAG cycle detection & topology, browse/enroll, prerequisite-gated module content, path graph visualization.
Roles: Instructor/Admin (author), Student (learn)
Files: backend/Controllers/LearningPathController.cs, Services/LearningPath/LearningPathService.cs, Algorithms/Graph/DagValidator.cs, Entities/{LearningPath,Module,ModuleDependency,ModuleResource,ModuleObjective,ModuleTag}.cs, frontend/components/learningPath/PathGraph/PathGraph.tsx, pages/learningPaths/*
Status: Fully implemented
4. Question Bank Management
JSON question-bank upload with per-question validation, auto-versioning on re-upload, download, search by title/subject/tag, archive/restore; questions/options parsed into relational tables.
Roles: Admin (manage), Instructor (view/search)
Files: backend/Controllers/QuestionBankController.cs, Services/QuestionBank/QuestionBankService.cs, Services/Validation/JsonValidationService.cs, Entities/{QuestionBank,Question,Option}.cs, frontend/pages/admin/AdminQuestionBanksPage.tsx
Status: Fully implemented
5. Quiz Engine
Quiz authoring (question count, difficulty, random/sequential selection, time limit, pass %, max attempts), draft→published→archived lifecycle, module linking, timed student attempts with seeded random selection, auto-grading/score/pass, answer review, attempt limits, quiz analytics.
Roles: Admin/Instructor (author), Student (attempt)
Files: backend/Controllers/{QuizController,AttemptController}.cs, Services/{Quiz/QuizService,Attempt/AttemptService}.cs, Entities/{Quiz,QuizAttempt,StudentAnswer,ModuleQuiz}.cs, frontend/pages/quiz/*, pages/admin/AdminQuizEditorPage.tsx
Status: Fully implemented (already documented in features/QUIZ_ENGINE.md; included for completeness)
6. Progress Tracking & Certificate Issuance
Mark modules complete with prerequisite enforcement, per-path progress % and unlock state, automatic certificate record issuance at 100% completion, certificate listing (student) and admin management.
Roles: Student, Instructor/Admin
Files: backend/Controllers/{ProgressController,CertificateController}.cs, Services/Progress/ProgressService.cs, Entities/{Progress,Certificate}.cs, frontend/services/progressService.ts, pages/certificates/CertificatesPage.tsx
Status: Partially implemented — progress fully works; CertificateUrl is a stub, no actual certificate document/renderer
7. Classroom Engine
Classrooms bound to a learning path, invite-code join, join/leave, member listing/removal, instructor marks students invalid with reason, admin reassignment of paths.
Roles: Instructor (manage), Student (join), Admin (oversight)
Files: backend/Controllers/ClassroomController.cs, Services/Classroom/ClassroomService.cs, Entities/{Classroom,UserClassroom}.cs, frontend/pages/classroom/*, redux/slices/classroomSlice.ts
Status: Fully implemented
8. Assignment & Submission Workflow
Assignment CRUD per classroom, file submissions (PDF/DOC/DOCX/TXT; size/extension/MIME validation, path-traversal protection), full status lifecycle (submitted→under review→reviewed→graded/returned/submitted-again), late detection, grade/feedback, publish evaluation, authorized preview/download.
Roles: Instructor (create/grade), Student (submit)
Files: backend/Controllers/{ClassroomController,SubmissionController}.cs, Services/Classroom/ClassroomService.cs, Services/Submission/{FileStorageService,FileValidationService}.cs, Entities/{Assignment,Submission,SubmissionStatus}.cs, frontend/pages/classroom/AssignmentDetailPage.tsx
Status: Fully implemented
9. AI-Powered Assignment Feedback
Instructors generate structured AI feedback (summary, grammar, rubric coverage, missing topics, suggested score, recommendation) for TXT/PDF submissions via LLM; structured response parsed and persisted with regeneration; students see it after evaluation is published.
Roles: Instructor (generate), Student (view)
Files: backend/Controllers/SubmissionController.cs, Services/Ai/{AiFeedbackService,NvidiaProvider,PromptBuilder,AiResponseParser}.cs (GeminiProvider.cs exists but unregistered), Entities/SubmissionAiFeedback.cs, frontend/components/classroom/AiFeedbackSection.tsx
Status: Fully implemented (NVIDIA provider active; Gemini provider dormant)
10. Community Platform (Forum, Groups, Moderation)
Posts (categories, tags, code snippets), threaded comments/replies, up/down voting with score math, trending-rank search, public/private groups (join/leave, member ban/unban, pinned announcements), report→resolve/dismiss moderation queue.
Roles: All (post/comment/vote/report), Group owner/Admin (moderate), Admin (moderation queue)
Files: backend/Controllers/CommunityController.cs, Services/Community/CommunityService.cs, Repositories/CommunityRepository.cs, Entities/{Post,Comment,PostVote,CommentVote,Group,GroupMember,Report}.cs, frontend/pages/community/*, redux/slices/communitySlice.ts
Status: Fully implemented
11. Analytics & Dashboard
Post-login dashboard (stat cards, recent activity, per-path progress), personal analytics (weekly activity, streaks, path completion, module-type breakdown), per-quiz analytics (attempts, avg/pass %, score distribution, hardest questions), admin platform stats.
Roles: All users (own), Admin (platform/quiz analytics)
Files: backend/Controllers/{DashboardController,AnalyticsController}.cs, Services/{Dashboard/DashboardService,Analytics/AnalyticsService}.cs, frontend/pages/dashboard/DashboardPage.tsx, pages/analytics/AnalyticsPage.tsx, pages/admin/AdminQuizAnalyticsPage.tsx
Status: Fully implemented
12. Notification System
In-app notification listing, mark-one-read, mark-all-read, unread badge in topbar; fetch on app boot.
Roles: All authenticated users
Files: backend/Controllers/NotificationController.cs, Services/Notification/NotificationService.cs, Entities/Notification.cs, frontend/redux/slices/notificationSlice.ts, components/dashboard/Topbar/Topbar.tsx
Status: Partially implemented — read/manage works; no event producers call CreateAsync, so no flow actually triggers notifications
13. Admin / Super Admin Console
Platform-wide stats, user search/list/detail with activity aggregates, change roles (Super Admin only), activate/deactivate/mark-invalid/soft-delete, manage learning paths, classrooms (reassign path), certificates, quiz analytics, and global audit-history browsing.
Roles: Admin, Super Admin (role changes/global history)
Files: backend/Controllers/{AdminController,HistoryController}.cs, Services/Admin/AdminService.cs, DTOs/Admin/*, frontend/pages/admin/*
Status: Fully implemented
14. Audit Trail / Activity History
Append-only audit logging for ~90 action types across auth/academic/assessment/community domains; users query own history; Super Admin browses all users' history with filters; immutability enforced in DbContext.
Roles: All (own), Super Admin (global)
Files: backend/Controllers/HistoryController.cs, Services/Audit/{AuditLogService,AuditActionCatalog}.cs, Entities/{AuditLog,AuditAction}.cs, Data/ApplicationDbContext.cs, frontend/pages/history/HistoryPage.tsx, pages/admin/AdminHistoryPage.tsx
Status: Fully implemented
15. API Platform Infrastructure
Global exception middleware (maps known exceptions + FK violations), request logging, per-IP rate limiting (50 req/min), unified ApiResponse<T> envelope, FluentValidation auto-registration, Swagger with Bearer auth, health check, AutoMapper, JSON enum-string conversion, centralized DI.
Roles: Developers/ops (infrastructure)
Files: backend/Middleware/{ExceptionMiddleware,LoggingMiddleware,RateLimitingMiddleware}.cs, Common/ApiResponse.cs, Swagger/SwaggerConfig.cs, Controllers/HealthController.cs, Program.cs
Status: Fully implemented
TOTAL MAJOR FEATURES: 15
Final numbered list in recommended documentation order
Authentication & Account Management
Role-Based Authorization & Security Infrastructure
Learning Path Engine (DAG-based)
Question Bank Management
Quiz Engine (already documented — features/QUIZ_ENGINE.md)
Progress Tracking & Certificate Issuance (partial)
Classroom Engine
Assignment & Submission Workflow
AI-Powered Assignment Feedback
Community Platform (Forum, Groups, Moderation)
Analytics & Dashboard
Notification System (partial)
Admin / Super Admin Console
Audit Trail / Activity History
API Platform Infrastructure