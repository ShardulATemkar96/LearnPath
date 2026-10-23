# LEARNPATH — PROJECT INTERVIEW UNDERSTANDING & PREPARATION GUIDE

This guide is designed to help you explain the **LearnPath** project naturally, confidently, and accurately in an interview. It translates the raw technical facts and source code into human-understandable concepts while preserving the correct implementation architecture and terminology.

---

## 1. LearnPath — Big Picture

### What is LearnPath?
LearnPath is an interactive learning platform designed to structure educational content. Instead of displaying courses as simple lists that students can scroll through and open randomly, LearnPath organizes modules into structured paths where topics unlock logically as progress is made.

### What Problem Does It Solve?
* **Lack of Structure:** Students are often overwhelmed by massive lists of resources. Without guidance, beginners open advanced modules too early and get confused, while fast learners waste time trying to figure out what to study next.
* **Lack of Control:** Admins cannot enforce prerequisites. If a student tries to learn advanced data structures before learning basic programming concepts, the platform should prevent it.

### Why Does It Exist?
It exists to create a guided, journey-like learning experience. The system acts like a mentor, knowing exactly what topics must be completed first and dynamically guiding the student through a visual dependency map.

### How Does It Solve the Problem?
It models a learning path as a **directed graph (DAG)**. Modules are nodes, and requirements are links (edges) between them. The backend enforces these prerequisites at the API level, and the frontend renders a visual path graph where locked modules are visually grayed out. Next modules unlock automatically the moment prerequisites are completed.

### Main Features (Implemented in Code)
1. **DAG-Based Learning Path:** Creation of paths, modules, and dependencies with cycle prevention.
2. **Progress & Automatic Certificates:** Tracks completed modules and automatically issues a certificate when a path reaches 100%.
3. **Randomized Quiz Engine:** Custom quiz attempts with shuffled questions and answers to evaluate learning.
4. **AI-Driven Assignment Feedback:** Integrates Nvidia Nemotron LLM to provide automated feedback on student submissions.
5. **Classrooms & Assignments:** Code-based classrooms where admins can post assignments, grade files, and track student status.
6. **Community Forums:** Structured social feed with posts supporting code snippets, programming languages, tags, voting, and trending sorting.
7. **Role-Based Auth & Security:** Stateless JWT authentication (with Refresh Tokens) and detailed access policies for Students, Admins, and Admins.
8. **Immutable Audit Logging:** A tamper-proof database log record of all major administrative actions.

---

## 2. Overall Project Flow

The following sequence represents the end-to-end user and system flow:

```text
Admin creates path (POST /api/v1/paths)
                  ↓
Admin adds modules (POST /api/v1/paths/{id}/modules)
                  ↓
Admin links module dependencies (checks cycle in DagValidator)
                  ↓
Student logs in & receives JWT (stored in local client state)
                  ↓
Student opens path (GET /api/v1/paths/{id} -> loads graph representation)
                  ↓
Backend calculates IsUnlocked (checks user's completed modules list)
                  ↓
Student clicks module -> if locked (shows Snackbar, blocks API fetch with 403)
                  ↓
Student completes module or passes quiz -> Progress marked complete
                  ↓
System recomputes status -> Next modules unlock, certificate issued if 100%
```

---

## 3. Feature 1: DAG-Based Learning Path (Module Dependency)

### What It Does
This feature organizes modules within a learning path into a structured tree/graph instead of a flat list. It ensures that a student cannot access advanced modules without first completing their prerequisites.

### Why It Exists
In education, concepts build on top of each other. You cannot write object-oriented code without understanding basic variables. We needed a system that visually and programmatically enforces this logical order.

### Understand It
Think of it like a video game skill tree. Each node is a topic (e.g., "Variables", "Loops", "OOP"). When you first join a path, only the starting node is active. As you finish "Variables", the line leading to "Loops" lights up. If you try to skip ahead and click "OOP", the game says "Locked". 

Technically, we represent this as a **Directed Acyclic Graph (DAG)**. Directed means the connections go in one direction (A -> B). Acyclic means there are no loops (you can't have A depend on B, B depend on C, and C depend on A), because loops would lock the student out forever.

### How It Works
1. **Admin Action:** Connects Module A as a dependency of Module B.
2. **Cycle Validation:** The backend intercepts the dependency and runs a **DFS (Depth-First Search)** check to make sure this connection doesn't create a loop.
3. **Fetching the Path:** When a student opens a path, the backend pulls the list of completed modules for that student.
4. **Calculating Lock Status:** For each module, the backend checks: *Are all dependencies of this module present in the student's completed list?* If yes (or if the module has no dependencies and the previous order is complete), it sets `IsUnlocked = true`. Otherwise, `IsUnlocked = false`.
5. **Rendering & Protection:** The frontend renders an SVG diagram using node connections. If `IsUnlocked` is false, it renders the module with lower opacity and a lock icon, preventing clicks. If the student tries to bypass the UI and hit the API direct, the backend rejects it with `403 Forbidden`.

### How It Is Implemented
* **Database Entity:** [ModuleDependency.cs](file:///D:/FInal%20Project/LearnPath/backend/Entities/ModuleDependency.cs) is a join table mapping `ModuleId` (dependent) to `DependsOnModuleId` (prerequisite). Both are foreign keys linked to the `Modules` table, configured in [ModuleDependencyConfiguration.cs](file:///D:/FInal%20Project/LearnPath/backend/Configurations/ModuleDependencyConfiguration.cs).
* **Validation Logic:** [DagValidator.cs](file:///D:/FInal%20Project/LearnPath/backend/Algorithms/Graph/DagValidator.cs) implements DFS to prevent cycles. It uses `WouldCreateCycle` which walks the graph and throws an error if it finds a path back to the starting point. It also has a `TopologicalSort` function used to sort modules in correct learning order.
* **Service Layer:** [LearningPathService.cs](file:///D:/FInal%20Project/LearnPath/backend/Services/LearningPath/LearningPathService.cs) contains the core business logic. In `GetByIdAsync`, it calculates the `IsUnlocked` status for each module. In `GetModuleContentAsync`, it verifies lock status and throws an exception if bypassed.
* **Frontend Components:** 
  * [PathGraph.tsx](file:///D:/FInal%20Project/LearnPath/frontend/src/components/learningPath/PathGraph/PathGraph.tsx) draws the interactive SVG showing modules and connection arrows.
  * `LearningPathDetailPage.tsx` controls the `ModuleCard` components, displaying locked styling (gray lock icon, 0.65 opacity) and displaying a Snackbar message on blocked clicks.

### Technical Terms to Remember
* **DAG (Directed Acyclic Graph):** A graph of nodes connected by arrows (directed) with no loops (acyclic).
* **DFS (Depth-First Search):** An algorithm used to traverse the graph to verify that adding a dependency doesn't loop back to create a cycle.
* **Topological Sort:** An algorithm that orders graph nodes linearly so that for every directed edge U -> V, node U comes before V.
* **Sequential Fallback:** If an admin hasn't set explicit dependencies, the system defaults to checking if the immediate previous module (by sorting order) is completed.

### Explain It to the Interviewer
> "In LearnPath, instead of a simple linear course list, I built a DAG-based learning path system. Basically, it allows admins to connect modules with custom prerequisite requirements. The system represents these relationships as a directed graph. 
> 
> When a student loads the path, the backend evaluates their progress and calculates if each module is unlocked. If a module has prerequisites, all of them must be completed first. If there are no custom prerequisites, it falls back to a sequential order. On the frontend, I used SVG to render this map. Locked modules are shown with a gray lock icon and lower opacity, and clicking them shows a warning. To prevent API bypass, the backend checks the lock status on every module resource request and blocks access with a 403 Forbidden response if prerequisites aren't met."

### Mention Initially
* It's a graph structure (DAG) rather than a list.
* Custom dependencies are verified using a cycle detection algorithm.
* Unlock logic is enforced on both the frontend UI and the backend API.

### Don't Explain Initially
* Details of the DFS recursion stack or `inStack` array.
* Low-level EF Core SQL joins used to fetch modules and dependencies.
* Opacity values or CSS layout details of the cards.

### If Asked Deeper
* **How does DFS cycle prevention work?** In [DagValidator.cs](file:///D:/FInal%20Project/LearnPath/backend/Algorithms/Graph/DagValidator.cs), `WouldCreateCycle` starts at the proposed prerequisite module and performs a depth-first traversal. If it manages to reach the dependent module, it means adding this edge would create a loop, so the system rejects it.
* **How does sequential fallback work?** If a module's dependencies list is empty, the code in [LearningPathService.cs](file:///D:/FInal%20Project/LearnPath/backend/Services/LearningPath/LearningPathService.cs) identifies the index of the module in the sorted list. It then checks if the module immediately preceding it (by order index) exists in the user's list of completed module IDs.

### Likely Interview Questions
* **Why did you use a DAG instead of a simple sorted list?** (Very Likely)
  * *Answer Direction:* Lists are too rigid. A graph allows a module to have multiple prerequisite requirements, or let students start multiple paths in parallel if they don't depend on each other, while still guaranteeing no circular logic loops exist.
* **How do you prevent a student from accessing a locked module by calling the API directly?** (Likely)
  * *Answer Direction:* The lock verification is centralized in the backend service layer (`GetModuleContentAsync`). Even if the frontend UI is bypassed, the API checks the student's progress in the database and throws an unauthorized exception, which our global middleware translates into a `403 Forbidden` response.

---

## 4. Feature 2: Randomized Quiz Engine

### What It Does
The quiz engine allows admins to link quizzes to modules. Students must pass these quizzes to mark a module as completed and unlock subsequent topics. The engine randomizes question and answer choices for each attempt.

### Why It Exists
We needed a reliable way to assess whether a student actually understood a topic. To prevent cheating or memorizing answer positions, every student attempt needs to feel unique while still being fair and reviewable.

### Understand It
Imagine taking an exam where the order of questions is shuffled, and the options (A, B, C, D) are also shuffled. 

To make this work in a web app:
* The backend uploads a large JSON file representing a "Question Bank".
* An admin creates a Quiz by choosing how many questions to pull from that bank, specifying the difficulty filter.
* When a student starts an attempt, the backend generates a random number (seed) and saves it with the attempt.
* We use this seed to shuffle the questions and options. Because we store the seed, if the student refreshes or reviews their quiz later, the questions and options remain in the *exact same randomized order* they saw during the test.

### How It Works
1. **Admin JSON Upload:** Admins upload a JSON file (up to 5MB) of questions. The system validates formatting, options, correct answers, and difficulties.
2. **Quiz Linking:** Quizzes are defined with limits (time limit, pass percentage, max attempts) and linked to modules.
3. **Starting the Quiz:** The system checks prerequisites. If unlocked, it creates a new attempt record with a randomized seed (`Random.Shared.Next()`).
4. **Fisher-Yates Shuffling:** The system fetches matching questions. It shuffles the question list using the attempt's seed. For each question, it shuffles the options using a seed calculated as `seed + QuestionId`.
5. **Answering & Grading:** The student submits answers. The backend matches the selected `OptionId` against the correct option in the database, calculates the percentage, and determines if they passed.
6. **Progression:** If they pass, the system automatically triggers progress completion for the module.

### How It Is Implemented
* **Core Services:** 
  * [QuestionBankService.cs](file:///D:/FInal%20Project/LearnPath/backend/Services/QuestionBank/QuestionBankService.cs) handles question bank versioning and updates.
  * [QuizService.cs](file:///D:/FInal%20Project/LearnPath/backend/Services/Quiz/QuizService.cs) manages quiz states (Draft -> Published -> Archived) and locks them to modules.
  * [AttemptService.cs](file:///D:/FInal%20Project/LearnPath/backend/Services/Attempt/AttemptService.cs) generates attempts, shuffles data, and grades submissions.
  * [JsonValidationService.cs](file:///D:/FInal%20Project/LearnPath/backend/Services/Validation/JsonValidationService.cs) processes the uploaded JSON file and validates schema rules.
* **Shuffling Algorithm:** In [AttemptService.cs](file:///D:/FInal%20Project/LearnPath/backend/Services/Attempt/AttemptService.cs#L368), the `ShuffleList` method implements the Fisher-Yates algorithm using a `new Random(seed)` instance.
* **Controllers:** `QuestionBankController.cs`, `QuizController.cs`, and `AttemptController.cs` map the public HTTP endpoints.
* **Frontend Pages:** `AdminQuestionBanksPage.tsx` handles uploads; `QuizInstructionsPage.tsx` displays quiz constraints; `AttemptPage.tsx` manages interactive timer and answer selections; `ReviewPage.tsx` shows corrected answers alongside chosen answers.

### Technical Terms to Remember
* **Fisher-Yates Shuffle:** An algorithm that shuffles a list randomly by iterating backwards and swapping elements.
* **Deterministic Randomness:** Using a saved seed value to generate the same random sequence repeatedly.
* **Question Bank:** A reusable collection of categorized questions, separated from the quizzes themselves.
* **Identity Lockout:** The system restricts attempts if the student exceeds `MaximumAttempts` or has already passed the quiz.

### Explain It to the Interviewer
> "I implemented a quiz evaluation engine to verify student progress before unlocking next modules. The system splits questions into a reusable Question Bank, which admins upload via JSON. When creating a quiz, the admin specifies the passing criteria, difficulty filters, and attempt limits.
> 
> Behind the scenes, the main challenge was randomizing the exam for students while keeping it reviewable. To solve this, when a student starts a quiz, the backend generates a random seed and stores it in the database. Using this seed, we perform a Fisher-Yates shuffle on the question order and option order. Because the seed is saved, the randomized layout remains consistent if the page is refreshed or reviewed. On submission, the backend grades the attempt. If the student passes, it automatically marks the module complete, updating the DAG path."

### Mention Initially
* Shuffling of both questions and options.
* Saving the random seed to keep the attempt repeatable for reviews.
* Uploading question banks via JSON with strict schema validation.

### Don't Explain Initially
* The 5MB JSON file size limit.
* The exact arithmetic of `seed + QuestionId` for options shuffling.
* Details on the HTTP failed queue logic in Axios.

### If Asked Deeper
* **Why did you use a seed rather than saving the shuffled order directly?** Saving the shuffled order of 50 questions and their options would require creating thousands of join rows in the database (e.g., `ShuffledQuestionOrder` table). By saving a single integer seed, we can reconstruct the exact shuffled sequence in memory instantly, keeping the database slim.
* **How does JSON validation work?** [JsonValidationService.cs](file:///D:/FInal%20Project/LearnPath/backend/Services/Validation/JsonValidationService.cs) checks that the file is valid JSON, contains a title and subject, and has questions with at least two options. It also checks that the correct answer matches one of the options (case-insensitive) and that no duplicate question text exists.

### Likely Interview Questions
* **How do you handle page refreshes or network drops during a quiz?** (Very Likely)
  * *Answer Direction:* The quiz attempt status is saved as `InProgress` in the database. If a student refreshes, the frontend calls the API which returns the active attempt, preserving the timer and utilizing the stored seed to render the questions in the exact same order.
* **What happens to the path if a student fails a quiz?** (Likely)
  * *Answer Direction:* The attempt is marked as failed (`Passed=false`), the module remains incomplete, and subsequent modules stay locked. The student must start a new attempt (up to `MaximumAttempts`).

---

## 5. Feature 3: AI Assignment Feedback

### What It Does
This feature provides automated grading assistance. It allows admins to request AI reviews on student assignment files (PDFs and TXT files) using Nvidia's large language models.

### Why It Exists
Grading assignments takes a lot of time for admins. Providing detailed feedback on grammar, missing concepts, and grading rubrics is helpful but slow. This tool gives admins a starting draft of feedback and a suggested score.

### Understand It
When a student uploads a report, the admin can click "Generate AI Feedback". 
* The backend reads the file (even parsing text from PDFs).
* It packages the student's text, the assignment details, and grading criteria into a prompt.
* It sends this to Nvidia's AI completions API.
* The AI replies with structured feedback (grammar issues, missing topics, and a suggested score).
* The admin reviews this advice, adjusts it if needed, writes their final score, and publishes it. Only *after* the admin publishes does the student get to see the feedback.

### How It Works
1. **File Parsing:** The backend reads the uploaded file. If it's a PDF, we use a library called `PdfPig` to extract the text. If it's a TXT file, it reads it directly.
2. **Prompt Assembly:** The system extracts the assignment prompt, rubric, and student text, passing them through a structured system template.
3. **AI Completion:** The backend calls Nvidia's completions API using the configured model (like `nvidia-nemotron-nano-9b-v2`).
4. **Parsing Response:** The raw text response from the AI is parsed into structured fields (grammar points, missing details, rating).
5. **Database Storage:** The feedback is saved in a dedicated `SubmissionAiFeedback` table.
6. **Publication Gate:** The student cannot see the feedback immediately. The admin must finalize their evaluation and click "Publish", which clears the visibility flag.

### How It Is Implemented
* **AI Provider Structure:** [IAiProvider.cs](file:///D:/FInal%20Project/LearnPath/backend/Interfaces/IAiProvider.cs) defines the client interface. [NvidiaProvider.cs](file:///D:/FInal%20Project/LearnPath/backend/Services/Ai/NvidiaProvider.cs) implements this interface, using `HttpClient` to send requests to Nvidia's endpoints.
* **Coordinator Service:** [AiFeedbackService.cs](file:///D:/FInal%20Project/LearnPath/backend/Services/Ai/AiFeedbackService.cs) manages the process: verifying permissions, reading files from disk, calling the prompt generator, and saving the results.
* **Helper Classes:**
  * [PromptBuilder.cs](file:///D:/FInal%20Project/LearnPath/backend/Services/Ai/PromptBuilder.cs) constructs the instructions for the model.
  * [AiResponseParser.cs](file:///D:/FInal%20Project/LearnPath/backend/Services/Ai/AiResponseParser.cs) parses the AI's markdown response into a structured DTO.
* **PDF Extraction:** Uses the `UglyToad.PdfPig` library in [AiFeedbackService.cs](file:///D:/FInal%20Project/LearnPath/backend/Services/Ai/AiFeedbackService.cs).
* **Partially Implemented Note:** `GeminiProvider.cs` exists in the codebase but is **dead code**. Only the Nvidia provider is registered in `Program.cs`.

### Technical Terms to Remember
* **Nvidia Nemotron:** The specific LLM model used to analyze submissions.
* **System Prompt:** Rules sent to the AI to force it to return replies in a specific structure.
* **API Key Configuration:** The key is retrieved from the `LEARNPATH_NVIDIA_API_KEY` environment variable.
* **PdfPig:** A NuGet package used to parse text layout from PDF pages.

### Explain It to the Interviewer
> "I built an AI Assignment Feedback system to assist admins with grading. When a student submits a PDF or text file, the admin can trigger an AI evaluation. 
> 
> Behind the scenes, the backend extracts text from the document (using `PdfPig` for PDFs) and combines it with the assignment description and rubric. This data is formatted into a structured prompt and sent to Nvidia's Nemotron model API. The AI evaluates the response and returns feedback on grammar, missing topics, and a suggested score. We parse this into a structured database record. To keep the grading process human-controlled, this feedback is hidden from the student until the admin reviews, edits, and officially publishes the grade."

### Mention Initially
* Admins trigger it manually; it is not automatic on student upload.
* Parses both text files and PDFs (`PdfPig`).
* Uses Nvidia Nemotron model.
* Feedback is kept private until the admin publishes the grade.

### Don't Explain Initially
* Mentioning the dead `GeminiProvider.cs` file.
* Specific regular expressions used in the response parser.
* The file system path where PDFs are saved.

### If Asked Deeper
* **Why isn't AI feedback triggered automatically when a student submits?** (Deeper Concept)
  * *Answer Direction:* We wanted to keep admins in control of the classroom. Automated grading can make students try to exploit the AI, and admins should have the final say on grades. Manual triggering also helps manage API costs.
* **What happens if the AI fails or returns unstructured text?**
  * *Answer Direction:* The parser verifies that the required summary blocks exist. If they are missing, it throws a validation exception. The AI service will log the error and display a "please try again" message on the frontend.

### Likely Interview Questions
* **How does your backend extract text from PDF files?** (Very Likely)
  * *Answer Direction:* We use the `UglyToad.PdfPig` library. The service loops through each page of the document, extracts the raw text blocks, and appends them into a single string for analysis.
* **Why did you use Nvidia instead of Gemini?** (Likely)
  * *Answer Direction:* Although we created an interface to support multiple providers, the Nvidia model was registered as the active service because its formatting and evaluation results fit our structured grading requirements better during testing.

---

## 6. Feature 4: Immutable Audit Logging

### What It Does
The audit logging system acts as an unchangeable diary for the application. It automatically records every major administrative change (e.g. creating paths, publishing modules, updating grades), logging who did what and when.

### Why It Exists
For security and troubleshooting, it's vital to have a reliable record of administrative actions. If a quiz is deleted or a grade is changed, we must be able to prove who performed that action.

### Understand It
Imagine a log book written in permanent ink. If someone makes a change, they can only write a new line at the bottom; they cannot erase or modify old pages. 

To implement this:
* We create an `AuditLog` database table.
* Every time a major service method runs successfully, it writes a log entry describing the action.
* The system automatically determines who is logged in by reading the details inside their JWT authentication token.
* In our database settings, we override EF Core's default saving method. If the system detects any attempt to edit or delete rows in the `AuditLog` table, it blocks the action and throws an error.

### How It Works
1. **Triggering the Log:** A service completes a task, like publishing a module.
2. **Actor Resolution:** The service passes details to the `AuditLogService`. The service reads the user ID, email, and role from the active web request's credentials.
3. **Log Creation:** A new `AuditLog` record is added to the database.
4. **Immutability Guard:** If a user (even a SuperAdmin) tries to run an update or delete query on the `AuditLog` table, the EF Core interceptor blocks the transaction.
5. **Viewing History:** Students can retrieve their own log history (e.g. login times, quiz attempts). SuperAdmins can view the entire system log.
6. **Entity Existence Check:** When showing logs to the admin, the system checks if the affected item (like a deleted classroom) still exists in the database.

### How It Is Implemented
* **Database Entity:** [AuditLog.cs](file:///D:/FInal%20Project/LearnPath/backend/Entities/AuditLog.cs) stores administrative details (Timestamp, UserId, Role, ActionType, EntityType, EntityId, Description, OldValue, NewValue).
* **Immutability Code:** [ApplicationDbContext.cs](file:///D:/FInal%20Project/LearnPath/backend/Data/ApplicationDbContext.cs) overrides `SaveChanges` and `SaveChangesAsync`. It checks `ChangeTracker.Entries<AuditLog>()` and throws an `InvalidOperationException` if any logs have a state of `Modified` or `Deleted`.
* **Logging Service:** [AuditLogService.cs](file:///D:/FInal%20Project/LearnPath/backend/Services/Audit/AuditLogService.cs) resolves the active user from `IHttpContextAccessor.HttpContext.User` and writes to the database.
* **History Routing:** `HistoryController.cs` exposes endpoints `/api/v1/history/me` and `/api/v1/history/admin` (which enforces SuperAdmin checks).

### Technical Terms to Remember
* **Immutability:** Unchangeable. The log table is append-only.
* **EF Core ChangeTracker:** An EF Core tool that monitors whether entities have been added, modified, or deleted before saving.
* **IHttpContextAccessor:** An ASP.NET Core tool used to access the details of the current HTTP web request.
* **Entity Existence Check:** Running quick database checks to see if an entity (e.g., Module 5) still exists or has been deleted.

### Explain It to the Interviewer
> "I implemented a system-wide Audit Logging system to track all administrative actions. The core requirement was that it had to be completely tamper-proof. 
> 
> To achieve this, I wrote an interceptor in my database context using EF Core's Change Tracker. Before writing changes to the database, the context checks if any `AuditLog` records are marked for modification or deletion. If they are, it aborts the transaction and throws an exception, making the log table append-only. 
> 
> The logging is integrated across our services. The service grabs the user's ID and role from the HttpContext user claims, creating a clear history of who edited what. Admins can view this history through a dashboard, where the system checks if referenced entities still exist in the database."

### Mention Initially
* Append-only implementation using EF Core's ChangeTracker.
* Automatically identifies the user using JWT claims from the web request.
* Distinct history views for standard users vs SuperAdmins.

### Don't Explain Initially
* The details of the `ResolveActor` fallback code block.
* Listing all the enum values in `AuditAction`.
* How the frontend displays the history list.

### If Asked Deeper
* **How does EF Core block updates?** (Deeper explanation)
  * In [ApplicationDbContext.cs](file:///D:/FInal%20Project/LearnPath/backend/Data/ApplicationDbContext.cs), we intercept save requests:
    ```csharp
    var hasAuditViolations = ChangeTracker.Entries<AuditLog>()
        .Any(e => e.State == EntityState.Modified || e.State == EntityState.Deleted);
    if (hasAuditViolations) {
        throw new InvalidOperationException("Audit logs are append-only.");
    }
    ```
* **How does the system resolve deleted entities?** (Entity Existence check)
  * In [AuditLogService.cs](file:///D:/FInal%20Project/LearnPath/backend/Services/Audit/AuditLogService.cs), `ResolveEntityExistenceAsync` performs batch queries across tables (LearningPaths, Classrooms, Quizzes) to check if the `EntityId` still exists. If not, the UI shows a "Deleted" status tag.

### Likely Interview Questions
* **Why did you implement immutability in the backend code instead of using database triggers?** (Very Likely)
  * *Answer Direction:* Implementing it in the application's DBContext keeps the security rules inside the source code, making it database-agnostic and easier to unit test.
* **Does writing audit logs slow down standard operations?** (Likely)
  * *Answer Direction:* The log writing runs asynchronously (`LogAsync`) and represents a single SQL insert. We write logs after the main database changes succeed, ensuring it doesn't block core business transactions.

---

## 7. Architecture & Stack

LearnPath uses a standard client-server architecture:

```
+----------------------------------------------------+
|               React Frontend (Vite)                |
|  (MUI 5, Redux Toolkit, Router 6, Axios Client)    |
+----------------------------------------------------+
                          │ (REST API via HTTPS)
                          ▼
+----------------------------------------------------+
|               Nginx Reverse Proxy                  |
|    (Listens on Port 80, forwards to Port 5000)     |
+----------------------------------------------------+
                          │
                          ▼
+----------------------------------------------------+
|               ASP.NET Core Web API                 |
|   (Dependency Injection, Controllers, Services)    |
+----------------------------------------------------+
                          │
                          ▼
+----------------------------------------------------+
|                  Database Layer                    |
|        (EF Core ORM, SQL Server Database)          |
+----------------------------------------------------+
```

### Key Architectural Rules
* **Stateless Communication:** The frontend and backend talk only via JSON over HTTPS.
* **Separation of Concerns:** Controllers handle incoming requests and validation, services run business logic, and EF Core handles database queries.
* **Cross-Cutting Concerns:** Error handling, authorization, and rate limiting are handled by dedicated middleware.

---

## 8. Frontend & Redux Integration

### State Management Flow
We use **Redux Toolkit** to manage application state (like user login details, learning path data, and UI feedback). 

```text
User triggers UI action (e.g. click "Join Classroom")
                  ↓
Component dispatches a Redux Thunk (async API call)
                  ↓
Axios client sends tokenized HTTP request to backend
                  ↓
Backend responds with JSON (ApiResponse wrapper)
                  ↓
Thunk receives reply -> Dispatches action to Redux slice
                  ↓
Redux store updates -> React component re-renders
```

### Axios Interceptors & JWT Handling
In frontend code, we intercept every outgoing request to attach the JWT token. If a request returns a `401 Unauthorized` status (due to token expiry), the interceptor pauses outgoing calls, triggers a token refresh request, updates the local storage, and retries the original request.

---

## 9. Backend & Database Design

* **Framework:** .NET 10 Web API.
* **ORM:** Entity Framework Core (EF Core) using a code-first approach.
* **Database:** SQL Server 2022.
* **Validation:** FluentValidation handles incoming request payloads before they reach controllers.

---

## 10. Authentication & Role Management

LearnPath uses stateless JWT-based authentication.

### How it Works
1. **Login:** Student registers or logs in via `/api/v1/auth/login`.
2. **Token Generation:** The backend validates password hashes using ASP.NET Identity. If valid, it returns an **Access Token** (expires in 60 minutes) and a **Refresh Token** (expires in 7 days).
3. **Usage:** The frontend stores the access token in memory and uses it in HTTP request headers: `Authorization: Bearer <token>`.
4. **Roles & Policies:** Identity details contain claims defining user roles (Student, Admin). Controllers restrict endpoints using policies like `[Authorize(Roles = "Admin")]`.

---

## 11. Classroom & Assignments Workflow

Classrooms facilitate structure between admins and students:

```text
Admin creates classroom -> System generates an 8-character invite code
                  ↓
Student enters invite code -> Joins classroom
                  ↓
Admin posts assignment with due date
                  ↓
Student uploads assignment file (verified size & type)
                  ↓
Admin transitions assignment status (Submitted -> UnderReview -> Graded)
                  ↓
Admin generates optional AI feedback -> Reviews & Publishes evaluation
                  ↓
Student accesses grade and AI feedback
```

---

## 12. Important Technical Decisions & Tradeoffs

As a developer, you should be ready to explain the limitations and compromises made in your codebase:

* **Single AI Provider:** Although `IAiProvider` was designed to support both Gemini and Nvidia, only the Nvidia Nemotron provider was configured. The Gemini implementation remains unused.
* **In-Memory Rate Limiting:** We restricted API abuse to 50 requests per minute using an in-memory `ConcurrentDictionary`. This works well for single-server setups but would need to be replaced by a database-backed solution (like Redis) to support multi-server scaling.
* **Polling Notifications:** Notifications are retrieved using standard HTTP polling (`GET /api/v1/notifications`). Using WebSockets or SignalR would have allowed real-time updates but was skipped to keep the initial implementation simpler.
* **Placeholder Certificates:** The certificate generation service saves a record in the database and returns a URL placeholder (`/certificates/{userId}/{pathId}`). The system does not generate an actual PDF document.

---

## 13. Challenges (Problem → Approach → Why → Result)

### Challenge 1: Inconsistent API Responses and Validation Messages
* **Problem:** Different services threw different exceptions, and validation failures returned unstructured raw details. The frontend had to parse multiple formats, causing UI bugs.
* **Approach:** I unified all API responses using a generic wrapper class (`ApiResponse<T>`) and built a global exception middleware.
* **Why:** Centralizing error handling ensures that no matter what error occurs, the frontend receives a clean, standardized JSON response containing a readable message.
* **Result:** Reduced frontend code complexity and provided clear error popups for users.

### Challenge 2: Cascade Delete Conflicts in the Database
* **Problem:** Deleting a learning path resulted in database foreign key errors (`FK violation 547`) because related classrooms, modules, and progress entries were left behind.
* **Approach:** I updated the deletion method in [LearningPathService.cs](file:///D:/FInal%20Project/LearnPath/backend/Services/LearningPath/LearningPathService.cs) to use an explicit database transaction and delete related records in a strict dependency-safe order.
* **Why:** Explicitly clearing dependent records in code before deleting the parent path prevents database constraint violations and guarantees clean deletions.
* **Result:** Clean deletions of learning paths without database conflicts.

### Challenge 3: Security Bypass in Module Access
* **Problem:** Although the frontend UI locked cards, users could bypass the interface and fetch module content by making direct API calls.
* **Approach:** I added a validation check to the backend service layer (`GetModuleContentAsync`). Before returning content, the API validates the student's progress.
* **Why:** Security checks must be implemented in the backend, as frontend restrictions can easily be bypassed by intercepting network traffic.
* **Result:** Robust path lock enforcement, returning `403 Forbidden` on unauthorized API requests.

### Challenge 4: Inconsistent Question Bank Upload Formats
* **Problem:** Administrators frequently uploaded incorrectly formatted JSON question files. The backend rejected them, but the frontend only showed a generic error, frustrating users.
* **Approach:** I updated the JSON validation service to return detailed error logs. On the frontend, I added a modal detailing the formatting rules and showcasing a JSON template.
* **Result:** Admins can self-correct their upload formats without referencing code files.

### Challenge 5: Basic Forum Feel
* **Problem:** The forum feature felt like a simple comment list, lacking engagement.
* **Approach:** I added support for code snippets, programming languages, and category tags. I also implemented a scoring algorithm that ranks posts based on votes and creation age.
* **Result:** An active, searchable community feed.

---

## 14. Personal Contribution

> **Personal Contribution: Needs my input**

*(Draft template you can customize:)*
> "I was responsible for developing several core features in the LearnPath project. I implemented the DAG-based learning path unlock logic and built the backend evaluation rules in `LearningPathService.cs` and `DagValidator.cs`. I also developed the randomized quiz engine with Fisher-Yates shuffling, the AI feedback service integration utilizing Nvidia's Nemotron model, and the append-only audit logging system using EF Core's ChangeTracker interceptors."

---

## 15. What I Learned

* **Designing Graphs in Code:** Designing graphs is easy on paper but requires careful implementation to prevent circular loops and manage fallback ordering rules.
* **Standardizing Interfaces:** Creating consistent API wrappers early in development saves significant frontend refactoring time.
* **Database Constraints:** Managing foreign key deletions requires explicit transactional operations in the service layer.
* **Defensive Coding:** Implementing validation checks on both the client and server is critical for security and user experience.

---

## 16. Project-Level Likely Questions

* **How does the application handle cross-origin requests?**
  * *Answer Direction:* We configure CORS policies in [Program.cs](file:///D:/FInal%20Project/LearnPath/backend/Program.cs), permitting requests from the frontend origin and allowing authorization headers.
* **What password hashing algorithms are used?**
  * *Answer Direction:* We use ASP.NET Core Identity's default configuration, which hashes passwords using PBKDF2 with SHA-256 and unique salts.
* **How are file uploads secured?**
  * *Answer Direction:* `FileValidationService.cs` restricts uploads by file extension (e.g. PDF/TXT only) and limits file size (max 6MB).

---

## 17. Project-Level Follow-ups

* **How would you scale this system to support thousands of active users?**
  * *Answer Direction:* I would migrate rate limiting to a Redis cluster, run stateless API containers behind a load balancer, and use database read-replicas.
* **How would you handle notifications without polling?**
  * *Answer Direction:* I would implement real-time hubs using ASP.NET Core SignalR, allowing the backend to push alerts directly to active clients over WebSockets.

---

## 18. Important Things I Must Know

1. **Active AI Model:** Nvidia Nemotron is the configured provider; Gemini is dead code.
2. **File Size Limits:** Assignments are restricted to 6MB; Question Banks are limited to 5MB.
3. **Database Guard:** Audit logs are completely read-only and block modifications in EF Core.
4. **Token Expirations:** Access tokens expire in 60 minutes; refresh tokens last 7 days.

---

## 19. Final Quick Revision

Before your interview, review this quick summary checklist:

* [x] **Path Unlock Rule:** Lock status is calculated dynamically using `dagUnlocked && sequentialUnlocked`.
* [x] **Quiz Randomization:** Uses a stored seed value and the Fisher-Yates algorithm to shuffle questions and options deterministically.
* [x] **AI Feedback Gate:** Admins must publish grades before AI analysis is displayed to students.
* [x] **Tamper Protection:** Override EF Core `SaveChanges` to block modifications to the `AuditLog` table.
* [x] **Classroom Joins:** Handled using unique 8-character invite codes.
