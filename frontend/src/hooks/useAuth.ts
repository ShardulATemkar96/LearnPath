import { useDispatch, useSelector } from "react-redux";
import { AppDispatch } from "../redux/store";
import {
  selectAuthError,
  selectAuthLoading,
  selectCurrentUser,
  selectIsAuthenticated,
  selectUserRoles,
} from "../redux/selectors/authSelectors";
import { loginThunk, logoutThunk, registerThunk } from "../redux/slices/authSlice";
import { LoginRequest, RegisterRequest } from "../types/auth.types";
import { ROLES } from "../constants/roles";

export const useAuth = () => {
  const dispatch = useDispatch<AppDispatch>();
  const user = useSelector(selectCurrentUser);
  const isAuthenticated = useSelector(selectIsAuthenticated);
  const loading = useSelector(selectAuthLoading);
  const error = useSelector(selectAuthError);
  const roles = useSelector(selectUserRoles);

  const login = (payload: LoginRequest) => dispatch(loginThunk(payload));
  const register = (payload: RegisterRequest) => dispatch(registerThunk(payload));
  const logout = () => dispatch(logoutThunk());

  const isAdmin = roles.includes(ROLES.ADMIN);
  const isInstructor = roles.includes(ROLES.INSTRUCTOR);
  const isStudent = roles.includes(ROLES.STUDENT);

  return { user, isAuthenticated, loading, error, roles, isAdmin, isInstructor, isStudent, login, register, logout };
};