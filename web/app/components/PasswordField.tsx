"use client";

import { forwardRef, useState } from "react";
import { useTranslation } from "react-i18next";
import IconButton from "@mui/material/IconButton";
import InputAdornment from "@mui/material/InputAdornment";
import TextField, { type TextFieldProps } from "@mui/material/TextField";
import Visibility from "@mui/icons-material/Visibility";
import VisibilityOff from "@mui/icons-material/VisibilityOff";

/** TextField with a show / hide button; works with react-hook-form register(). */
const PasswordField = forwardRef<HTMLInputElement, Omit<TextFieldProps, "type">>(function PasswordField(props, ref) {
  const { t } = useTranslation("auth");
  const [visible, setVisible] = useState(false);

  return (
    <TextField
      {...props}
      inputRef={ref}
      type={visible ? "text" : "password"}
      fullWidth
      slotProps={{
        input: {
          endAdornment: (
            <InputAdornment position="end">
              <IconButton
                type="button"
                onClick={() => setVisible((open) => !open)}
                aria-label={visible ? t("hidePassword") : t("showPassword")}
                edge="end"
              >
                {visible ? <VisibilityOff /> : <Visibility />}
              </IconButton>
            </InputAdornment>
          ),
        },
      }}
    />
  );
});

export default PasswordField;
