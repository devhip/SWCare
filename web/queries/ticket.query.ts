import { keepPreviousData, useQuery } from "@tanstack/react-query";
import { listTicketsApi, ticketApi, ticketCountsApi } from "@/lib/ticket.api";
import { deviceModelsApi, productsApi, programmersApi, settingsApi } from "@/lib/masterData.api";
import type { TicketListParams } from "@/types/ticket";

export const ticketKey = (id: number) => ["ticket", id] as const;

/** Prefix of every list and count query: invalidate it after a ticket changes. */
export const TICKET_LISTS_KEY = ["tickets"] as const;

export function useTicketQuery(id: number | null) {
  return useQuery({
    queryKey: ticketKey(id ?? 0),
    queryFn: async () => (await ticketApi(id!)).Result,
    enabled: !!id,
  });
}

export function useTicketListQuery(params: TicketListParams) {
  return useQuery({
    queryKey: [...TICKET_LISTS_KEY, "list", params],
    queryFn: () => listTicketsApi(params),
    placeholderData: keepPreviousData,
  });
}

export function useTicketCountsQuery() {
  return useQuery({
    queryKey: [...TICKET_LISTS_KEY, "counts"],
    queryFn: async () => (await ticketCountsApi()).Result,
    refetchInterval: 60_000,
  });
}

/** Dropdowns of the ticket forms; they rarely change. */
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

export function useProgrammersQuery(enabled: boolean) {
  return useQuery({
    queryKey: ["programmers"],
    queryFn: async () => (await programmersApi()).Result,
    enabled,
    staleTime: 5 * 60_000,
  });
}
