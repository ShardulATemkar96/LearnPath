tsimport { RootState } from "../store";

export const selectNotifications = (s: RootState) => s.notifications.notifications;
export const selectUnreadCount   = (s: RootState) => s.notifications.unreadCount;
export const selectNotifLoading  = (s: RootState) => s.notifications.loading;
