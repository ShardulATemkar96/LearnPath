tsimport { useState, useEffect } from "react";

export const useDebounce = <T>(value: T, delay = 400): T => {
  const [debounced, setDebounced] = useState<T>(value);

  useEffect(() => {
    const timer = setTimeout(() => setDebounced(value), delay);
    return () => clearTimeout(timer);
  }, [value, delay]);

  return debounced;
};
