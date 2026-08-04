import { useState, useCallback } from "react";
import { extractErrorMessage } from "../utils/errorUtils";

export const useApiError = () => {
  const [error, setError] = useState<string | null>(null);

  const handleError = useCallback((err: unknown) => {
    setError(extractErrorMessage(err));
  }, []);

  const clearError = useCallback(() => setError(null), []);

  return { error, handleError, clearError };
};
