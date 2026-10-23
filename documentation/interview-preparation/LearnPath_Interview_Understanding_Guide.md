# LEARNPATH — PROJECT INTERVIEW UNDERSTANDING GUIDE

> **What this document is:** a plain-language version of `LearnPath_Factual_Interview_Material.md`.
> Same facts, same technical depth — explained so you actually understand it and can say it out loud.
>
> **How to read it:** read *Understand It* until the picture is clear in your head. Only then look at
> *Explain It to the Interviewer*. That part is a sample of how it can sound, **not a script to memorise**.
>
> **Source files verified:** `AuthService.cs`, `JwtTokenGenerator.cs`, `Program.cs`, `DagValidator.cs`,
> `LearningPathService.cs`, `ProgressService.cs`, `AttemptService.cs`, `AiFeedbackService.cs`,
> `ClassroomService.cs`, `AuditLogService.cs`, `ApplicationDbContext.cs`, `apiClient.ts`, EF configurations.
>
> **Originals untouched:** `LearnPath_Factual_Interview_Material.md` and
> `LearnPath_Humanized_Interview_Guide.md` were not modified.

---

## 1. LearnPath — The Big Picture

### What is LearnPath?

LearnPath is a learning platform where a course is not a flat list of lessons — it is a **graph**.

An admin creates a *learning path*, adds *modules* inside it, and then says which module must be
finished before which other one. The platform stores those connections and uses them to decide what a
student is allowed to open next. On top of that there are quizzes, classrooms with assignments,
AI-assisted grading help, certificates, a community section, and a full history of who did what.

### What problem does it solve?

The real problem, stated simply:

* Learners have plenty of material but no reliable **order**.
* In a normal course list, nothing stops a beginner from opening an advanced topic first.
* An admin can *suggest* an order, but cannot *enforce* it.

The result is students getting lost in topics they were not ready for.

### Why does it exist?

Because "what should I learn next?" is a structural question, not a content question. LearnPath's answer
is to make prerequisites a real part of the data model instead of a note in the description — and then
let the **server** enforce them, so the rule holds no matter how the request arrives.

### How does it solve the problem?

1. A module can declare that it depends on other modules. Those links form a graph.
2. Before saving a new link, the system checks it does not create a **cycle** (A needs B, B needs A —
   which would mean neither can ever be opened).
3. Every time a student loads a path, the system recalculates which modules are unlocked based on what
   that student has actually completed.
4. The same check is repeated on every sensitive endpoint — opening module content, starting a quiz,
   submitting an assignment — so the lock cannot be bypassed by calling the API directly.

### Main features (all verified as implemented)

| Feature | What it actually does |
|---|---|
| **Learning path + DAG** | Paths, modules, dependencies, cycle prevention, publish / archive / reorder, graph view |
| **Progress + certificate** | Mark complete, progress percentage, automatic certificate at 100% |
| **Quiz engine** | Question bank JSON upload, quiz draft/published/archived, seeded random question selection, attempts, automatic evaluation |
| **Classroom + assignment** | 8-character invite code, join/leave, assignments, file submission, grading, publish evaluation |
| **AI assignment feedback** | Admin-triggered; reads TXT/PDF, sends a structured prompt to a hosted model, stores parsed feedback |
| **Community** | Posts with code snippets and tags, threaded comments, votes, groups, reports, trending feed |
| **Auth + roles** | Register / login / refresh / revoke, JWT, roles Student / Admin, plus a SuperAdmin flag |
| **Audit logging** | Append-only history of every important action, enforced at the database-context level |
| **Dashboard / analytics** | Stats and weekly charts using Recharts |

**Be honest about these — they are partial:**

* `GeminiProvider.cs` exists but is **not registered**. Only the NVIDIA provider is live.
* The certificate is a **database record** with a URL string. **No PDF file is generated.**
* Notifications are **fetched by the client**. There is no SignalR or WebSocket push.
* Rate limiting is **in-memory per instance** — it would not hold across multiple servers.
* `ADMIN_QUIZZES` is duplicated in `AppRoutes.tsx`.

---

## 2. Overall Project Flow

This is the path every request takes. Learn it once and you can answer a surprising number of questions.

```text
User clicks something in the React page
        ↓
Redux thunk calls a service file (pathService, quizService, ...)
        ↓
apiClient (Axios) attaches the JWT as "Authorization: Bearer <token>"
        ↓
Nginx (port 80) forwards /api/* to the ASP.NET Core API (port 5000)
        ↓
Middleware pipeline:  Exception → Logging → RateLimit → CORS → Authentication → Authorization
        ↓
Controller  (marked [Authorize], reads the user id from the token claims)
        ↓
Service     (all business rules live here, plus the audit log call)
        ↓
EF Core DbContext → SQL Server
        ↓
Result wrapped in ApiResponse<T> { success, message, data, errors }
        ↓
Redux slice updates → the component re-renders
```

Two rules that hold everywhere in this project:

* **Controllers only validate and delegate.** All real logic sits in services.
* **Every response has the same shape**, including validation failures — so the frontend has exactly
  one thing to handle.

---

## 3. Feature 1 — Learning Path and Module Dependencies (the DAG)

This is the heart of the project. Give it the most preparation time.

### What It Does

Lets an admin connect modules with "finish this before that", and then locks a module for a student
until all the modules it depends on are complete.

### Why It Exists

Without it, `Collections` could be opened before `OOP`. The whole point of the product is that the system
knows what should come first and will not let a student skip ahead.

### Understand It

Think of a learning path as a set of boxes with arrows between them.

An arrow from *OOP* to *Collections* means "Collections needs OOP first". In the database that arrow is a
row in the `ModuleDependencies` table with two columns: `ModuleId` (the module that needs something) and
`DependsOnModuleId` (the prerequisite). Both are foreign keys back to `Modules` — which is why this is
called a **self-referencing many-to-many** relationship: a table pointing at itself, twice.

All of these arrows together form a **graph**. It must be **acyclic** — no loop. If OOP needed Collections
*and* Collections needed OOP, neither could ever unlock. A directed graph with no loops is called a **DAG**,
a Directed Acyclic Graph. That is where the name comes from.

There is one more piece that people miss, and it matters: **what if a module has no dependencies at all?**
Then the system falls back to plain **sequential order** — the previous module by `Order` must be complete.
So the final rule has two parts:

```text
dagUnlocked        = every prerequisite of this module is completed
sequentialUnlocked = if this module has NO prerequisites, the previous module by Order must be completed
isUnlocked         = dagUnlocked AND sequentialUnlocked
```

That is verified in `LearningPathService.cs` lines 64–98.

### How It Works

**Adding a dependency (admin side):**

```text
Admin connects two modules in the UI
        ↓
POST /api/v1/paths/{pathId}/dependencies  { ModuleId, DependsOnModuleId }
        ↓
Service confirms the caller OWNS this path (otherwise 403)
        ↓
Service loads all existing dependencies of this path and builds an adjacency map
        ↓
DagValidator.WouldCreateCycle(...)  — a depth-first search
        ↓
cycle found → reject ("Adding this dependency would create a cycle")
no cycle    → save the row + write an audit log entry
```

The cycle check itself is the clever bit, and it is only a few lines. To add "A depends on B", it starts
the search at **B** and asks: *following existing arrows, can I get back to A?* If yes, adding this arrow
would close a loop, so it is refused. Nothing is saved.

**Opening a module (student side):**

```text
Student opens a path
        ↓
GET /api/v1/paths/{id}
        ↓
Backend loads the modules, loads that student's completed modules,
and recalculates isUnlocked for every module right there
        ↓
Response includes IsCompleted and IsUnlocked per module
        ↓
UI shows: green check = done, blue play = unlocked, grey lock = locked (0.65 opacity, "Locked" chip)
        ↓
Clicking a locked card shows a Snackbar and does NOT navigate
```

And if someone skips the UI and calls the API directly, `GetModuleContentAsync` runs the same check and
throws, which the exception middleware turns into **403 Forbidden**. The same guard exists in the quiz
flow and the assignment flow.

Nothing is precomputed and stored — unlock status is recalculated on every fetch from the student's
`Progress` rows. There is no background job.

### How It Is Implemented

* `Algorithms/Graph/DagValidator.cs` — `WouldCreateCycle` (DFS) and `TopologicalSort`
* `Services/LearningPath/LearningPathService.cs` — `AddDependencyAsync`, `GetByIdAsync` (unlock calculation), `GetModuleContentAsync` (the 403 guard), `PublishModuleAsync`
* `Entities/ModuleDependency.cs` + `Configurations/ModuleDependencyConfiguration.cs`
* `Controllers/LearningPathController.cs` — routes under `api/v1/paths`
* Frontend: `LearningPathDetailPage.tsx` (the locked/unlocked card), `PathGraph.tsx` + `graphUtils.ts` (the SVG graph view)

### Technical Terms to Remember

* **DAG** — a graph with direction and no loops; here, modules and their prerequisites.
* **Dependency / prerequisite** — one row saying module X needs module Y first.
* **DFS (depth-first search)** — follow one chain of arrows as far as it goes, then back up and try another. Used for the cycle check.
* **Self-referencing many-to-many** — `ModuleDependency` links `Module` to `Module`.
* **Topological sort** — an ordering of the graph where every prerequisite comes before what needs it.

### Explain It to the Interviewer

> "Basically, a learning path in my project is not a flat list — modules can depend on other modules, and
> those dependencies form a graph. A student only sees a module as unlocked when everything it depends on
> is already completed. The graph has to stay acyclic, otherwise two modules could depend on each other and
> neither would ever open. So before I save a new dependency, I run a depth-first search starting from the
> prerequisite to check whether it already leads back to the module being edited. If it does, I reject it.
> And if a module has no dependencies set, it falls back to simple sequential order."

*(That is roughly 35 seconds. Then stop and let them ask.)*

### Mention Initially

* Path contains modules; modules can depend on other modules — that is the graph.
* A module unlocks only when its prerequisites are complete.
* A cycle check runs before saving a dependency.
* The check is on the server, not just in the UI.

### Don't Explain Initially

* The exact adjacency-dictionary structure.
* `TopologicalSort` (it exists, but it is not what drives unlocking).
* The SVG drawing logic in `PathGraph.tsx`.
* Opacity values and chip labels in the module card.

### If Asked Deeper

* **Complexity:** the DFS touches each module and each dependency at most once — `O(V + E)`.
* **Sequential fallback:** explain the two-part rule above; this is the detail most candidates miss and it lands well.
* **Weaknesses, if pushed:** the DFS is recursive, so a very deep graph could in principle exhaust the stack; and because the check reads the graph and then writes, two simultaneous edits could in theory combine into a cycle.

### Likely Interview Questions

**Very likely**
1. What is a learning path, and how do dependencies work?
2. How do you stop someone creating a circular dependency?
3. What stops a student opening a locked module directly through the API?

**Likely**
4. What is the difference between the DAG rule and sequential order?
5. Where is the unlock status stored?

**Possible**
6. What is the time complexity of the cycle check?
7. Why a graph rather than a simple ordered list?

### Possible Follow-ups

* "Is unlock status calculated or stored?" → Calculated on every fetch from the `Progress` table.
* "Why a graph instead of a list?" → A list forces one fixed order; a graph lets one module have several prerequisites, or none at all.
* "What does the student actually see?" → Grey card with a lock icon and a "Locked" chip; clicking shows a message and does not navigate.

---

## 4. Feature 2 — Progress Tracking and Certificates

### What It Does

Records which modules a student has completed, calculates progress for a path, and issues a certificate
automatically when a path reaches 100%.

### Why It Exists

Progress is what drives everything else — unlocking, quiz access, the dashboard, and the certificate. It
is the single source of truth for "what has this student actually done".

### Understand It

There is one `Progress` row per user per module, holding `IsCompleted` and `CompletedAt`. That table has a
**unique index on `(UserId, ModuleId)`**, so the same user can never end up with two rows for the same
module — even if two requests arrive at the same instant.

A module can be completed in two ways:

1. **Directly** — the student marks it complete (`POST /api/v1/progress/complete`).
2. **By passing its quiz** — the quiz flow calls `MarkModuleCompleteFromQuizAsync` internally.

Either way, the same thing happens afterwards: an audit entry is written, and the system checks whether
this completion finished the whole path.

### How It Works

```text
Student marks a module complete
        ↓
Service loads the module with its dependencies
        ↓
Counts how many prerequisites this student has already completed
        ↓
Missing any → reject with "Complete all prerequisite modules first."
        ↓
Insert or update the Progress row, save
        ↓
Write an audit log entry (MODULE_COMPLETED)
        ↓
Count the student's completed modules in this path
        ↓
All complete AND no certificate yet → create a Certificate row + audit entry
```

The progress summary for a path is built the same way in reverse: count completed vs total for the
percentage, and report each module with its `IsUnlocked` flag.

### How It Is Implemented

* `Services/Progress/ProgressService.cs` — `MarkCompleteAsync`, `MarkModuleCompleteFromQuizAsync`, `TryIssueCertificateAsync`, `GetPathProgressAsync`
* `Entities/Progress.cs`, `Entities/Certificate.cs`
* `Configurations/ProgressConfiguration.cs` — the unique index
* `Controllers/ProgressController.cs`, `Controllers/CertificateController.cs`

### Technical Terms to Remember

* **Unique index** — a database rule that blocks duplicate combinations; here `(UserId, ModuleId)` and `(UserId, LearningPathId)`.
* **Idempotent-ish behaviour** — clicking complete twice gives a clear error rather than a second row.

### Explain It to the Interviewer

> "Progress is one row per user per module, with a unique index so a module can't be completed twice. When
> a student marks something complete, the backend first checks that all the prerequisites are done — it
> doesn't trust the UI for that. After saving, it counts the completed modules in that path, and if
> everything is done it creates a certificate record for that user and path. That's also protected by a
> unique index, so a student can't end up with two certificates for the same path."

### Mention Initially

* One progress row per user per module.
* Prerequisites are checked on the server before completion is accepted.
* Certificate is issued automatically at 100%.

### Don't Explain Initially

* The exact DTO fields returned in the progress summary.
* The per-path loop used when building progress for all enrolled paths.

### If Asked Deeper

* **"How is the certificate generated?"** — Say it precisely: *a `Certificate` record is created, with a
  `CertificateUrl` string in the format `/certificates/{userId}/{pathId}`.* **Do not say "PDF".** There is
  no file generation. If they ask what you would improve, real PDF generation is a good answer.
* **Concurrency:** two simultaneous completions are stopped by the application check first, and by the
  unique index underneath it.

### Likely Interview Questions

**Very likely**
1. How do you track student progress?
2. How does a certificate get issued?

**Likely**
3. What stops a student marking a locked module complete?
4. What happens if the student clicks complete twice?

### Possible Follow-ups

* "Where does the percentage come from?" → Completed modules ÷ total modules in the path, rounded.
* "Is the certificate a file?" → No — a database record with a URL string.

---

## 5. Feature 3 — Quiz Engine

### What It Does

Lets an admin upload a bank of questions as JSON, an admin build a quiz from that bank and attach it
to a module, and a student attempt it. Passing the quiz completes the module.

### Why It Exists

To check that a student actually understood a module before the next one unlocks — rather than trusting a
"mark as complete" button.

### Understand It

Three things are deliberately kept separate, and that separation is worth explaining:

* **Question bank** — a pool of questions, uploaded as JSON. Reusable.
* **Quiz** — a configuration built *from* a bank: how many questions, time limit, passing percentage, maximum attempts.
* **Attempt** — one student's single run at a quiz.

Because the bank is separate from the quiz, the same questions can feed several quizzes.

**How randomisation works, and why it is not just `Random`:** each attempt stores a `RandomSeed`. Question
selection uses a **Fisher-Yates shuffle** seeded with that number. Fisher-Yates walks the list from the end,
swapping each item with a randomly chosen earlier one — it produces a fair shuffle in a single pass.

The important consequence: because the seed is stored on the attempt, the *same* attempt always produces
the *same* question order. That is what makes the review screen possible — the student can go back and see
exactly the quiz they took. Options are shuffled too, using `seed + QuestionId`, so option order is
deterministic per question but different across questions and attempts.

### How It Works

```text
Admin uploads questions JSON  →  validated  →  QuestionBank + Questions + Options stored
        ↓
Admin creates a Quiz from that bank (Draft), publishes it, links it to a module
        ↓
Student starts an attempt
        ↓
Server checks: quiz is Published, quiz is linked to this module,
               the module is UNLOCKED (same DAG + sequential rule),
               an unfinished attempt is resumed instead of creating a new one,
               attempt limit not exceeded
        ↓
Attempt created with a RandomSeed; questions selected and shuffled with that seed
        ↓
Student answers → each answer saved individually (PUT /attempts/{id}/answer)
        ↓
Student submits
        ↓
Server re-selects the same questions using the stored seed, compares each saved
OptionId with the option flagged IsCorrect, counts the correct ones
        ↓
Percentage = correct ÷ total ;  Passed = Percentage >= PassingPercentage
        ↓
Passed → module marked complete via ProgressService → next module can unlock,
         and the certificate check runs
Failed → nothing unlocks; the student can retry if attempts remain
```

Answers are saved one at a time as the student moves through the quiz, so a page refresh does not lose work.

**Validation on the JSON upload** (`JsonValidationService`) — it collects *all* errors instead of stopping
at the first one, so an admin can fix the file in a single pass:

* extension must be `.json`; size under **5 MB**
* the file must parse as JSON, with `Title`, `Subject`, `Questions` present
* each question needs question text, at least 2 options, a `correct_answer` that matches one of the options, a difficulty of easy/medium/hard, and an explanation
* duplicate question text is rejected

Uploading the same title + subject again creates a new **version** (`Version + 1`) rather than overwriting.

### How It Is Implemented

* `Services/QuestionBank/QuestionBankService.cs` — upload, parse, versioning
* `Services/Validation/JsonValidationService.cs` — the staged validation above
* `Services/Quiz/QuizService.cs` — create, publish, archive, link to module
* `Services/Attempt/AttemptService.cs` — `StartAttemptAsync`, `SaveAnswerAsync`, `SubmitAttemptAsync`, `GetSelectedQuestionsAsync`, `ShuffleList`
* Entities: `QuestionBank`, `Question`, `Option`, `Quiz`, `ModuleQuiz`, `QuizAttempt`, `StudentAnswer`
* Frontend: `AdminQuestionBanksPage` (upload, with the JSON template and rules dialog), `QuizInstructionsPage`, `AttemptPage`, `ResultPage`, `ReviewPage`

### Technical Terms to Remember

* **Seed** — the number stored on an attempt that makes the shuffle repeatable.
* **Fisher-Yates shuffle** — a single-pass algorithm producing a fair random ordering.
* **Junction table** — `ModuleQuiz`, which links a module to a quiz.
* **Server-side evaluation** — scoring happens on the backend; the browser never receives the answer key.

### Explain It to the Interviewer

> "The quiz engine has three parts — a question bank uploaded as JSON, a quiz built from that bank, and an
> attempt, which is one student's run. When a student starts an attempt, I store a random seed on it, and
> use that seed with a Fisher-Yates shuffle to pick and order the questions. Storing the seed means the same
> attempt always shows the same questions, which is what lets the student review it afterwards. Answers are
> saved as they go, and on submit the backend does the scoring — it compares each saved option with the
> correct one. If the percentage is above the passing mark, the module is marked complete, which can unlock
> the next module and trigger the certificate."

### Mention Initially

* Bank → quiz → attempt, and why they are separate.
* Seeded random selection so an attempt is reproducible.
* Evaluation is on the server.
* Passing completes the module.

### Don't Explain Initially

* The full list of eight JSON validation rules.
* Quiz status transitions (Draft → Published → Archived → Unpublish).
* The `Version + 1` behaviour on re-upload.
* How options are shuffled with `seed + QuestionId`.

### If Asked Deeper

* **"Why store a seed instead of the selected question ids?"** — Storing the seed keeps the attempt row small and reproduces both the question order and the option order from a single number.
* **"What is Fisher-Yates?"** — Walk from the last item backwards; swap each with a randomly picked item at or before it. Every ordering is equally likely, in one pass.
* **On the size limit:** the controller has `[RequestSizeLimit(6MB)]` — the HTTP cap — while the file validation limit is **5 MB**. If you are asked, that is the honest explanation of the two numbers.

### Likely Interview Questions

**Very likely**
1. How does the quiz feature work end to end?
2. How is a quiz scored, and where does the scoring happen?

**Likely**
3. How do you randomise questions, and how can a student still review the attempt?
4. How do you validate the uploaded question bank?

**Possible**
5. Can a student retake a quiz? How is that limited?
6. What happens if the student refreshes mid-quiz?

### Possible Follow-ups

* "Could the answers leak to the browser?" → No; evaluation is server-side and the correct flags are not sent to the attempt screen.
* "What if they close the tab?" → Answers are already saved; the unfinished attempt is resumed on return.

---

## 6. Feature 4 — Classroom, Assignment and Submission

### What It Does

Lets an admin run a class around a learning path, post assignments, and grade the files students
submit.

### Why It Exists

A learning path is self-paced. A classroom adds the teaching side — a group of students, deadlines, and
graded work.

### Understand It

A classroom is created against a learning path. On creation the server generates an **invite code**: the
first 8 characters of a GUID, uppercased, with a unique index on the column. The creator is immediately
added as a member with the role `Admin`.

Students join with that code. Membership lives in `UserClassroom` — a **many-to-many** join between users
and classrooms that also stores the member's role in *that* classroom. It has to be an explicit entity
rather than a plain join table precisely because it carries that extra role column.

Assignments have their own unlocking rule, and it is **different** from the module rule — worth knowing so
you do not mix them up. An assignment unlocks when the previous assignment's submission has a
**grade of 5 or more**. Locked assignments do not even expose their description to students.

### How It Works

```text
Admin creates a classroom (against a learning path)
        ↓  invite code generated, admin added as a member
Student joins with the code  →  UserClassroom row with role "Student"
        ↓
Admin creates an assignment (title, description, due date)
        ↓
Student uploads a file
        ↓  server checks the assignment is unlocked (previous grade >= 5)
        ↓  FileValidationService checks the file
        ↓  FileStorageService writes it to uploads/assignments/
        ↓  Submission row created; IsLate set if past the due date
        ↓  (if the database write fails, the stored file is deleted again)
        ↓
Admin moves it through statuses: Submitted → UnderReview → Reviewed
        ↓  grades it (Grade + Feedback, status Graded)
        ↓  publishes the evaluation (sets PublishedAt)
        ↓
Only now does the student see the grade, the feedback, and any AI feedback
```

**File validation** checks five things: size against the configured maximum (5 MB), that the file is not
empty, that the extension is one of `.pdf .doc .docx .txt`, that the MIME type is allowed, **and that the
MIME type matches the extension** — so a file renamed to `.pdf` that reports a different type is rejected.

### How It Is Implemented

* `Services/Classroom/ClassroomService.cs` — `CreateAsync`, `JoinAsync`, `CreateAssignmentAsync`, `UploadSubmissionAsync`, `EnsureAssignmentUnlockedAsync`, `GradeSubmissionAsync`, `PublishEvaluationAsync`, `GenerateInviteCode`
* `Services/Submission/FileValidationService.cs`, `FileStorageService.cs`
* Entities: `Classroom`, `UserClassroom`, `Assignment`, `Submission`
* `Configuration/UploadSettings` + `appsettings.json` (`MaxFileSize`, allowed extensions and MIME types)

### Technical Terms to Remember

* **Many-to-many with payload** — `UserClassroom` links users and classrooms and stores the role too.
* **Invite code** — 8 characters from a GUID, unique-indexed.
* **MIME type** — the content type the browser reports for a file; checked against the extension here.

### Explain It to the Interviewer

> "A classroom sits on top of a learning path. When the admin creates one, the server generates an
> 8-character invite code, and students join with that code — membership is stored in a join table that also
> holds their role in that classroom. The admin posts assignments, and students upload files. The upload
> is validated on size, extension and MIME type, and I also check the extension and MIME agree, so a renamed
> file gets rejected. The file goes to disk and the database row keeps the path and the metadata. After
> grading, the admin publishes the evaluation — and only then does the student see the grade and
> feedback."

### Mention Initially

* Invite code join, membership with a role.
* Assignment → file submission → grade → publish.
* Files are validated and stored on disk; the row keeps the path.

### Don't Explain Initially

* The full status transition list.
* The "delete the stored file if the database write fails" cleanup.
* The `IsLate` flag.

### If Asked Deeper

* **"Is the file validation safe?"** — Be honest: extension and MIME both come from the client and can be
  spoofed. The stronger check is reading the file's magic bytes. Say that as the improvement.
* **"Why disk and not the database?"** — Keeps the database small and backups fast. The trade-off, which you
  should name yourself: local disk does not survive a replaced container and does not work across multiple
  servers; cloud object storage would be the production answer.
* **Assignment unlocking** is grade-based (previous grade ≥ 5), which is a *different* rule from module
  unlocking. Do not conflate them.

### Likely Interview Questions

**Very likely**
1. How does a student join a classroom?
2. How does assignment submission work?

**Likely**
3. How do you validate an uploaded file? Is it safe?
4. Why store files on disk instead of in the database?

### Possible Follow-ups

* "When does a student see their grade?" → Only after the admin publishes the evaluation.
* "What if the upload succeeds but the database write fails?" → The stored file is deleted so no orphan file is left behind.

---

## 7. Feature 5 — AI Assignment Feedback

### What It Does

Gives an admin a **draft** review of a student's submission — a summary, grammar notes, missing topics
and a suggested score — to speed up grading.

### Why It Exists

Grading long written submissions is slow. This gives the admin a starting point. It does **not** grade
automatically, and that framing matters in an interview.

### Understand It

The flow is deliberately manual and admin-only:

* It is triggered by the admin pressing a button — **not** automatically when a student submits.
* The service confirms the caller really is an admin **of that classroom**, by checking the
  `UserClassrooms` table for a row with role `Admin`.
* Only **TXT** and **PDF** submissions can be analysed. TXT is read directly; PDF text is extracted with the
  **PdfPig** library. Any other extension gets a clear error telling the admin to use TXT or PDF.
* The student sees the AI feedback only after the admin **publishes** the evaluation.

The design detail worth mentioning is the **`IAiProvider` interface**. The feedback service does not know
which AI vendor it is talking to — it builds a prompt and hands it to whatever provider is registered in
`Program.cs`. Today that is `NvidiaProvider`, injected as a typed `HttpClient`.

### How It Works

```text
Admin opens a submission and clicks "Generate AI feedback"
        ↓
POST /api/v1/submissions/{id}/ai-feedback
        ↓
Service checks the caller is an admin of that classroom
        ↓
Checks the API key is configured
        ↓
Reads the submission text  (TXT directly, PDF via PdfPig)
        ↓  no extractable text → clear error, nothing saved
        ↓
PromptBuilder assembles a structured prompt
(assignment title + description + the student's text)
        ↓
IAiProvider.SendAsync → HTTP POST to the NVIDIA endpoint
        ↓  provider failure → logged and surfaced as an error, nothing saved
        ↓
AiResponseParser turns the raw text into fields:
Summary, GrammarFeedback, RubricCoverage, MissingTopics,
SuggestedScore, OverallRecommendation, Disclaimer
        ↓  empty summary → treated as a failure, not saved as empty feedback
        ↓
Saved to SubmissionAiFeedback (one row per submission — regenerating updates it)
        ↓
Student sees it only after the admin publishes the evaluation
```

Notice that **every failure path leaves the submission untouched** — the admin can always still grade
manually.

### How It Is Implemented

* `Services/Ai/AiFeedbackService.cs` — the orchestration and all the guards
* `Services/Ai/PromptBuilder.cs` — builds the structured prompt
* `Services/Ai/NvidiaProvider.cs` — the HTTP call, registered as `AddHttpClient<IAiProvider, NvidiaProvider>()`
* `Services/Ai/AiResponseParser.cs` — turns the model's text into typed fields
* `Configuration/AiOptions.cs` — model, base URL, timeout, API key (from environment variables)
* `Entities/SubmissionAiFeedback.cs` — unique per submission

### Technical Terms to Remember

* **Interface / dependency inversion** — `IAiProvider` lets the service depend on an abstraction, not a vendor.
* **Typed HttpClient** — an `HttpClient` registered for one service, with its configuration attached.
* **Prompt** — the structured text sent to the model.
* **Text extraction** — pulling readable text out of a PDF (here, with PdfPig).

### Explain It to the Interviewer

> "The idea is to help the admin grade faster, not to grade automatically. The admin opens a
> submission and asks for AI feedback. The backend checks they're actually an admin of that classroom,
> reads the text out of the file — TXT directly, PDF through a library called PdfPig — and builds a prompt
> with the assignment details and the student's answer. That goes out to a hosted model through an
> `IAiProvider` interface, so the service isn't tied to one vendor. The response is parsed into fields like
> summary, missing topics and a suggested score, and saved against the submission. The student only sees it
> once the admin publishes the evaluation, and the admin still decides the final grade."

### Mention Initially

* It assists the admin; it does not auto-grade.
* Admin-triggered and admin-only.
* Prompt → model → parsed into structured fields → stored.
* The student sees it only after publish.

### Don't Explain Initially

* The exact prompt wording.
* Every field on `SubmissionAiFeedback`.
* The parser's internals.

### If Asked Deeper

* **"Do you support multiple providers?"** — Careful. Say: *there is an `IAiProvider` abstraction with one
  provider registered.* A `GeminiProvider` file exists but nothing selects between them, so it is dead code.
  Do not claim multi-provider support.
* **"What if the AI call fails?"** — Walk through the guards above. Nothing is saved, grading is unaffected.
* **"How is the API key protected?"** — Read from a `.env` file or environment variables into configuration,
  never committed, only used server-side, and `Program.cs` refuses to start if it is missing.

### Likely Interview Questions

**Very likely**
1. What does the AI feedback feature actually do?
2. What happens if the AI service fails or returns something unusable?

**Likely**
3. Why is there an interface in front of the AI provider?
4. How do you protect the API key?

### Possible Follow-ups

* "Can a student trigger it?" → No — admin of that classroom only.
* "Does it work on any file?" → TXT and PDF only.

---

## 8. Feature 6 — Audit Logging

### What It Does

Records every important action — who did it, what they did it to, and when — in a table that cannot be
edited or deleted afterwards.

### Why It Exists

So there is a reliable answer to "who published this module?", "who graded this?", "who changed this quiz?".
Without it, you cannot prove what happened.

### Understand It

Think of it as a diary that only ever gets new lines added — never edited, never erased. That property has
a name: **append-only**.

Each entry stores: timestamp (set automatically at creation, in UTC), the user's id, username and role, the
**action type** (an enum like `LOGIN`, `MODULE_COMPLETED`, `DEPENDENCY_ADDED`, `CERTIFICATE_GENERATED`), the
**entity type** and **entity id** it refers to, a human-readable description, and optional old/new values
plus extra data.

The part worth being proud of is *how* immutability is enforced. It is not a rule that every developer has
to remember — it is enforced in one place, in `ApplicationDbContext`. `GuardAuditLogImmutability()` inspects
the change tracker on every save, and if any `AuditLog` entry is marked as Modified or Deleted, it throws.
So even a bug elsewhere in the code cannot rewrite history.

### How It Works

```text
User performs an action  (e.g. admin publishes a module)
        ↓
Controller → Service does the real work and saves
        ↓
Service immediately calls _auditLog.LogAsync(action, entityType, entityId, description, ...)
        ↓
AuditLogService works out WHO the actor is:
either passed in explicitly, or read from HttpContext.User claims
(NameIdentifier, email, role) via IHttpContextAccessor
        ↓
A new AuditLog row is added and saved
        ↓
Later:  GET /api/v1/history/me     → only that user's own entries
        GET /api/v1/history/admin  → all entries, SuperAdmin only
```

Both history endpoints support paging (max page size 100) and filters — action type, date range, search —
newest first. The admin view additionally resolves whether the referenced entity still **exists**, by batch
querying per entity type — so an admin can see that a log line refers to a path that has since been deleted.

`IAuditLogService` is injected into a dozen or more services: auth, learning path, classroom, progress,
quiz, attempt, question bank, community, AI feedback, admin.

### How It Is Implemented

* `Services/Audit/AuditLogService.cs` — `LogAsync`, `ResolveActor`, `GetMyHistoryAsync`, `GetAdminHistoryAsync`, `ResolveEntityExistenceAsync`
* `Entities/AuditLog.cs` + the `AuditAction` enum
* `Data/ApplicationDbContext.cs` — `GuardAuditLogImmutability()` inside the overridden `SaveChanges` / `SaveChangesAsync`
* `Controllers/HistoryController.cs`
* Frontend: `historyService` → history page / admin history tab

### Technical Terms to Remember

* **Append-only / immutable** — rows can be added but never changed or removed.
* **Change tracker** — EF Core's record of what is about to be inserted, updated or deleted on save.
* **`IHttpContextAccessor`** — lets a service read the current request's user claims.
* **Enum** — `AuditAction`, the fixed list of action types.

### Explain It to the Interviewer

> "Every important action writes an audit entry — who did it, what entity it was on, and when. Services call
> a shared `AuditLogService`, which works out the current user from the JWT claims if it isn't passed in
> explicitly. The part I like is that the log is append-only, and that's enforced in the DbContext: I
> override `SaveChanges` and throw if anything tries to modify or delete an audit row. So it isn't something
> a developer has to remember — the database context refuses it. Users can see their own history, and a
> SuperAdmin can see everything with filters."

### Mention Initially

* What is recorded and why.
* One shared service, injected everywhere.
* Append-only, enforced centrally in the DbContext.
* Own history vs SuperAdmin history.

### Don't Explain Initially

* All eleven fields of the entity.
* The full `AuditAction` enum.
* The `EntityExists` batch resolution.

### If Asked Deeper

* **"How exactly is immutability enforced?"** — The change tracker check described above.
* **"How does the service know who did it?"** — `IHttpContextAccessor` reads the claims that the JWT
  middleware put on `HttpContext.User`; the caller can also pass the user id explicitly (needed during
  registration, before a token exists).
* **"What is `EntityExists` for?"** — It tells the admin whether the thing a log line points at still exists.

### Likely Interview Questions

**Very likely**
1. What does your audit logging do, and why did you add it?
2. How do you make sure audit logs cannot be tampered with?

**Likely**
3. How does the service know which user performed the action?
4. Who can see the history?

### Possible Follow-ups

* "Doesn't logging on every action slow things down?" → It is one extra insert after the main save; acceptable at this scale, and it could be moved to a background queue if it ever mattered.

---

## 9. Feature 7 — Authentication and Role Management

### What It Does

Handles register, login, token refresh and logout, and controls what each role is allowed to do.

### Why It Exists

Every other feature depends on knowing who is calling and what they are allowed to do.

### Understand It

**Registration** creates the user through ASP.NET Identity, which **hashes** the password (PBKDF2) — the
plain password is never stored. Every new user is given the `Student` role. Admin are
assigned separately; roles themselves are seeded at startup by `RoleSeeder`, along with a default admin
account. There is also a `SuperAdmin` **flag** on the user (`IsSuperAdmin`), used for the admin history view.

**Login** finds the user by email, checks the password hash, then checks the account status — Deleted,
Inactive and Invalid accounts are refused. A useful detail: an unknown email and a wrong password return
the **same** message, "Invalid credentials", so an attacker cannot use the error to discover which emails
are registered. The real reason is written to the audit log instead.

**Two tokens are issued, and the difference matters:**

* **Access token** — a JWT, signed with HS256, valid for **60 minutes**, carrying claims: `sub` (user id),
  `email`, `jti` (unique token id), first name, last name, and one claim per role. It is **stateless** —
  the API validates the signature and needs no database lookup to know who you are.
* **Refresh token** — 64 cryptographically random bytes, base64 encoded, valid **7 days**, and **stored in
  the database**. Because it is stored, it can be revoked — something you cannot do to a JWT.

The refresh flow uses **rotation**: the old refresh token is marked revoked and a brand-new pair is issued.
Each refresh token is effectively single-use. Logging out revokes every un-revoked token for that user.

**Authentication vs authorization**, in this project's own terms:

* *Authentication* = who you are → validating the JWT.
* *Authorization* = what you may do → role claims, plus the policies `AdminOnly`.
* And beyond roles there is **ownership**: editing a learning path requires being its creator, not merely
  being an admin. That extra layer is worth mentioning — it is what makes the answer sound real.

### How It Works

```text
Login  →  password checked against the stored hash  →  account status checked
       →  roles loaded  →  access token + refresh token issued
       →  refresh token row saved  →  audit entry written

Every later request  →  Authorization: Bearer <token>
       →  middleware validates signature, issuer, audience, expiry
       →  claims placed on HttpContext.User
       →  controller reads the user id from the claims

Token expires (60 min)  →  API returns 401
       →  the Axios interceptor calls /auth/refresh ONCE
          (an isRefreshing flag plus a failedQueue hold the other requests)
       →  old refresh token revoked, new pair issued
       →  queued requests retried with the new token
       →  refresh itself fails → tokens cleared → redirect to login
```

That queue exists for a real reason: if ten requests fail with 401 at the same moment, without it each one
would fire its own refresh call.

### How It Is Implemented

* `Services/Auth/AuthService.cs` — `RegisterAsync`, `LoginAsync`, `RefreshTokenAsync`, `RevokeTokenAsync`
* `Authentication/Jwt/JwtTokenGenerator.cs` — `GenerateAccessToken`, `GenerateRefreshToken`
* `Program.cs` — Identity options, JWT validation parameters (`ClockSkew = TimeSpan.Zero`), the `AdminOnly` policy
* `Data/Seeders/RoleSeeder.cs`, `AdminSeeder.cs`
* Frontend: `apiClient.ts` (interceptors), `tokenUtils.ts`, `authSlice.ts`, `useAuth.ts`, `ProtectedRoute` / `AdminRoute`

### Technical Terms to Remember

* **JWT** — a signed token proving who the caller is; carries claims, validated without a database hit.
* **Claims** — the pieces of information inside the token (user id, email, roles).
* **Hashing** — one-way transformation of the password; say *hashed*, not *encrypted*.
* **Token rotation** — each refresh issues a new refresh token and revokes the old one.
* **Policy** — a named authorization rule, here `AdminOnly`.

### Explain It to the Interviewer

> "When a user logs in, the backend checks the password through ASP.NET Identity — only the hash is stored —
> and then issues two tokens. The access token is a JWT that's valid for an hour and carries the user id,
> email and roles, so the API can identify the caller without hitting the database. The refresh token lasts
> seven days and is stored in the database, so it can be revoked. When the access token expires the frontend
> calls the refresh endpoint, the old refresh token is revoked and a new pair is issued. On the API side,
> `[Authorize]` means any logged-in user and the role policies handle admin and admin endpoints — and
> for things like editing a learning path I also check ownership, not just the role."

### Mention Initially

* Passwords are hashed by Identity, never stored.
* Two tokens, and why: short-lived JWT, revocable refresh token.
* Roles in the token; policies on the endpoints.
* Ownership checks on top of roles.

### Don't Explain Initially

* The exact claim names.
* `ClockSkew = TimeSpan.Zero`.
* The internals of the refresh queue.

### If Asked Deeper

* **`ClockSkew = TimeSpan.Zero`** — by default ASP.NET Core allows five minutes of clock tolerance, so a
  token would stay valid past its stated expiry. Setting it to zero makes the 60 minutes exact.
* **Where tokens are stored on the client** — check `tokenUtils.ts` and state it exactly. The honest general
  point: browser storage is readable by JavaScript, so it is exposed if the app has an XSS hole; an HttpOnly
  cookie for the refresh token would be stronger.
* **Brute-force protection — know this before you are asked.** Identity's lockout is configured in
  `Program.cs` (5 attempts, 5 minutes), but login uses `UserManager.CheckPasswordAsync`, which does **not**
  increment the failed-attempt counter — that needs `SignInManager.PasswordSignInAsync`. So the policy exists
  in configuration but is not actually driven by the login path. The safe answer: *"There's a rate limiter,
  failed logins are audited, and lockout is configured — though the login path uses `CheckPasswordAsync`, so
  the counter isn't incremented. I'd fix that by moving to `SignInManager`."*

### Likely Interview Questions

**Very likely**
1. How does login work in your project?
2. What is JWT and why did you use it?
3. What is the difference between authentication and authorization here?
4. Why do you need a refresh token?
5. What happens when the access token expires while the user is working?

**Likely**
6. How are roles assigned and enforced?
7. How are passwords stored?

**Possible**
8. Where do you store tokens on the client, and is that safe?

### Possible Follow-ups

* "How does the backend know which user is logged in?" → From the `sub` / NameIdentifier claim on `HttpContext.User`, after the middleware validates the token.
* "Can the frontend route guards be bypassed?" → Yes, and it does not matter — every endpoint enforces the same rules server-side.

---

## 10. Feature 8 — Community

### What It Does

A discussion area: posts with code snippets and tags, threaded comments, votes, groups, reporting and
moderation, plus a trending feed.

### Why It Exists

So learners can ask questions and share code around the paths they are studying, rather than being alone
in a course.

### Understand It

A post carries a title, content, an optional **code snippet** with its programming language, tags, a
category, and optionally a learning path or a group it belongs to. Comments can have a **parent comment**,
which is how replies nest — the `Comment` table references itself.

Votes are stored per user per post (and per comment), so the same person cannot vote twice; changing a vote
adjusts the score accordingly.

The **trending** feed is a small formula rather than a plain sort:

```text
trendingScore = score / (ageInHours + 2) ^ 1.5
```

A post with a good score that is only an hour old outranks an older post with the same score — so the feed
stays fresh instead of being permanently topped by the all-time best post.

This is also the one area with a **repository** — `CommunityRepository` — because feeds need shared paging,
filtering and sorting logic that several service methods reuse. Everywhere else, services use the
`DbContext` directly. If you are asked about your data-access pattern, that inconsistency is real and worth
explaining rather than hiding.

### How It Is Implemented

* `Services/Community/CommunityService.cs` — posts, comments, votes, groups, reports, moderation, trending
* `Repositories/CommunityRepository.cs` — `PagedResult`, filters and sorting
* Entities: `Post`, `Comment` (self-referencing `ParentCommentId`), `PostVote`, `CommentVote`, `Group`, `GroupMember`, `Report`
* Frontend: community pages, `communitySlice.ts`, `useCommunity` / `useGroups`

### Technical Terms to Remember

* **Pagination** — returning one page of results plus the total, instead of everything.
* **Self-referencing relationship** — a comment pointing at its parent comment.
* **Race condition** — a slower earlier response arriving after a newer one; handled here by tracking a request id and using an abort signal.

### Explain It to the Interviewer

> "The community section has posts with code snippets and tags, threaded comments, votes, and groups. Feeds
> are paged and filtered through a repository, because that logic is reused in several places. Trending isn't
> a plain sort by score — it divides the score by the age of the post, so newer posts with good engagement
> come up rather than the feed being stuck on one old post."

### Mention Initially

* Posts, threaded comments, votes, groups.
* Feeds are paged and filtered.
* Trending is score adjusted by age.

### Don't Explain Initially

* The exact vote arithmetic.
* Moderation actions like pinning and banning.
* The exact trending exponent.

### If Asked Deeper

* **The request-id / abort-signal race fix** — if you personally worked on it, this is a strong debugging
  story. If you did not, do not claim it.
* **Why a repository only here** — shared paging, filtering and sorting across several methods.

### Likely Interview Questions

**Likely**
1. What does the community feature do?
2. How is the feed paginated?

**Possible**
3. How does trending work?
4. Do you use the repository pattern?

---

## 11. Architecture

```text
React SPA (MUI + Redux Toolkit + Router + Axios)
      served by Nginx on port 80, which proxies /api/* to the API
              ↓
ASP.NET Core Web API on port 5000  (.NET 10)
      Middleware: Exception → Logging → RateLimit (50/min) → CORS → Authentication → Authorization
              ↓
Controllers (api/v1/*)  — [Authorize], FluentValidation, ApiResponse<T>
              ↓
Services (~23)  — all business rules, plus audit logging
              ↓
EF Core DbContext (IdentityDbContext<User>, 30 DbSets)
              ↓
SQL Server 2022
```

**Why layered:** rules like the cycle check and the prerequisite check are needed by more than one
controller, so they belong in services. Keeping data access behind services means a controller never
contains a query, and services can be unit-tested — which the `backend.Tests` project relies on.

**Two rules that hold everywhere:**

* Controllers validate and delegate; services decide.
* Every response uses `ApiResponse<T>` — including model-validation failures, which are converted into the
  same envelope instead of ASP.NET's default problem-details format.

### Error handling

All of it is centralised in `ExceptionMiddleware`, registered first so it wraps everything after it.
Services throw meaningful exception types and the middleware maps them:

| Thrown by a service | Response | Meaning |
|---|---|---|
| `ArgumentException` | 400 | Bad input or a rule violation; the message is shown to the user |
| `UnauthorizedAccessException` | 403 | Logged in, but not allowed |
| `KeyNotFoundException` | 404 | The record does not exist |
| Foreign-key violation (SQL error 547) | 409 | Cannot delete — other data depends on it |
| Anything else | 500 | Logged in full; the user sees a safe generic message |

The point to say out loud: unexpected exceptions are logged completely but **never** returned to the
client, so stack traces and SQL details do not leak.

---

## 12. Frontend

**Structure is by feature:** `pages/` (screens), `components/` (reusable pieces), `services/` (API calls),
`redux/` (slices and selectors), `routes/` (routing and guards), `hooks/` (`useAuth`, `useCommunity`, …),
`layouts/` (page shells), `theme/` (light and dark palettes).

**State:** two kinds, handled differently.

* Shared across screens — the logged-in user, paths, classrooms, community, notifications — lives in
  **Redux Toolkit** slices, with async thunks for API calls (which give you pending / fulfilled / rejected
  states for free, driving loaders and error messages).
* Local to one component — a form field, whether a dialog is open — stays in `useState`. Putting that in
  Redux would be more code for no benefit.

**Routing:** React Router 6 with about 34 routes, each page loaded with `React.lazy` so the initial bundle
stays small. `ProtectedRoute` redirects to login when there is no authenticated user and remembers where
the user was trying to go; `AdminRoute` additionally check the role.

Say this whenever route guards come up: **the guards are for user experience.** Someone who bypasses them
still gets nothing, because every endpoint checks the token and role on the server.

**API communication:** one shared Axios instance in `apiClient.ts` holds the base URL, the bearer-token
request interceptor, and the 401 refresh handling — so no component calls Axios directly, and the refresh
logic exists in exactly one place.

---

## 13. Backend and Database

**Backend:** 17 controllers under `api/v1/`, ~23 services, AutoMapper for entity → DTO mapping (so the API
never returns entities directly, which keeps password hashes and navigation properties out of responses),
and 12 FluentValidation validators.

**Two layers of validation, and it is worth naming both:**

* **FluentValidation** checks the shape of the request — required fields, lengths, formats — and failures
  come back as 400 in the standard envelope *before* any service code runs.
* **Business rules that need database state** — "prerequisites must be complete", "this would create a
  cycle", "the quiz must be published before linking" — are checked inside the services, because a
  validator cannot know them.

**Database:** SQL Server, EF Core code-first, 41 entities, 31 configuration classes, 11 migrations.

The spine of the schema — give this, not all 41 entities:

* `User` (extends the Identity user) is the centre: it owns paths, progress, classrooms, submissions, posts and certificates.
* `LearningPath` → many `Module`; `ModuleDependency` is the self-referencing join on `Module`.
* `Progress` links a user to a module; `Certificate` links a user to a learning path.
* `Classroom` ↔ users through `UserClassroom`; `Classroom` → `Assignment` → `Submission` → `SubmissionAiFeedback`.
* Quiz side: `QuestionBank` → `Quiz` → `Question` → `Option`, with `QuizAttempt` → `StudentAnswer`.

**Constraints worth mentioning:** unique indexes on `Progress(UserId, ModuleId)`,
`Certificate(UserId, LearningPathId)` and `Classroom.InviteCode`. Each encodes a rule the application also
checks — but the database enforces it even if two requests arrive simultaneously.

**Migrations:** change an entity, run `dotnet ef migrations add <Name>`, and EF generates the up and down
steps. `Program.cs` applies pending migrations at startup and then seeds roles and the admin account.
Convenient for a project and for Docker; in a real deployment you would apply migrations as a controlled
step so multiple starting instances do not race.

---

## 14. Important Technical Decisions

Explain each as **decision → why → trade-off**. Where the personal reasoning is yours, it is marked.

| Decision | Why (supported by the code) | Trade-off / honest note |
|---|---|---|
| **Graph instead of an ordered list** | One module can have several prerequisites, or none; a list can only express a straight line | More complex than a list; needs cycle validation |
| **Validate the cycle before saving** | A stored cycle would be a permanently broken path; cleaning up afterwards means hunting the bad edge | Costs one extra read before the write |
| **Recalculate unlock status on every fetch** | Always correct, no stale cached state, no background job | Slightly more work per request |
| **One `ApiResponse<T>` envelope everywhere** | The frontend has exactly one shape to handle, including validation errors | Slightly more verbose responses |
| **Central `ExceptionMiddleware`** | No try/catch scattered through controllers; consistent status codes; no leaked internals | Services must throw meaningful exception types |
| **Store a seed per quiz attempt** | Reproduces both question and option order from one number, which makes the review screen possible | The shuffle must stay deterministic |
| **`IAiProvider` interface** | The feedback logic does not depend on one vendor | Only one provider is actually registered |
| **Audit immutability in the DbContext** | Enforced once, centrally, instead of relying on every developer | Any legitimate correction is impossible by design — which is the point |
| **Files on disk, metadata in the database** | Keeps the database small; backups stay fast | Local disk does not survive a replaced container or work across servers |
| **Repository only for community** | Feeds reuse paging, filtering and sorting logic | Inconsistent with the rest of the codebase — explain it rather than hide it |
| **JWT + refresh tokens** | Stateless API; short-lived access token; the refresh token is revocable because it is stored | Client-side token storage has its own exposure |

> **Personal reasoning: needs my input.** Where a decision was simply the standard ASP.NET Core way of doing
> things that you followed, say so — that is a perfectly good fresher answer, and far better than inventing
> a deep architectural justification you would then have to defend.

---

## 15. Challenges — Problem → Approach → Why → Result

These come from the factual document. **Use only the ones you actually lived through.**

### Challenge 1 — Inconsistent, confusing error responses

* **Problem:** every service threw different exception types, and the frontend was receiving raw validation and SQL text.
* **Approach:** introduced the `ApiResponse<T>` wrapper, mapped exception types to status codes in `ExceptionMiddleware`, and overrode `InvalidModelStateResponseFactory` so model-validation failures use the same envelope. The question-bank upload returns a typed `ValidationError[]` with the question index.
* **Why:** the frontend should never have to guess at the shape of an error.
* **Result:** one format everywhere; the UI can always display a human-readable message.

### Challenge 2 — Deleting a path left orphaned data

* **Problem:** deleting a learning path hit foreign-key violations (SQL error 547) and risked orphan rows, because cascade and restrict rules were mixed.
* **Approach:** `DeleteAsync` now uses EF Core's execution strategy with an explicit transaction (needed because `EnableRetryOnFailure` is on) and deletes in a foreign-key-safe order, after checking whether classrooms or certificates still reference the path.
* **Why:** the delete has to be all-or-nothing.
* **Result:** an atomic delete, and a clear 409 message when something still depends on the path.

### Challenge 3 — The DAG was not actually enforcing the lock

* **Problem:** the dependency data existed, but the UI showed every module and the API could be called directly to bypass the lock. Assignments had a separate sequential rule that was not connected to it.
* **Approach:** added the dual `dagUnlocked && sequentialUnlocked` check in `LearningPathService`, and repeated the same guard in `AttemptService` and `ClassroomService.EnsureAssignmentUnlockedAsync`, throwing `UnauthorizedAccessException` → 403. The frontend module card uses `isUnlocked` to disable navigation and show a message, and locked assignments no longer expose their description.
* **Why:** a rule enforced only in the UI is not a rule.
* **Result:** prerequisites are genuinely enforced end to end.

### Challenge 4 — Valid question banks being rejected with no guidance

* **Problem:** the JSON validation was strict, but the UI only said "Upload failed", so an admin had no idea what to fix.
* **Approach:** the backend returns a list of `ValidationError`s with a per-question index and reason. The admin page now shows the JSON template and the validation rules with a copy button, and displays the specific server errors.
* **Why:** validation that does not explain itself just moves the problem to the user.
* **Result:** admins can correct the file themselves.

### Challenge 5 — The community felt like plain comments

* **Problem:** posts were just title plus content, and hard to discover.
* **Approach:** added code snippets with a language, tags, categories, group and path association, voting, a trending score based on score and age, filters and sorting, pagination through `CommunityRepository`, and a request-id plus abort-signal fix for the race when filters changed quickly.
* **Why:** a learning community needs code sharing and discovery, not a comment box.
* **Result:** a searchable, social feed.

### Challenge 6 — Making audit logging consistent and tamper-proof

* **Problem:** every service needed to log consistently, the actor had to be resolved even when a service was called without an explicit user id, and history had to be impossible to edit.
* **Approach:** a central `AuditLogService` with `ResolveActor` reading from `HttpContext.User`, injected everywhere; `GuardAuditLogImmutability` in the DbContext blocking modified or deleted audit rows; history split into a user-scoped endpoint and a SuperAdmin endpoint with an entity-existence check.
* **Why:** an audit trail that can be edited is not an audit trail.
* **Result:** a complete, append-only history.

---

## 16. Personal Contribution

> **Personal Contribution: needs my input.**

The code does not record who wrote what, so nothing here can be filled in for you. Prepare two things:

1. **What you personally built.** Be specific. *"I built the learning path and the dependency validation on
   the backend, plus the path detail screen"* is strong. *"I worked on everything"* is weak and invites a
   question about a part you never touched. If parts were done with help or guidance, say so — you can still
   explain the flow.

2. **The part you can explain in the most detail.** Interviewers usually go there next, so choose the ground
   you want to be standing on. For most people with this project, that is the DAG.

Then prepare your **hardest problem** as a real story, in four beats, under 90 seconds: what the problem was
→ how you investigated it → what the cause turned out to be → what you changed. A small, specific,
genuinely-solved problem beats a grand vague one every time.

**Do not invent a challenge.** If you need prompting, section 15 lists what the code suggests was hard — but
use only what you actually lived through.

---

## 17. What I Learned

Adapt these in your own words — they are drawn from the project's real history:

* Designing a graph is easy on paper and harder in code — you need both explicit dependencies and a simple fallback order for modules that have none.
* One consistent API response format saves days of frontend error handling.
* Transactions matter: without an ordered delete, the database is left with orphans.
* Showing the expected file format in the UI reduces failed uploads more than stricter backend validation does.
* Centralising cross-cutting concerns — audit logging, exception handling, rate limiting — keeps controllers thin.

---

## 18. Project-Level Likely Questions

**Very likely**
1. What is LearnPath and what problem does it solve?
2. What are the major features?
3. What is the most interesting or unique part of the project?
4. Explain the overall architecture.
5. Walk me through what happens when a user performs one action, end to end.
6. How does the frontend communicate with the backend?
7. How does login work? What is JWT and why did you use it?
8. How do module dependencies work, and how do you prevent cycles?
9. Describe your database design.
10. What is EF Core and why did you use it?
11. What is dependency injection and where did you use it?
12. How is error handling done?
13. What did you personally build? *(yours)*
14. What was the biggest challenge? *(yours)*

**Likely**
15. Why this architecture? Do you use a repository pattern?
16. How is input validated?
17. What is middleware and why does the order matter?
18. What security measures does the project have?
19. Did you write tests? How would you deploy this?
20. What would you improve if you continued? *(yours)*

---

## 19. Project-Level Follow-ups

* "How does the backend know which user is logged in?" → The claims on `HttpContext.User`, after the JWT middleware validates the token.
* "What happens when the token expires?" → 401 → one refresh call, other requests queued → retried with the new token → if refresh fails, redirect to login.
* "What if someone calls the API directly, skipping your UI?" → Every rule — unlock, role, ownership — is enforced server-side; the UI only hides things.
* "What happens if a record does not exist?" → `KeyNotFoundException` → 404 with the message.
* "What if two users do the same thing at once?" → Application checks first, unique indexes underneath as the real guarantee.
* "What breaks if you had far more users?" → In-memory rate limiting is per-instance; uploads go to local disk; some list endpoints do not page; no caching. The JWT design does scale, since any instance can validate a token.
* "Why did you choose this technology?" → Give the technical reason, and if the honest reason is familiarity, say that too.

---

## 20. Important Things I Must Know

1. **Unlock rule:** `isUnlocked = dagUnlocked && sequentialUnlocked` — prerequisites, with a fallback to previous-by-order when a module has none.
2. **Quiz randomisation:** a stored seed plus a Fisher-Yates shuffle, which is what makes an attempt reproducible for review.
3. **Certificate:** a **record** with a URL string. **Not a PDF.**
4. **AI feedback:** admin-triggered, TXT and PDF only, NVIDIA provider only (Gemini is dead code); students see it only after the evaluation is published.
5. **Audit logs:** append-only, enforced by overriding `SaveChanges` in the DbContext.
6. **Tokens:** access 60 minutes, refresh 7 days with rotation; `ClockSkew = Zero`.
7. **Size limits:** `[RequestSizeLimit(6 MB)]` is the HTTP cap; the actual file validation limit is **5 MB**
   (both for uploads and for question-bank JSON). If you quote a number, quote the 5 MB and be ready to
   explain the 6 MB.
8. **Classroom joins:** 8-character invite code from a GUID, unique-indexed.
9. **Assignment unlocking** is grade-based (previous grade ≥ 5) — a *different* rule from module unlocking.
10. **Lockout gap:** lockout is configured but the login path does not increment the failure counter. Know
    this before you are asked.
11. **Notifications are fetched, not pushed** — no SignalR.
12. **Rate limiting is in-memory** — per instance only.

---

## 21. Final Quick Revision

The evening before, do these five things and then stop:

* [ ] Run the app and click through login → a learning path → a locked module → a quiz, so the flows are fresh.
* [ ] Run `dotnet test` so you know the truth about your tests. Never claim a passing suite you have not seen pass.
* [ ] Say the DAG explanation out loud twice, without reading it.
* [ ] Re-read section 20 — the facts most likely to be checked.
* [ ] Write out your answer to *"what did you personally build"* and *"what was the hardest problem"* in your own words.

**Two habits that matter more than any single answer:**

* **Answer, then stop.** Silence after a complete answer is the interviewer's turn, not a gap you must fill.
* **When you do not know, say what you do know and where you would look.** That is what the job actually
  looks like, and it reads far better than a confident guess.
