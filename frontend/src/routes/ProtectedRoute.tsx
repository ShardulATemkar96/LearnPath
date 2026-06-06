import { Navigate, Outlet, useLocation } from "react-router-dom";
import { useAuth } from "../hooks/useAuth";
import { ROUTES } from "../constants/routes";
import Loader from "../components/common/Loader/Loader";

const ProtectedRoute = () => {
  const { isAuthenticated, loading } = useAuth();
  const location = useLocation();

  if (loading) return <Loader />;

  return isAuthenticated
    ? <Outlet />
    : <Navigate to={ROUTES.LOGIN} state={{ from: location }} replace />;
};

export default ProtectedRoute;