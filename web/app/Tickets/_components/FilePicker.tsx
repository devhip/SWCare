"use client";

import { useRef } from "react";
import { useTranslation } from "react-i18next";
import { toast } from "react-toastify";
import Button from "@mui/material/Button";
import Chip from "@mui/material/Chip";
import Stack from "@mui/material/Stack";
import Typography from "@mui/material/Typography";
import AttachFileRounded from "@mui/icons-material/AttachFileRounded";
import { ALLOWED_EXTENSIONS, MAX_FILES, checkFiles, formatBytes } from "@/lib/fileRules";
import { color, typeScale } from "@/theme/tokens";

/** Pick files (checked against the API limits) and show them as removable chips. */
export default function FilePicker({ files, onChange, disabled }: { files: File[]; onChange: (files: File[]) => void; disabled?: boolean }) {
  const { t } = useTranslation("ticket");
  const input = useRef<HTMLInputElement>(null);

  const add = (picked: FileList | null) => {
    if (!picked?.length) return;
    const next = [...files, ...Array.from(picked)];
    const problem = checkFiles(next);
    if (problem) toast.error(t(`validation.${problem.key}`, { name: problem.name, max: MAX_FILES }));
    else onChange(next);
    if (input.current) input.current.value = "";
  };

  return (
    <Stack spacing={1}>
      <Stack direction="row" spacing={1} sx={{ alignItems: "center", flexWrap: "wrap", gap: 1 }}>
        <Button
          variant="outlined"
          size="small"
          startIcon={<AttachFileRounded />}
          onClick={() => input.current?.click()}
          disabled={disabled}
        >
          {files.length ? t("files.addMore") : t("files.add")}
        </Button>
        {files.map((file, index) => (
          <Chip
            key={`${file.name}-${index}`}
            label={`${file.name} · ${formatBytes(file.size)}`}
            onDelete={disabled ? undefined : () => onChange(files.filter((_, i) => i !== index))}
          />
        ))}
      </Stack>
      <Typography sx={{ ...typeScale.caption12, color: color.text.secondary }}>{t("hint.files", { max: MAX_FILES })}</Typography>
      <input
        ref={input}
        type="file"
        multiple
        hidden
        accept={ALLOWED_EXTENSIONS.join(",")}
        onChange={(e) => add(e.target.files)}
      />
    </Stack>
  );
}
