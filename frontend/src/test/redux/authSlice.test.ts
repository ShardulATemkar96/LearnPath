tsimport { describe, it, expect, beforeEach } from "vitest";
import { configureStore } from "@reduxjs/toolkit";
import authReducer, {
  clearAuth, setUser,
} from "../../redux/slices/authSlice";

const makeStore = () =>
  configureStore({ reducer: { auth: authReducer } });

describe("authSlice", () => {
  it("initial state is unauthenticated", () => {
    const store = makeStore();
    const state = store.getState().auth;
    expect(state.isAuthenticated).toBe(false);
    expect(state.user).toBeNull();
    expect(state.loading).toBe(false);
    expect(state.error).toBeNull();
  });

  it("setUser authenticates the user", () => {
    const store = makeStore();
    store.dispatch(setUser({
      userId:    "user-1",
      email:     "test@test.com",
      firstName: "John",
      lastName:  "Doe",
      roles:     ["Student"],
    }));

    const state = store.getState().auth;
    expect(state.isAuthenticated).toBe(true);
    expect(state.user?.email).toBe("test@test.com");
    expect(state.user?.roles).toContain("Student");
  });

  it("clearAuth resets to initial state", () => {
    const store = makeStore();
    store.dispatch(setUser({
      userId:    "user-1",
      email:     "test@test.com",
      firstName: "John",
      lastName:  "Doe",
      roles:     ["Student"],
    }));
    store.dispatch(clearAuth());

    const state = store.getState().auth;
    expect(state.isAuthenticated).toBe(false);
    expect(state.user).toBeNull();
  });
});
