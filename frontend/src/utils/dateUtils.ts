export const dateUtils = {
  format: (date: string | Date, options?: Intl.DateTimeFormatOptions): string => {
    const d = typeof date === "string" ? new Date(date) : date;
    return d.toLocaleDateString("en-US", options ?? {
      year: "numeric", month: "short", day: "numeric",
    });
  },

  timeAgo: (date: string | Date): string => {
    const d      = typeof date === "string" ? new Date(date) : date;
    const now    = new Date();
    const diffMs = now.getTime() - d.getTime();
    const diffSec  = Math.floor(diffMs / 1000);
    const diffMin  = Math.floor(diffSec / 60);
    const diffHr   = Math.floor(diffMin / 60);
    const diffDay  = Math.floor(diffHr / 24);
    const diffWeek = Math.floor(diffDay / 7);
    const diffMon  = Math.floor(diffDay / 30);

    if (diffSec  < 60)  return "just now";
    if (diffMin  < 60)  return `${diffMin}m ago`;
    if (diffHr   < 24)  return `${diffHr}h ago`;
    if (diffDay  < 7)   return `${diffDay}d ago`;
    if (diffWeek < 5)   return `${diffWeek}w ago`;
    if (diffMon  < 12)  return `${diffMon}mo ago`;
    return `${Math.floor(diffMon / 12)}y ago`;
  },

  isOverdue: (date: string | Date): boolean => {
    const d = typeof date === "string" ? new Date(date) : date;
    return d < new Date();
  },

  isFuture: (date: string | Date): boolean => {
    const d = typeof date === "string" ? new Date(date) : date;
    return d > new Date();
  },
};
