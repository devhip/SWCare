import { create } from "zustand";
import { persist } from "zustand/middleware";
import type { LoginResult, MeResult } from "@/types/api";

type AuthState = {
  token: string | null;
  refreshToken: string | null;
  user: MeResult | null;
  hasHydrated: boolean;
  setSession: (login: LoginResult) => void;
  setUser: (user: MeResult) => void;
  logout: () => void;
};

export const useAuthStore = create<AuthState>()(
  persist(
    (set) => ({
      token: null,
      refreshToken: null,
      user: null,
      hasHydrated: false,
      setSession: (login) => set({ token: login.AccessToken, refreshToken: login.RefreshToken, user: login.User }),
      setUser: (user) => set({ user }),
      logout: () => set({ token: null, refreshToken: null, user: null }),
    }),
    {
      name: "swcare-auth",
      skipHydration: true,
      partialize: (s) => ({ token: s.token, refreshToken: s.refreshToken, user: s.user }),
      onRehydrateStorage: () => () => {
        useAuthStore.setState({ hasHydrated: true });
      },
    }
  )
);
