tsimport { useState, useMemo } from "react";

export const usePagination = <T>(items: T[], pageSize = 10) => {
  const [page, setPage] = useState(1);

  const totalPages = Math.ceil(items.length / pageSize);

  const paginated = useMemo(() => {
    const start = (page - 1) * pageSize;
    return items.slice(start, start + pageSize);
  }, [items, page, pageSize]);

  const goToPage    = (p: number) => setPage(Math.min(Math.max(1, p), totalPages));
  const nextPage    = () => goToPage(page + 1);
  const prevPage    = () => goToPage(page - 1);
  const hasNext     = page < totalPages;
  const hasPrev     = page > 1;

  return {
    page, totalPages, paginated,
    goToPage, nextPage, prevPage,
    hasNext, hasPrev,
  };
};
