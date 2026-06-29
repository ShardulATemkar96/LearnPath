import { createAsyncThunk, createSlice } from "@reduxjs/toolkit";
import {
  notificationService,
  AppNotification,
} from "../../services/notificationService";

interface NotificationState {
  notifications: AppNotification[];
  unreadCount: number;
  loading: boolean;
}

const initialState: NotificationState = {
  notifications: [],
  unreadCount: 0,
  loading: false,
};

export const fetchNotifications = createAsyncThunk(
  "notifications/fetchMy",
  async () => notificationService.getMy()
);

export const markReadThunk = createAsyncThunk(
  "notifications/markRead",
  async (id: number) => { await notificationService.markRead(id); return id; }
);

export const markAllReadThunk = createAsyncThunk(
  "notifications/markAllRead",
  async () => { await notificationService.markAllRead(); }
);

const notificationSlice = createSlice({
  name: "notifications",
  initialState,
  reducers: {},
  extraReducers: (builder) => {
    builder
      .addCase(fetchNotifications.pending,   (s) => { s.loading = true; })
      .addCase(fetchNotifications.fulfilled, (s, a) => {
        s.loading       = false;
        s.notifications = a.payload;
        s.unreadCount   = a.payload.filter((n) => !n.isRead).length;
      });

    builder.addCase(markReadThunk.fulfilled, (s, a) => {
      const n = s.notifications.find((n) => n.id === a.payload);
      if (n && !n.isRead) { n.isRead = true; s.unreadCount = Math.max(0, s.unreadCount - 1); }
    });

    builder.addCase(markAllReadThunk.fulfilled, (s) => {
      s.notifications.forEach((n) => { n.isRead = true; });
      s.unreadCount = 0;
    });
  },
});

export default notificationSlice.reducer;
