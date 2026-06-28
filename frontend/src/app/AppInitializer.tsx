import { useEffect } from "react";
import { useDispatch } from "react-redux";
import { AppDispatch } from "../redux/store";
import { tokenUtils } from "../utils/tokenUtils";
import { setUser, clearAuth } from "../redux/slices/authSlice";
import { fetchNotifications } from "../redux/slices/notificationSlice";

interface AppInitializerProps {
  children: React.ReactNode;
}

const AppInitializer = ({ children }: AppInitializerProps) => {
  const dispatch = useDispatch<AppDispatch>();

  useEffect(() => {
    const token = tokenUtils.getAccessToken();

    if (!token || tokenUtils.isTokenExpired(token)) {
      tokenUtils.clearTokens();
      dispatch(clearAuth());
      return;
    }

    try {
      const payload = JSON.parse(atob(token.split(".")[1]));

      dispatch(setUser({
        userId: payload.sub,
        email: payload.email,
        firstName: payload.firstName,
        lastName: payload.lastName,
        roles: Array.isArray(payload.role)
          ? payload.role
          : [payload.role],
      }));

      dispatch(fetchNotifications());
    } catch {
      tokenUtils.clearTokens();
      dispatch(clearAuth());
    }
  }, [dispatch]);

  return <>{children}</>;
};

export default AppInitializer;
