import { lazy, Suspense } from "react";
import { Routes, Route } from "react-router-dom";
import { ROUTES } from "../constants/routes";
import ProtectedRoute from "./ProtectedRoute";
import AdminRoute     from "./AdminRoute";
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
const AnalyticsPage          = lazy(() => import("../pages/analytics/AnalyticsPage"));
const CommunityPage          = lazy(() => import("../pages/community/CommunityPage"));
const CommunityPostPage      = lazy(() => import("../pages/community/CommunityPostPage"));
const CertificatesPage       = lazy(() => import("../pages/certificates/CertificatesPage"));
const ProfilePage            = lazy(() => import("../pages/profile/ProfilePage"));
const SettingsPage           = lazy(() => import("../pages/settings/SettingsPage"));
const AdminPage              = lazy(() => import("../pages/admin/AdminPage"));
const AdminPathsPage         = lazy(() => import("../pages/admin/AdminPathsPage"));
const AdminModuleEditorPage  = lazy(() => import("../pages/admin/AdminModuleEditorPage"));
const AdminUsersPage         = lazy(() => import("../pages/admin/AdminUsersPage"));
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
          <Route element={<DashboardLayout />}>
            <Route path={ROUTES.DASHBOARD} element={<DashboardPage />} />
            <Route path={ROUTES.LEARNING_PATHS} element={<LearningPathsPage />} />
            <Route path={ROUTES.LEARNING_PATH_DETAIL} element={<LearningPathDetailPage />} />
            <Route path={ROUTES.LESSON} element={<LessonPage />} />
            <Route path={ROUTES.CLASSROOM} element={<ClassroomPage />} />
            <Route path={ROUTES.CLASSROOM_DETAIL} element={<ClassroomDetailPage />} />
            <Route path={ROUTES.ANALYTICS} element={<AnalyticsPage />} />
            <Route path={ROUTES.COMMUNITY} element={<CommunityPage />} />
            <Route path={ROUTES.COMMUNITY_DETAIL} element={<CommunityPostPage />} />
            <Route path={ROUTES.CERTIFICATES} element={<CertificatesPage />} />
            <Route path={ROUTES.PROFILE} element={<ProfilePage />} />
            <Route path={ROUTES.SETTINGS} element={<SettingsPage />} />
            <Route element={<AdminRoute />}>
              <Route element={<AdminLayout />}>
                <Route path={ROUTES.ADMIN} element={<AdminPage />} />
                <Route path={ROUTES.ADMIN_PATHS} element={<AdminPathsPage />} />
                <Route path={ROUTES.ADMIN_PATH_MODULES} element={<AdminModuleEditorPage />} />
                <Route path={ROUTES.ADMIN_USERS} element={<AdminUsersPage />} />
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
