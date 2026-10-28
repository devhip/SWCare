"use client";

import { useState } from "react";
import { useTranslation } from "react-i18next";
import Alert from "@mui/material/Alert";
import Button from "@mui/material/Button";
import Divider from "@mui/material/Divider";
import Stack from "@mui/material/Stack";
import TextField from "@mui/material/TextField";
import Typography from "@mui/material/Typography";
import { addCommentApi } from "@/lib/ticket.api";
import { formatDateTime } from "@/lib/format";
import type { TicketDetail } from "@/types/ticket";
import { color, typeScale } from "@/theme/tokens";
import AttachmentList from "./AttachmentList";
import FilePicker from "./FilePicker";

export default function Comments({
  ticket,
  isReporter,
  onChanged,
}: {
  ticket: TicketDetail;
  isReporter: boolean;
  onChanged: (ticket: TicketDetail) => void;
}) {
  const { t, i18n } = useTranslation("ticket");
  const [body, setBody] = useState("");
  const [files, setFiles] = useState<File[]>([]);
  const [sending, setSending] = useState(false);

  const canComment = ticket.AllowedActions.includes("Comment");

  const send = async () => {
    if (!body.trim() && !files.length) return;
    setSending(true);
    try {
      const res = await addCommentApi(ticket.Id, body, files);
      setBody("");
      setFiles([]);
      onChanged(res.Result);
    } catch {
      // The error toast is shown by lib/http.
    } finally {
      setSending(false);
    }
  };

  return (
    <Stack spacing={2}>
      {ticket.Comments.length === 0 ? (
        <Typography sx={{ ...typeScale.small, color: color.text.secondary }}>{t("comment.empty")}</Typography>
      ) : (
        <Stack spacing={2} divider={<Divider flexItem />}>
          {ticket.Comments.map((comment) => (
            <Stack key={comment.Id} spacing={0.5}>
              <Typography sx={{ ...typeScale.caption, color: color.text.secondary }}>
                <strong>{comment.Author.DisplayName}</strong> · {formatDateTime(comment.CreatedAt, i18n.language)}
              </Typography>
              {comment.Body ? (
                <Typography sx={{ ...typeScale.body, color: color.text.primary, whiteSpace: "pre-wrap", wordBreak: "break-word" }}>{comment.Body}</Typography>
              ) : null}
              <AttachmentList ticketId={ticket.Id} attachments={comment.Attachments} />
            </Stack>
          ))}
        </Stack>
      )}

      {canComment ? (
        <Stack spacing={1.5}>
          {ticket.Status === "WaitingInfo" && isReporter ? <Alert severity="info">{t("comment.answerHint")}</Alert> : null}
          <TextField
            placeholder={t("comment.placeholder")}
            multiline
            minRows={3}
            fullWidth
            value={body}
            onChange={(e) => setBody(e.target.value)}
            disabled={sending}
          />
          <FilePicker files={files} onChange={setFiles} disabled={sending} />
          <Stack direction="row" sx={{ justifyContent: "flex-end" }}>
            <Button variant="contained" onClick={() => void send()} disabled={sending || (!body.trim() && !files.length)}>
              {sending ? t("comment.sending") : t("comment.send")}
            </Button>
          </Stack>
        </Stack>
      ) : null}
    </Stack>
  );
}
