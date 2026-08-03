export type FormErrorTree = Record<string, Record<string, string[]>>;

export type ParsedApiError = {
  code?: string;
  /** Keyed by the input name the client posted, e.g. "user[user_from]". */
  fields: Record<string, string[]>;
  message: string;
};

function asRecord(value: unknown): Record<string, unknown> | null {
  return value != null && typeof value === "object" && !Array.isArray(value)
    ? (value as Record<string, unknown>)
    : null;
}

function asMessages(value: unknown): string[] {
  if (Array.isArray(value)) {
    return value.filter((item): item is string => typeof item === "string");
  }

  return typeof value === "string" ? [value] : [];
}

/**
 * Flattens osu-web's `{"form_error": {"user": {"user_from": ["..."]}}}` into
 * `{"user[user_from]": ["..."]}`, so a section can look its own inputs up directly.
 */
function flattenFormError(formError: unknown): Record<string, string[]> {
  const tree = asRecord(formError);
  const fields: Record<string, string[]> = {};

  if (tree == null) {
    return fields;
  }

  for (const [group, groupValue] of Object.entries(tree)) {
    const groupFields = asRecord(groupValue);

    if (groupFields == null) {
      continue;
    }

    for (const [field, messages] of Object.entries(groupFields)) {
      fields[`${group}[${field}]`] = asMessages(messages);
    }
  }

  return fields;
}

/**
 * Understands the three shapes the API can return: osu-web's `form_error` from the account
 * endpoints, Laravel style `errors` from the oauth endpoints, and the flat `{error, hint,
 * message}` used everywhere else.
 */
export function parseApiErrorBody(body: string, fallback: string): ParsedApiError {
  let json: unknown;

  try {
    json = JSON.parse(body);
  } catch {
    return { fields: {}, message: body.trim().length > 0 ? body.slice(0, 280) : fallback };
  }

  const record = asRecord(json);

  if (record == null) {
    return { fields: {}, message: fallback };
  }

  if (record.form_error != null) {
    const fields = flattenFormError(record.form_error);
    const summary = Object.entries(fields)
      .map(([key, messages]) => `${humanizeField(key)} ${messages.join(", ")}`)
      .join("; ");

    return { fields, message: summary.length > 0 ? summary : fallback };
  }

  if (record.errors != null) {
    const errors = asRecord(record.errors);
    const fields: Record<string, string[]> = {};

    if (errors != null) {
      for (const [field, messages] of Object.entries(errors)) {
        fields[field] = asMessages(messages);
      }
    }

    const summary = Object.entries(fields)
      .map(([field, messages]) => `${field}: ${messages.join(", ")}`)
      .join("; ");

    return { fields, message: summary.length > 0 ? summary : fallback };
  }

  const message =
    (typeof record.hint === "string" ? record.hint : null) ??
    (typeof record.message === "string" ? record.message : null) ??
    (typeof record.error === "string" ? record.error : null) ??
    fallback;

  return {
    code: typeof record.error === "string" ? record.error : undefined,
    fields: {},
    message,
  };
}

/** "user[user_from]" -> "user from" */
function humanizeField(key: string) {
  const match = /\[([^\]]+)\]$/.exec(key);

  return (match?.[1] ?? key).replaceAll("_", " ");
}
