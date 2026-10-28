"use client";

import { useState } from "react";
import { Controller, useForm, useWatch } from "react-hook-form";
import { yupResolver } from "@hookform/resolvers/yup";
import * as yup from "yup";
import { useQuery } from "@tanstack/react-query";
import { useTranslation } from "react-i18next";
import Autocomplete from "@mui/material/Autocomplete";
import Button from "@mui/material/Button";
import MenuItem from "@mui/material/MenuItem";
import Stack from "@mui/material/Stack";
import TextField from "@mui/material/TextField";
import ToggleButton from "@mui/material/ToggleButton";
import ToggleButtonGroup from "@mui/material/ToggleButtonGroup";
import Typography from "@mui/material/Typography";
import { useDebouncedValue } from "@/hooks/useDebouncedValue";
import { customerNamesApi } from "@/lib/ticket.api";
import { userLookupApi } from "@/lib/masterData.api";
import { useAuthStore } from "@/stores/auth.store";
import {
  TICKET_PRIORITIES,
  TICKET_TYPES,
  type DeviceModel,
  type Product,
  type SaveTicket,
  type TicketDetail,
  type TicketPriority,
  type TicketType,
  type UserLookup,
} from "@/types/ticket";
import { card, color, typeScale } from "@/theme/tokens";
import FilePicker from "./FilePicker";

const normalize = (text: string) => text.replace(/\s+/g, " ").trim();

function buildSchema(testedRequired: (type: TicketType) => boolean, template: string) {
  return yup.object({
    title: yup.string().trim().required().max(200),
    type: yup.mixed<TicketType>().oneOf(TICKET_TYPES).required(),
    productId: yup.number().required().min(1, "required"),
    version: yup.string().max(50),
    customerName: yup.string().max(200),
    description: yup.string().trim().required().max(20000),
    // Customer Support says what it already tried (Q55); the untouched headings do not count (Q56).
    testedDetails: yup
      .string()
      .max(8000)
      .test("tested", "testedRequired", function (value) {
        if (!testedRequired(this.parent.type)) return true;
        return !!value?.trim() && normalize(value) !== normalize(template);
      }),
    requestedPriority: yup.mixed<TicketPriority>().oneOf(TICKET_PRIORITIES).required(),
    requester: yup.mixed<UserLookup>().nullable(),
    deviceModelIds: yup.array(yup.number().required()).required(),
  });
}
type FormValues = yup.InferType<ReturnType<typeof buildSchema>>;

type Props = {
  products: Product[];
  deviceModels: DeviceModel[];
  testedTemplate: string;
  /** Edit: the ticket to change. */
  initial?: TicketDetail;
  submitLabel: string;
  submittingLabel: string;
  /** New tickets take their files with them; an edit adds files from the ticket page. */
  withFiles: boolean;
  onSubmit: (values: SaveTicket, files: File[]) => Promise<void>;
};

export default function TicketForm({ products, deviceModels, testedTemplate, initial, submitLabel, submittingLabel, withFiles, onSubmit }: Props) {
  const { t } = useTranslation("ticket");
  const { t: tv } = useTranslation("validation");
  const user = useAuthStore((s) => s.user);
  const [files, setFiles] = useState<File[]>([]);

  const isSupport = user?.DepartmentKind === "Support";
  const testedRequired = (type: TicketType) => isSupport && (type === "Bug" || type === "FeatureRequest");

  const {
    control,
    register,
    handleSubmit,
    formState: { errors, isSubmitting },
  } = useForm<FormValues>({
    resolver: yupResolver(buildSchema(testedRequired, testedTemplate)),
    defaultValues: {
      title: initial?.Title ?? "",
      type: initial?.Type ?? "Bug",
      productId: initial?.Product.Id ?? 0,
      version: initial?.Version ?? "",
      customerName: initial?.CustomerName ?? "",
      description: initial?.Description ?? "",
      testedDetails: initial ? initial.TestedDetails ?? "" : testedTemplate,
      requestedPriority: initial?.RequestedPriority ?? "Normal",
      requester: initial?.Requester ? { Id: initial.Requester.Id, DisplayName: initial.Requester.DisplayName, DepartmentId: 0, DepartmentName: "" } : null,
      deviceModelIds: initial?.DeviceModels.map((m) => m.Id) ?? [],
    },
  });

  const type = useWatch({ control, name: "type" });

  const [userSearch, setUserSearch] = useState("");
  const debouncedUserSearch = useDebouncedValue(userSearch);
  const users = useQuery({
    queryKey: ["userLookup", debouncedUserSearch],
    queryFn: async () => (await userLookupApi(debouncedUserSearch)).Result,
  });

  const [customerSearch, setCustomerSearch] = useState("");
  const debouncedCustomerSearch = useDebouncedValue(customerSearch);
  const customers = useQuery({
    queryKey: ["customerNames", debouncedCustomerSearch],
    queryFn: async () => (await customerNamesApi(debouncedCustomerSearch)).Result,
  });

  /** "required" and "testedRequired" are i18n keys (lib/yupLocale); anything else is yup's own text. */
  const message = (key?: string) => {
    if (!key) return undefined;
    if (key === "testedRequired") return t("validation.testedRequired");
    if (key === "required") return tv("required");
    return key;
  };

  const submit = handleSubmit(async (values) => {
    await onSubmit(
      {
        Title: values.title,
        Type: values.type,
        ProductId: values.productId,
        Version: values.version || null,
        CustomerName: values.customerName || null,
        Description: values.description,
        TestedDetails: values.testedDetails || null,
        RequestedPriority: values.requestedPriority,
        RequesterId: values.requester?.Id ?? null,
        DeviceModelIds: values.deviceModelIds,
      },
      files
    );
  });

  return (
    <Stack
      component="form"
      spacing={4.5}
      onSubmit={submit}
      noValidate
      sx={{ p: { xs: 4, md: 6 }, ...card }}
    >
      <TextField label={t("field.title")} required fullWidth {...register("title")} error={!!errors.title} helperText={message(errors.title?.message)} />

      <Stack direction={{ xs: "column", md: "row" }} spacing={4}>
        <Controller
          control={control}
          name="type"
          render={({ field }) => (
            <TextField select label={t("field.type")} required fullWidth {...field}>
              {TICKET_TYPES.map((value) => (
                <MenuItem key={value} value={value}>
                  {t(`type.${value}`)}
                </MenuItem>
              ))}
            </TextField>
          )}
        />
        <Controller
          control={control}
          name="productId"
          render={({ field }) => (
            <TextField
              select
              label={t("field.product")}
              required
              fullWidth
              {...field}
              value={field.value || ""}
              error={!!errors.productId}
              helperText={message(errors.productId?.message)}
            >
              {products.map((product) => (
                <MenuItem key={product.Id} value={product.Id}>
                  {product.Name}
                </MenuItem>
              ))}
            </TextField>
          )}
        />
      </Stack>

      <Stack direction={{ xs: "column", md: "row" }} spacing={4}>
        <TextField label={t("field.version")} fullWidth {...register("version")} />
        <Controller
          control={control}
          name="customerName"
          render={({ field }) => (
            <Autocomplete
              freeSolo
              fullWidth
              options={customers.data ?? []}
              inputValue={field.value ?? ""}
              onInputChange={(_, value) => {
                field.onChange(value);
                setCustomerSearch(value);
              }}
              renderInput={(params) => <TextField {...params} label={t("field.customerName")} />}
            />
          )}
        />
      </Stack>

      <Controller
        control={control}
        name="deviceModelIds"
        render={({ field }) => (
          <Autocomplete
            multiple
            options={deviceModels}
            getOptionLabel={(option) => option.Name}
            value={deviceModels.filter((model) => field.value.includes(model.Id))}
            onChange={(_, value) => field.onChange(value.map((model) => model.Id))}
            renderInput={(params) => <TextField {...params} label={t("field.deviceModels")} />}
          />
        )}
      />

      <TextField
        label={t("field.description")}
        required
        fullWidth
        multiline
        minRows={4}
        {...register("description")}
        error={!!errors.description}
        helperText={message(errors.description?.message)}
      />

      <TextField
        label={t("field.testedDetails")}
        required={testedRequired(type)}
        fullWidth
        multiline
        minRows={5}
        {...register("testedDetails")}
        error={!!errors.testedDetails}
        helperText={message(errors.testedDetails?.message) ?? (isSupport ? t("hint.testedRequired") : undefined)}
      />

      <Stack spacing={1}>
        <Typography sx={{ ...typeScale.small, color: color.text.secondary }}>{t("field.requestedPriority")}</Typography>
        <Controller
          control={control}
          name="requestedPriority"
          render={({ field }) => (
            <ToggleButtonGroup exclusive color="primary" value={field.value} onChange={(_, value) => value && field.onChange(value)} sx={{ flexWrap: "wrap" }}>
              {TICKET_PRIORITIES.map((value) => (
                <ToggleButton key={value} value={value} sx={{ px: 2 }}>
                  {t(`priority.${value}`)}
                </ToggleButton>
              ))}
            </ToggleButtonGroup>
          )}
        />
      </Stack>

      <Controller
        control={control}
        name="requester"
        render={({ field }) => (
          <Autocomplete
            options={(users.data ?? []).filter((option) => option.Id !== user?.UserId)}
            value={field.value ?? null}
            onChange={(_, value) => field.onChange(value)}
            onInputChange={(_, value, reason) => reason === "input" && setUserSearch(value)}
            getOptionLabel={(option) => option.DisplayName}
            isOptionEqualToValue={(option, value) => option.Id === value.Id}
            renderOption={(props, option) => (
              <li {...props} key={option.Id}>
                <Stack>
                  <span>{option.DisplayName}</span>
                  <Typography component="span" sx={{ ...typeScale.caption, color: color.text.secondary }}>
                    {option.DepartmentName}
                  </Typography>
                </Stack>
              </li>
            )}
            renderInput={(params) => <TextField {...params} label={t("field.requester")} helperText={t("hint.requester")} />}
          />
        )}
      />

      {withFiles ? <FilePicker files={files} onChange={setFiles} disabled={isSubmitting} /> : null}

      <Stack direction="row" sx={{ justifyContent: "flex-end" }}>
        <Button type="submit" variant="contained" size="large" disabled={isSubmitting}>
          {isSubmitting ? submittingLabel : submitLabel}
        </Button>
      </Stack>
    </Stack>
  );
}
