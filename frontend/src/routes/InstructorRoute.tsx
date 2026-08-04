import { Navigate, Outlet } from "react-router-dom";
import { useAuth } from "../hooks/useAuth";
import { ROUTES } from "../constants/routes";

const InstructorRoute = () => {
  const { isAuthenticated, isAdmin, isInstructor } = useAuth();

  if (!isAuthenticated) return <Navigate to={ROUTES.LOGIN} replace />;
  if (!isAdmin && !isInstructor) return <Navigate to={ROUTES.DASHBOARD} replace />;

  return <Outlet />;
};

export default InstructorRoute;
