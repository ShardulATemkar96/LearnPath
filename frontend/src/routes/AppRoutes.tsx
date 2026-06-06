import { lazy, Suspense } from "react";
import { Routes, Route } from "react-router-dom";
import { ROUTES } from "../constants/routes";
import ProtectedRoute from "./ProtectedRoute";
import AdminRoute from "./AdminRoute";
import GuestRoute from "./GuestRoute";
import Loader from "../components/common/Loader/Loader";
import MainLayout from "../layouts/MainLayout";
import DashboardLayout from "../layouts/DashboardLayout";
import AuthLayout from "../layouts/AuthLayout";
import AdminLayout from "../layouts/AdminLayout";

// Eager
import LoginPage from "../pages/auth/LoginPage";
import RegisterPage from "../pages/auth/RegisterPage";
import LandingPage from "../pages/public/LandingPage";
import NotFoundPage from "../pages/errors/NotFoundPage";

// Lazy
const DashboardPage = lazy(() => import("../pages/dashboard/DashboardPage"));
const LearningPathsPage = lazy(() => import("../pages/learningPaths/LearningPathsPage"));
const AnalyticsPage = lazy(() => import("../pages/analytics/AnalyticsPage"));
const AdminPage = lazy(() => import("../pages/admin/AdminPage"));
const ProfilePage = lazy(() => import("../pages/profile/ProfilePage"));

const AppRoutes = () => (
  <Suspense fallback={<Loader fullScreen />}>
    <Routes>
      {/* Public */}
      <Route element={<MainLayout />}>
        <Route path={ROUTES.HOME} element={<LandingPage />} />
      </Route>

      {/* Guest only */}
      <Route element={<GuestRoute />}>
        <Route element={<AuthLayout />}>
          <Route path={ROUTES.LOGIN} element={<LoginPage />} />
          <Route path={ROUTES.REGISTER} element={<RegisterPage />} />
        </Route>
      </Route>

      {/* Protected */}
      <Route element={<ProtectedRoute />}>
        <Route element={<DashboardLayout />}>
          <Route path={ROUTES.DASHBOARD} element={<DashboardPage />} />
          <Route path={ROUTES.LEARNING_PATHS} element={<LearningPathsPage />} />
          <Route path={ROUTES.ANALYTICS} element={<AnalyticsPage />} />
          <Route path={ROUTES.PROFILE} element={<ProfilePage />} />
        </Route>
      </Route>

      {/* Admin only */}
      <Route element={<AdminRoute />}>
        <Route element={<AdminLayout />}>
          <Route path={ROUTES.ADMIN} element={<AdminPage />} />
        </Route>
      </Route>

      <Route path={ROUTES.NOT_FOUND} element={<NotFoundPage />} />
    </Routes>
  </Suspense>
);

export default AppRoutes;