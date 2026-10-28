"use client";

import { useTranslation } from "react-i18next";
import Link from "@mui/material/Link";
import Stack from "@mui/material/Stack";
import Typography from "@mui/material/Typography";
import InsertDriveFileOutlined from "@mui/icons-material/InsertDriveFileOutlined";
import { downloadAttachment } from "@/lib/ticket.api";
import { formatBytes } from "@/lib/fileRules";
import type { Attachment } from "@/types/ticket";
import { color, typeScale } from "@/theme/tokens";

export default function AttachmentList({ ticketId, attachments, showEmpty }: { ticketId: number; attachments: Attachment[]; showEmpty?: boolean }) {
  const { t } = useTranslation("ticket");

  if (!attachments.length) {
    return showEmpty ? <Typography sx={{ ...typeScale.small, color: color.text.secondary }}>{t("files.none")}</Typography> : null;
  }

  return (
    <Stack spacing={0.5}>
      {attachments.map((file) => (
        <Stack key={file.Id} direction="row" spacing={1} sx={{ alignItems: "center" }}>
          <InsertDriveFileOutlined fontSize="small" sx={{ color: color.text.secondary }} />
          <Link
            component="button"
            type="button"
            underline="hover"
            onClick={() => void downloadAttachment(ticketId, file.Id, file.FileName).catch(() => undefined)}
            sx={{ ...typeScale.small, textAlign: "left", wordBreak: "break-all" }}
          >
            {file.FileName}
          </Link>
          <Typography sx={{ ...typeScale.caption, color: color.text.secondary, flexShrink: 0 }}>{formatBytes(file.SizeBytes)}</Typography>
        </Stack>
      ))}
    </Stack>
  );
}
