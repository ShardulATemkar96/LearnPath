# 🌟 LearnPath — Graph-Based Personalized Learning Platform

> A full-stack learning management system where instructors build structured learning paths as directed acyclic graphs (DAGs), and students progress through modules, collaborate in classrooms, earn certificates, and track everything with rich analytics.

<div align="center">

<!-- Placeholder for logo/screenshot/GIF -->
<img src="https://via.placeholder.com/800x400/6C63FF/FFFFFF?text=LearnPath+Graph+Dashboard" alt="LearnPath Screenshot" width="800"/>

<!-- Status badges -->
![Build](https://img.shields.io/badge/build-passing-brightgreen)
![.NET](https://img.shields.io/badge/.NET-10.0-512BD4)
![React](https://img.shields.io/badge/React-18.2-61DAFB)
![TypeScript](https://img.shields.io/badge/TypeScript-5.3-3178C6)
![License](https://img.shields.io/badge/license-MIT-blue)

</div>

---

## 📝 About The Project

**The problem:** Traditional learning platforms use linear course progression. Students often waste time on material they already know, or skip prerequisites they don't have, leading to confusion and drop-off. Instructors lack fine-grained control over module ordering and dependency enforcement.

**The solution:** LearnPath organizes learning content as a **directed acyclic graph (DAG)**. Each module has explicit prerequisites. The platform automatically enforces dependency ordering, visualizes the graph so students see their progression path, and unlocks modules only when all prerequisites are completed.

**Target audience:**
- **Students** who want self-paced, structured, and visual learning
- **Instructors** who need flexible module ordering with automatic dependency enforcement
- **Administrators** who manage users, classrooms, and platform-wide analytics

**Why it matters:** LearnPath combines the structure of academic course management with the flexibility of modern self-paced platforms — all in one open-source package with a clean, modern UI.
---

## 🔧 Repository Setup

Follow these steps to get the project running securely on your local machine.

### 1. Clone

```bash
git clone https://github.com/yourusername/LearnPath.git
cd LearnPath
```

### 2. Configure Environment Variables

Sensitive values (API keys, JWT secret, database connection) are loaded from a local `.env` file.

```bash
# From the backend/ directory
cd backend
cp .env.example .env
```

Then open `backend/.env` and fill in your real values:

```text
LEARNPATH_NVIDIA_API_KEY=nvapi-your-key
LEARNPATH_NVIDIA_MODEL=nvidia/nvidia-nemotron-nano-9b-v2
LEARNPATH_GROQ_API_KEY=gsk-your-key
LEARNPATH_GROQ_MODEL=groq-model-name
LEARNPATH_JWT_SECRET=a-32-plus-character-secret
LEARNPATH_DB_CONNECTION=Server=(localdb)\mssqllocaldb;Database=LearnPathDb;Trusted_Connection=True;TrustServerCertificate=True
```

> `.env` is ignored by Git and must **never** be committed. Only `.env.example` (which contains empty placeholders) is tracked.

Available variables are documented in [`backend/.env.example`](./backend/.env.example).

### 3. Run Database Migrations

```bash
cd backend
dotnet ef database update
```

### 4. Start Backend

```bash
cd backend
dotnet run
```

API: `http://localhost:5000` | Swagger: `http://localhost:5000/swagger`

### 5. Start Frontend

```bash
cd frontend
npm install
npm run dev
```

Frontend: `http://localhost:5173`

---

## 🚀 Quick Start & Installation

### Prerequisites

- [Node.js 20+](https://nodejs.org/) (frontend)
- [.NET 10.0 SDK](https://dotnet.microsoft.com/download) (backend)
- [SQL Server 2022](https://www.microsoft.com/en-us/sql-server/sql-server-downloads) — or use Docker as shown below
- [Docker Desktop](https://www.docker.com/products/docker-desktop/) (optional, for containerized setup)

### Clone & Setup

```bash
git clone https://github.com/yourusername/LearnPath.git
cd LearnPath
```

### Backend Setup

```bash
# Navigate to backend
cd backend

# Copy env template and fill in your secrets
cp .env.example .env

# Restore dependencies
dotnet restore

# Apply migrations & seed data
dotnet ef database update

# Run the API server
dotnet run
```

The API starts at `http://localhost:5000`. Swagger UI is available at `http://localhost:5000/swagger`.

### Frontend Setup

Open a second terminal:

```bash
# Navigate to frontend
cd frontend

# Install dependencies
npm install

# Start dev server
npm run dev
```

The frontend starts at `http://localhost:5173` and proxies `/api/` requests to the backend.

### Database (Docker)

If you don't have SQL Server installed locally:

```bash
docker run -e "ACCEPT_EULA=Y" -e "SA_PASSWORD=YourStr0ngP@ss" \
  -p 1433:1433 -d mcr.microsoft.com/mssql/server:2022-latest
```

Then set `LEARNPATH_DB_CONNECTION` in `backend/.env`:

```
LEARNPATH_DB_CONNECTION=Server=localhost,1433;Database=LearnPath;User Id=sa;Password=YourStr0ngP@ss;TrustServerCertificate=true
```

### Docker Compose (Full Stack)

From the project root:

```bash
docker compose up --build
```

This starts SQL Server, the .NET API (port 5000), and the React frontend served by Nginx (port 80).

### Environment Variables

For **Docker deployment**, copy the root `.env.example` and configure:

```bash
cp .env.example .env
```

Required Docker variables:

| Variable | Purpose |
|----------|---------|
| `DB_SA_PASSWORD` | SQL Server SA password for the Docker container |
| `JWT_SECRET` | JWT signing secret (32+ characters) |
| `FRONTEND_URL` | Frontend origin for CORS |
| `VITE_API_BASE_URL` | API URL used by the frontend build |

For **backend development**, copy `backend/.env.example` → `backend/.env` instead (see [Repository Setup](#-repository-setup)).

### Default Admin Credentials

After seeding, log in with:

| Email | Password | Role |
|-------|----------|------|
| `admin@learnpath.com` | `Admin@123` | Admin |

---

## 🛠️ Built With

### Frontend
[![React](https://img.shields.io/badge/React-18.2-61DAFB?logo=react)](https://react.dev)
[![TypeScript](https://img.shields.io/badge/TypeScript-5.3-3178C6?logo=typescript)](https://www.typescriptlang.org)
[![Vite](https://img.shields.io/badge/Vite-5-646CFF?logo=vite)](https://vite.dev)
[![Redux Toolkit](https://img.shields.io/badge/Redux_Toolkit-1.9-764ABC?logo=redux)](https://redux-toolkit.js.org)
[![MUI](https://img.shields.io/badge/MUI-5.15-007FFF?logo=mui)](https://mui.com)
[![React Router](https://img.shields.io/badge/React_Router-6.20-CA4245?logo=reactrouter)](https://reactrouter.com)
[![Recharts](https://img.shields.io/badge/Recharts-2.10-22B5BF)](https://recharts.org)
[![Axios](https://img.shields.io/badge/Axios-1.6-5A29E4?logo=axios)](https://axios-http.com)
[![Vitest](https://img.shields.io/badge/Vitest-1.0-6E9F18?logo=vitest)](https://vitest.dev)

### Backend
[![.NET](https://img.shields.io/badge/.NET-10.0-512BD4?logo=dotnet)](https://dotnet.microsoft.com)
[![ASP.NET Core](https://img.shields.io/badge/ASP.NET_Core-10.0-512BD4?logo=dotnet)](https://dotnet.microsoft.com/apps/aspnet)
[![Entity Framework Core](https://img.shields.io/badge/EF_Core-10.0-512BD4?logo=dotnet)](https://learn.microsoft.com/ef)
[![SQL Server](https://img.shields.io/badge/SQL_Server-2022-CC2927?logo=microsoftsqlserver)](https://www.microsoft.com/sql-server)
[![FluentValidation](https://img.shields.io/badge/FluentValidation-12-2563EB)](https://docs.fluentvalidation.net)
[![AutoMapper](https://img.shields.io/badge/AutoMapper-15-FF6A00)](https://automapper.org)
[![Swagger](https://img.shields.io/badge/Swagger-7-85EA2D?logo=swagger)](https://swagger.io)
[![xUnit](https://img.shields.io/badge/xUnit-tests-25A162)](https://xunit.net)

### Infrastructure
| | |
|---|---|
| **Containerization** | Docker, Docker Compose |
| **Reverse Proxy** | Nginx (SPA + API proxy) |
| **CI/CD** | GitHub Actions (build, test, push to ghcr.io, SSH deploy) |
| **Auth** | ASP.NET Core Identity + JWT Bearer (HS256) |

---

## 📖 How to Use

### User Roles

| Role | Capabilities |
|------|-------------|
| **Student** | Browse public paths, enroll, complete modules, join classrooms, participate in community, earn certificates |
| **Instructor** | Everything in Student + create/manage learning paths, create classrooms, create assignments, grade submissions |
| **Admin** | Everything + manage users, change roles, view platform stats, manage all paths |

### Typical User Flow

1. **Register** as a Student (or log in with the seeded admin account)
2. **Browse** the dashboard — view enrolled path progress, recent activity, and key stats
3. **Explore** the **Learning Paths** page — browse public paths or create your own
4. **Open a Path** — see the module list and an interactive **dependency graph** (DAG visualization)
5. **Complete modules** — the next modules unlock automatically when prerequisites are done
6. **Join a Classroom** — use an invite code to collaborate with peers on assignments
7. **Discuss** in the **Community** forum — create posts, comment, upvote
8. **Track progress** — the Analytics page shows weekly activity, path completion rates, and content type breakdown
9. **Earn Certificates** — automatically issued when all modules in a path are completed

---

## 📂 Project Structure

```
LearnPath/
├── backend/
│   ├── .env.example                  # Backend env template (tracked)
│   ├── .env                          # Local secrets (gitignored)
│   ├── ...                          # .NET 10 Web API
│   ├── Algorithms/Graph/
│   │   └── DagValidator.cs           # DAG cycle detection & topological sort
│   ├── Authentication/Jwt/
│   │   ├── JwtSettings.cs            # JWT configuration POCO
│   │   └── JwtTokenGenerator.cs      # Access + refresh token generation
│   ├── Common/
│   │   └── ApiResponse.cs            # Standard API response wrapper
│   ├── Controllers/                  # 11 API controllers (~55 endpoints)
│   │   ├── AdminController.cs
│   │   ├── AnalyticsController.cs
│   │   ├── AuthController.cs
│   │   ├── CertificateController.cs
│   │   ├── ClassroomController.cs
│   │   ├── CommunityController.cs
│   │   ├── DashboardController.cs
│   │   ├── HealthController.cs
│   │   ├── LearningPathController.cs
│   │   ├── NotificationController.cs
│   │   ├── ProgressController.cs
│   │   └── UserController.cs
│   ├── Data/
│   │   ├── ApplicationDbContext.cs   # EF Core DbContext (16 DbSets)
│   │   └── Seeders/RoleSeeder.cs
│   ├── DTOs/                         # 15+ request/response DTOs
│   ├── Entities/                     # 16 domain entities
│   ├── Middleware/                   # Exception, Logging, RateLimiting middleware
│   ├── Migrations/                   # EF Core migrations
│   ├── Services/                     # 9 service implementations
│   ├── Validators/                   # FluentValidation validators
│   └── Program.cs                    # App bootstrap & pipeline
│
├── frontend/                         # React + Vite + TypeScript SPA
│   ├── src/
│   │   ├── app/
│   │   │   ├── provider.tsx          # Redux + MUI + Router + ErrorBoundary
│   │   │   ├── store.ts              # Store re-export
│   │   │   └── AppInitializer.tsx    # JWT session restore on mount
│   │   ├── components/
│   │   │   ├── analytics/            # 4 chart components (Recharts)
│   │   │   ├── classroom/            # ClassroomCard, AssignmentCard
│   │   │   ├── common/               # Loader, EmptyState, ErrorBoundary, SearchBar, etc.
│   │   │   ├── community/            # PostCard, CommentItem
│   │   │   ├── dashboard/            # Sidebar, Topbar, StatCard, ActivityFeed, ProgressOverview
│   │   │   └── learningPath/         # PathCard, PathGraph (SVG DAG), CreatePathModal
│   │   ├── hooks/                    # useAuth, useDebounce, usePagination, useApiError
│   │   ├── layouts/                  # Main, Auth, Dashboard, Admin layouts
│   │   ├── pages/                    # 16 page components
│   │   ├── redux/                    # 7 slices + 7 selector files
│   │   ├── routes/                   # AppRoutes + 3 route guards
│   │   ├── services/                 # 10 API service modules (Axios)
│   │   ├── theme/                    # MUI theme (palette, typography)
│   │   ├── types/                    # TypeScript interfaces
│   │   └── utils/                    # dateUtils, tokenUtils, graphUtils, validationUtils
│   ├── vite.config.ts
│   └── nginx.conf                    # SPA + API proxy configuration
│
├── backend.Tests/                    # xUnit test project
├── .github/workflows/                # CI + CD pipelines
├── docker-compose.yml                # Full-stack Docker deployment
└── .env.example                      # Docker environment template
```

---

## 🔐 Authentication & Security

- **JWT-based authentication** with short-lived access tokens (60 min) and long-lived refresh tokens (7 days)
- **Refresh token rotation** — each refresh revokes the previous token; tokens stored in the database
- **Role-based authorization** — three tiers: `Student`, `Instructor`, `Admin`
- **Rate limiting** — IP-based, configurable (default 100 requests/minute), with `X-RateLimit` headers
- **Password policy** — minimum 8 characters, requires uppercase, lowercase, digit; account lockout after 5 failed attempts
- **Input validation** — FluentValidation on all write endpoints
- **Global exception handling** — structured JSON error responses with proper HTTP status codes
- **CORS** — locked to allowed origins (default: `http://localhost:5173` in development)
- **JWT token management** — frontend stores tokens in `localStorage`; Axios interceptors attach tokens and auto-refresh on 401

---

## 📊 Features

### 🔐 Authentication
- Register, login, token refresh, token revocation
- JWT session restore on page reload
- Route guards for authenticated, guest, and admin-only pages

### 📚 Learning Paths
- Create, update, delete learning paths (admin/instructor)
- Public / private visibility toggle
- Browse public paths and personal paths
- Detailed path view with module list and progress

### 🧩 Modules & Dependency Graph
- Add, update, delete modules within a path
- Define prerequisites via module dependencies
- **DAG validation** — cycle detection prevents circular dependencies
- **Interactive SVG graph** — visualizes the dependency tree with completion/unlock/locked states
- Automatic module unlocking when prerequisites are met

### 🏫 Classrooms
- Create classrooms linked to a learning path
- Join via 8-character invite code
- Instructor and student roles within classrooms
- Create and manage assignments with due dates
- Submit assignment URLs
- Grade submissions (instructor)

### 💬 Community Forum
- Create posts (title, content, category)
- Threaded comments with nested replies
- Upvote/downvote system for posts and comments
- Category filtering and search
- Paginated listing

### 📈 Analytics
- Weekly activity bar chart (modules completed per day)
- Path completion progress bars
- Content type breakdown (pie chart)
- Mini stat cards: modules completed, paths enrolled, certificates, streak, average completion rate, active classrooms

### 🔔 Notifications
- Real-time notification bell in the top bar
- Mark single or all notifications as read
- Unread count badge

### 🏆 Certificates
- Automatically issued when all modules in a path are completed
- Certificate listing page with path title and issue date

### 👑 Admin Panel
- Platform-wide statistics (users, paths, classrooms, certificates)
- User management (search, view, change roles, activate/deactivate, delete)
- All paths overview
- Module editor

### 🔍 Search
- Search across learning paths and community posts
- Debounced input for smooth UX

### 📐 Additional Utilities
- Pagination component for list views
- Loading skeletons during data fetch
- Empty state placeholders
- Confirmation dialogs for destructive actions
- Responsive design via MUI Grid

---

## 🤝 Contributing

Contributions are welcome! Here's how you can help:

- **Report bugs** — open an issue with steps to reproduce
- **Suggest features** — open an issue describing the feature
- **Submit pull requests** — fork the repo, create a branch, commit, and open a PR

Before contributing, please check open issues to avoid duplicating work.

---

## 📜 License

This project is open source under the **MIT License**. Feel free to use, modify, and distribute it as you see fit.

---

<div align="center">
Made with ❤️ as a CDAC project
</div>
