import { useState, useCallback } from "react";

export const useApiError = () => {
  const [error, setError] = useState<string | null>(null);

  const handleError = useCallback((err: any) => {
    const message =
      err?.response?.data?.message ??
      err?.message ??
      "An unexpected error occurred.";
    setError(message);
  }, []);

  const clearError = useCallback(() => setError(null), []);

  return { error, handleError, clearError };
};
