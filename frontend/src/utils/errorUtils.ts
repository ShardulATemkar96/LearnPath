/**
 * Centralized API error message extraction.
 *
 * Priority order:
 *   1. response.data.message      (ApiResponse envelope)
 *   2. response.data.error
 *   3. response.data.detail
 *   4. response.data.title        (ASP.NET ProblemDetails / ValidationProblemDetails)
 *   5. response.data.errors       (validation / ModelState messages)
 *   6. Genuine network failure
 *   7. Fallback generic message
 */

interface ApiErrorPayload {
  response?: {
    data?: {
      message?: unknown;
      error?: unknown;
      detail?: unknown;
      title?: unknown;
      errors?: unknown;
    };
  };
  message?: unknown;
  code?: string;
}

const asText = (value: unknown): string | null =>
  typeof value === "string" && value.trim().length > 0 ? value.trim() : null;

const collectValidationMessages = (errors: unknown): string | null => {
  if (!errors) return null;

  const messages: string[] = [];

  if (Array.isArray(errors)) {
    errors.forEach((entry) => {
      if (typeof entry === "string" && entry.trim()) messages.push(entry.trim());
    });
  } else if (typeof errors === "object") {
    Object.values(errors as Record<string, unknown>).forEach((entry) => {
      if (Array.isArray(entry)) {
        entry.forEach((item) => {
          if (typeof item === "string" && item.trim()) messages.push(item.trim());
        });
      } else if (typeof entry === "string" && entry.trim()) {
        messages.push(entry.trim());
      }
    });
  }

  return messages.length > 0 ? messages.join(" ") : null;
};

export const extractErrorMessage = (error: unknown): string => {
  const payload = (error ?? null) as ApiErrorPayload | null;

  if (payload) {
    const data = payload.response?.data;

    if (data) {
      const message = asText(data.message);
      if (message) return message;

      const errorField = asText(data.error);
      if (errorField) return errorField;

      const detail = asText(data.detail);
      if (detail) return detail;

      const title = asText(data.title);
      if (title) return title;

      const validation = collectValidationMessages(data.errors);
      if (validation) return validation;
    }

    // Genuine network failure (no response received).
    if (payload.code === "ERR_NETWORK" || asText(payload.message) === "Network Error") {
      return "Network error. Please check your connection and try again.";
    }
  }

  const rawMessage = asText(payload?.message);
  if (rawMessage) {
    // Raw Axios messages carry no business value — do not surface them.
    if (/^Request failed with status code \d+$/.test(rawMessage) || rawMessage === "AxiosError") {
      return "An unexpected error occurred. Please try again.";
    }
    return rawMessage;
  }

  return "An unexpected error occurred. Please try again.";
};
