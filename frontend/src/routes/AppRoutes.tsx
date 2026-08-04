import { lazy, Suspense } from "react";
import { Routes, Route } from "react-router-dom";
import { ROUTES } from "../constants/routes";
import ProtectedRoute from "./ProtectedRoute";
import AdminRoute     from "./AdminRoute";
import InstructorRoute from "./InstructorRoute";
import GuestRoute     from "./GuestRoute";
import Loader         from "../components/common/Loader/Loader";
import MainLayout     from "../layouts/MainLayout";
import DashboardLayout from "../layouts/DashboardLayout";
import AuthLayout     from "../layouts/AuthLayout";
import AdminLayout    from "../layouts/AdminLayout";

const LandingPage            = lazy(() => import("../pages/public/LandingPage"));
const LoginPage              = lazy(() => import("../pages/auth/LoginPage"));
const RegisterPage           = lazy(() => import("../pages/auth/RegisterPage"));
const DashboardPage          = lazy(() => import("../pages/dashboard/DashboardPage"));
const LearningPathsPage      = lazy(() => import("../pages/learningPaths/LearningPathsPage"));
const LearningPathDetailPage = lazy(() => import("../pages/learningPaths/LearningPathDetailPage"));
const LessonPage             = lazy(() => import("../pages/learningPaths/LessonPage"));
const ClassroomPage          = lazy(() => import("../pages/classroom/ClassroomPage"));
const ClassroomDetailPage    = lazy(() => import("../pages/classroom/ClassroomDetailPage"));
const AssignmentDetailPage   = lazy(() => import("../pages/classroom/AssignmentDetailPage"));
const AnalyticsPage          = lazy(() => import("../pages/analytics/AnalyticsPage"));
const CommunityPage          = lazy(() => import("../pages/community/CommunityPage"));
const CommunityPostPage      = lazy(() => import("../pages/community/CommunityPostPage"));
const CommunityGroupsPage    = lazy(() => import("../pages/community/CommunityGroupsPage"));
const CommunityGroupDetailPage = lazy(() => import("../pages/community/CommunityGroupDetailPage"));
const CommunityMyPostsPage   = lazy(() => import("../pages/community/CommunityMyPostsPage"));
const CommunityModerationPage = lazy(() => import("../pages/community/CommunityModerationPage"));
const CertificatesPage       = lazy(() => import("../pages/certificates/CertificatesPage"));
const HistoryPage            = lazy(() => import("../pages/history/HistoryPage"));
const AdminHistoryPage       = lazy(() => import("../pages/admin/AdminHistoryPage"));
const ProfilePage            = lazy(() => import("../pages/profile/ProfilePage"));
const SettingsPage           = lazy(() => import("../pages/settings/SettingsPage"));
const AdminPage              = lazy(() => import("../pages/admin/AdminPage"));
const AdminPathsPage         = lazy(() => import("../pages/admin/AdminPathsPage"));
const AdminModuleEditorPage  = lazy(() => import("../pages/admin/AdminModuleEditorPage"));
const AdminQuizEditorPage    = lazy(() => import("../pages/admin/AdminQuizEditorPage"));
const AdminUsersPage         = lazy(() => import("../pages/admin/AdminUsersPage"));
const AdminCertificatesPage  = lazy(() => import("../pages/admin/AdminCertificatesPage"));
const AdminClassroomsPage    = lazy(() => import("../pages/admin/AdminClassroomsPage"));
const AdminQuestionBanksPage = lazy(() => import("../pages/admin/AdminQuestionBanksPage"));
const AdminQuizManagementPage = lazy(() => import("../pages/admin/AdminQuizManagementPage"));
const AdminQuizAnalyticsPage = lazy(() => import("../pages/admin/AdminQuizAnalyticsPage"));
const QuizInstructionsPage = lazy(() => import("../pages/quiz/QuizInstructionsPage"));
const QuizAttemptPage      = lazy(() => import("../pages/quiz/QuizAttemptPage"));
const QuizResultPage       = lazy(() => import("../pages/quiz/QuizResultPage"));
const QuizReviewPage       = lazy(() => import("../pages/quiz/QuizReviewPage"));
const NotFoundPage           = lazy(() => import("../pages/errors/NotFoundPage"));

const AppRoutes = () => (
  <Suspense fallback={<Loader />}>
    <Routes>
      <Route element={<MainLayout />}>
        <Route index element={<LandingPage />} />
        <Route element={<AuthLayout />}>
          <Route element={<GuestRoute />}>
            <Route path={ROUTES.LOGIN} element={<LoginPage />} />
            <Route path={ROUTES.REGISTER} element={<RegisterPage />} />
          </Route>
        </Route>
        <Route element={<ProtectedRoute />}>
          <Route path={ROUTES.QUIZ_INSTRUCTIONS} element={<QuizInstructionsPage />} />
          <Route path={ROUTES.QUIZ_ATTEMPT} element={<QuizAttemptPage />} />
          <Route path={ROUTES.QUIZ_RESULT} element={<QuizResultPage />} />
          <Route path={ROUTES.QUIZ_REVIEW} element={<QuizReviewPage />} />
          <Route element={<DashboardLayout />}>
            <Route path={ROUTES.DASHBOARD} element={<DashboardPage />} />
            <Route path={ROUTES.LEARNING_PATHS} element={<LearningPathsPage />} />
            <Route path={ROUTES.LEARNING_PATH_DETAIL} element={<LearningPathDetailPage />} />
            <Route path={ROUTES.LESSON} element={<LessonPage />} />
            <Route path={ROUTES.CLASSROOM} element={<ClassroomPage />} />
            <Route path={ROUTES.CLASSROOM_DETAIL} element={<ClassroomDetailPage />} />
            <Route path={ROUTES.ASSIGNMENT_DETAIL} element={<AssignmentDetailPage />} />
            <Route path={ROUTES.ANALYTICS} element={<AnalyticsPage />} />
            <Route path={ROUTES.COMMUNITY} element={<CommunityGroupsPage />} />
            <Route path={ROUTES.COMMUNITY_DETAIL} element={<CommunityPostPage />} />
            <Route path={ROUTES.COMMUNITY_GROUPS} element={<CommunityGroupsPage />} />
            <Route path={ROUTES.COMMUNITY_GROUP_DETAIL} element={<CommunityGroupDetailPage />} />
            <Route path={ROUTES.COMMUNITY_POSTS} element={<CommunityPage />} />
            <Route path={ROUTES.COMMUNITY_MY_POSTS} element={<CommunityMyPostsPage />} />
            <Route element={<AdminRoute />}>
              <Route path={ROUTES.COMMUNITY_MODERATION} element={<CommunityModerationPage />} />
            </Route>
            <Route path={ROUTES.CERTIFICATES} element={<CertificatesPage />} />
            <Route path={ROUTES.HISTORY} element={<HistoryPage />} />
            <Route path={ROUTES.PROFILE} element={<ProfilePage />} />
            <Route path={ROUTES.SETTINGS} element={<SettingsPage />} />
            <Route element={<AdminRoute />}>
              <Route element={<AdminLayout />}>
                <Route path={ROUTES.ADMIN} element={<AdminPage />} />
                <Route path={ROUTES.ADMIN_PATHS} element={<AdminPathsPage />} />
                <Route path={ROUTES.ADMIN_PATH_MODULES} element={<AdminModuleEditorPage />} />
                <Route path={ROUTES.ADMIN_QUIZ_EDITOR} element={<AdminQuizEditorPage />} />
                <Route path={ROUTES.ADMIN_USERS} element={<AdminUsersPage />} />
                <Route path={ROUTES.ADMIN_CERTIFICATES} element={<AdminCertificatesPage />} />
                <Route path={ROUTES.ADMIN_CLASSROOMS} element={<AdminClassroomsPage />} />
                <Route path={ROUTES.ADMIN_QUESTION_BANKS} element={<AdminQuestionBanksPage />} />
                <Route path={ROUTES.ADMIN_QUIZZES} element={<AdminQuizManagementPage />} />
                <Route path={ROUTES.ADMIN_QUIZ_ANALYTICS} element={<AdminQuizAnalyticsPage />} />
                <Route path={ROUTES.ADMIN_HISTORY} element={<AdminHistoryPage />} />
              </Route>
            </Route>
            <Route element={<InstructorRoute />}>
              <Route element={<AdminLayout />}>
                <Route path={ROUTES.ADMIN_QUIZZES} element={<AdminQuizManagementPage />} />
              </Route>
            </Route>
          </Route>
        </Route>
        <Route path={ROUTES.NOT_FOUND} element={<NotFoundPage />} />
      </Route>
    </Routes>
  </Suspense>
);

export default AppRoutes;
