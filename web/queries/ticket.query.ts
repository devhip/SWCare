import { useQuery } from "@tanstack/react-query";
import { ticketApi } from "@/lib/ticket.api";
import { deviceModelsApi, productsApi, settingsApi } from "@/lib/masterData.api";

export const ticketKey = (id: number) => ["ticket", id] as const;

export function useTicketQuery(id: number | null) {
  return useQuery({
    queryKey: ticketKey(id ?? 0),
    queryFn: async () => (await ticketApi(id!)).Result,
    enabled: !!id,
  });
}

/** Dropdowns of the ticket form; they rarely change. */
export function useTicketFormDataQuery() {
  return useQuery({
    queryKey: ["ticketFormData"],
    queryFn: async () => {
      const [products, deviceModels, settings] = await Promise.all([productsApi(), deviceModelsApi(), settingsApi()]);
      return { products: products.Result, deviceModels: deviceModels.Result, settings: settings.Result };
    },
    staleTime: 5 * 60_000,
  });
}
