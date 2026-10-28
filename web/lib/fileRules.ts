/** Same limits as AttachmentRules in the API (Q20). */
export const MAX_FILE_BYTES = 20 * 1024 * 1024;
export const MAX_FILES = 10;

export const ALLOWED_EXTENSIONS = [
  ".jpg", ".jpeg", ".png", ".gif", ".webp", ".bmp", ".heic",
  ".pdf",
  ".xls", ".xlsx", ".csv",
  ".doc", ".docx",
  ".txt", ".log",
  ".zip", ".rar", ".7z",
  ".mp4", ".mov", ".webm",
];

export type FileProblem = { key: "fileTooLarge" | "fileType" | "tooManyFiles"; name?: string };

export function checkFiles(files: File[]): FileProblem | null {
  if (files.length > MAX_FILES) return { key: "tooManyFiles" };
  for (const file of files) {
    const dot = file.name.lastIndexOf(".");
    const ext = dot >= 0 ? file.name.slice(dot).toLowerCase() : "";
    if (!ALLOWED_EXTENSIONS.includes(ext)) return { key: "fileType", name: file.name };
    if (file.size > MAX_FILE_BYTES) return { key: "fileTooLarge", name: file.name };
  }
  return null;
}

export function formatBytes(bytes: number): string {
  if (bytes < 1024) return `${bytes} B`;
  if (bytes < 1024 * 1024) return `${(bytes / 1024).toFixed(0)} KB`;
  return `${(bytes / 1024 / 1024).toFixed(1)} MB`;
}
