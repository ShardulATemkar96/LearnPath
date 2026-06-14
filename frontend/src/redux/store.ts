import { configureStore } from "@reduxjs/toolkit";
import authReducer      from "./slices/authSlice";
import dashboardReducer from "./slices/dashboardSlice";
import pathReducer      from "./slices/pathSlice";
import classroomReducer from "./slices/classroomSlice";

export const store = configureStore({
  reducer: {
    auth:       authReducer,
    dashboard:  dashboardReducer,
    paths:      pathReducer,
    classrooms: classroomReducer,
  },
});

export type RootState   = ReturnType<typeof store.getState>;
export type AppDispatch = typeof store.dispatch;