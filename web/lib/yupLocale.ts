import { setLocale } from "yup";

/** Schema `.required()` emits the `validation.required` i18n key, not English yup text. */
setLocale({
  mixed: { required: "required" },
});
