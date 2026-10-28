"use client";

import { Fragment, useState } from "react";
import { usePathname, useRouter } from "next/navigation";
import { useTranslation } from "react-i18next";
import Box from "@mui/material/Box";
import Divider from "@mui/material/Divider";
import Drawer from "@mui/material/Drawer";
import IconButton from "@mui/material/IconButton";
import ListItemButton from "@mui/material/ListItemButton";
import ListItemIcon from "@mui/material/ListItemIcon";
import ListItemText from "@mui/material/ListItemText";
import Menu from "@mui/material/Menu";
import MenuItem from "@mui/material/MenuItem";
import Stack from "@mui/material/Stack";
import Typography from "@mui/material/Typography";
import MenuRounded from "@mui/icons-material/MenuRounded";
import AssignmentIndRounded from "@mui/icons-material/AssignmentIndRounded";
import AddCircleOutlineRounded from "@mui/icons-material/AddCircleOutlineRounded";
import FactCheckRounded from "@mui/icons-material/FactCheckRounded";
import PlaylistAddCheckRounded from "@mui/icons-material/PlaylistAddCheckRounded";
import ListAltRounded from "@mui/icons-material/ListAltRounded";
import ViewKanbanRounded from "@mui/icons-material/ViewKanbanRounded";
import GroupRounded from "@mui/icons-material/GroupRounded";
import ApartmentRounded from "@mui/icons-material/ApartmentRounded";
import TuneRounded from "@mui/icons-material/TuneRounded";
import Chip from "@mui/material/Chip";
import { useTicketCountsQuery } from "@/queries/ticket.query";
import AccountCircleRounded from "@mui/icons-material/AccountCircleRounded";
import KeyRounded from "@mui/icons-material/KeyRounded";
import LogoutRounded from "@mui/icons-material/LogoutRounded";
import { useAuthStore } from "@/stores/auth.store";
import { logoutApi } from "@/lib/auth.api";
import { MENU, type MenuEntry } from "@/lib/menu";
import { hasAnyRole, type AppRole } from "@/lib/roles";
import { color, layout, nav, radiusPx, typeScale } from "@/theme/tokens";
import LanguageSwitch from "./LanguageSwitch";
import NotificationBell from "./NotificationBell";
import Link from "./Link";
import RoleGuard from "./RoleGuard";

const ICONS: Record<MenuEntry["icon"], typeof AssignmentIndRounded> = {
  myTasks: AssignmentIndRounded,
  newTicket: AddCircleOutlineRounded,
  approvals: FactCheckRounded,
  triage: PlaylistAddCheckRounded,
  kanban: ViewKanbanRounded,
  allTickets: ListAltRounded,
  adminUsers: GroupRounded,
  adminDepartments: ApartmentRounded,
  adminMasterData: TuneRounded,
};

function Brand() {
  const { t } = useTranslation("common");
  return (
    <Stack sx={{ px: 1 }}>
      <Typography sx={{ ...typeScale.h3_20, color: color.text.brand }}>{t("appName")}</Typography>
      <Typography sx={{ ...typeScale.caption12, color: color.text.secondary }}>{t("appTagline")}</Typography>
    </Stack>
  );
}

function SideMenu({ onNavigate }: { onNavigate?: () => void }) {
  const { t } = useTranslation("navigation");
  const pathname = usePathname();
  const user = useAuthStore((s) => s.user);
  const counts = useTicketCountsQuery();

  return (
    <Stack spacing={`${nav.gap}px`} sx={{ p: 2 }}>
      {MENU.filter((entry) => hasAnyRole(user, entry.roles)).map((entry, index, entries) => {
        const Icon = ICONS[entry.icon];
        const startsAdmin = entry.section === "admin" && entries[index - 1]?.section !== "admin";
        const selected = pathname === entry.route;
        const badge = entry.badge ? (counts.data?.[entry.badge] ?? 0) : 0;
        return (
          <Fragment key={entry.route}>
            {startsAdmin ? (
              <Typography sx={{ ...typeScale.caption12, color: color.text.tertiary, px: 2, pt: 3, pb: 1, textTransform: "uppercase" }}>
                {t("adminSection")}
              </Typography>
            ) : null}
            <ListItemButton
              component={Link}
              href={entry.route}
              selected={selected}
              onClick={onNavigate}
              sx={{
                minHeight: nav.mainHeight,
                borderRadius: `${nav.radius}px`,
                "&.Mui-selected": { bgcolor: color.surface.brandSubtle, color: color.text.brand },
              }}
            >
              <ListItemIcon sx={{ minWidth: 40, color: selected ? color.text.brand : color.text.secondary }}>
                <Icon />
              </ListItemIcon>
              <ListItemText primary={t(entry.labelKey)} slotProps={{ primary: { sx: typeScale.bodyStrong16 } }} />
              {badge > 0 ? <Chip label={badge} size="small" color="primary" sx={{ height: 22, minWidth: 28 }} /> : null}
            </ListItemButton>
          </Fragment>
        );
      })}
    </Stack>
  );
}

function UserMenu() {
  const { t } = useTranslation("common");
  const router = useRouter();
  const user = useAuthStore((s) => s.user);
  const [anchor, setAnchor] = useState<HTMLElement | null>(null);

  const logout = async () => {
    setAnchor(null);
    const refreshToken = useAuthStore.getState().refreshToken;
    if (refreshToken) await logoutApi(refreshToken).catch(() => undefined);
    useAuthStore.getState().logout();
    router.replace("/Login/");
  };

  return (
    <>
      <IconButton onClick={(e) => setAnchor(e.currentTarget)} aria-label={user?.DisplayName}>
        <AccountCircleRounded />
      </IconButton>
      <Menu anchorEl={anchor} open={!!anchor} onClose={() => setAnchor(null)}>
        <Box sx={{ px: 2, py: 1 }}>
          <Typography sx={typeScale.bodyStrong16}>{user?.DisplayName}</Typography>
          <Typography sx={{ ...typeScale.caption12, color: color.text.secondary }}>{user?.DepartmentName}</Typography>
        </Box>
        <Divider />
        <MenuItem component={Link} href="/ChangePassword/" onClick={() => setAnchor(null)}>
          <ListItemIcon>
            <KeyRounded fontSize="small" />
          </ListItemIcon>
          {t("changePassword")}
        </MenuItem>
        <MenuItem onClick={() => void logout()}>
          <ListItemIcon>
            <LogoutRounded fontSize="small" />
          </ListItemIcon>
          {t("logout")}
        </MenuItem>
      </Menu>
    </>
  );
}

/** Header + side menu (a drawer on phones) around a logged-in page. */
export default function AppShell({ roles, children }: { roles?: AppRole[]; children: React.ReactNode }) {
  const { t } = useTranslation("common");
  const [drawerOpen, setDrawerOpen] = useState(false);

  return (
    <RoleGuard roles={roles}>
      <Box sx={{ minHeight: "100vh", bgcolor: color.surface.page }}>
        <Box
          component="header"
          sx={{
            position: "sticky",
            top: 0,
            zIndex: 10,
            height: { xs: 64, md: layout.headerHeight },
            display: "flex",
            alignItems: "center",
            gap: 1,
            px: 2,
            bgcolor: color.surface.card,
            borderBottom: `1px solid ${color.border.subtle}`,
          }}
        >
          <IconButton sx={{ display: { md: "none" } }} onClick={() => setDrawerOpen(true)} aria-label={t("openMenu")}>
            <MenuRounded />
          </IconButton>
          <Brand />
          <Box sx={{ flex: 1 }} />
          <NotificationBell />
          <LanguageSwitch />
          <UserMenu />
        </Box>

        <Drawer open={drawerOpen} onClose={() => setDrawerOpen(false)} sx={{ display: { md: "none" } }}>
          <Box sx={{ width: layout.sidebarWidth, pt: 2 }}>
            <Box sx={{ px: 2 }}>
              <Brand />
            </Box>
            <SideMenu onNavigate={() => setDrawerOpen(false)} />
          </Box>
        </Drawer>

        <Box sx={{ display: "flex" }}>
          <Box
            component="nav"
            sx={{
              display: { xs: "none", md: "block" },
              width: layout.sidebarWidth,
              flexShrink: 0,
              bgcolor: color.surface.card,
              borderRight: `1px solid ${color.border.subtle}`,
              minHeight: `calc(100vh - ${layout.headerHeight}px)`,
            }}
          >
            <SideMenu />
          </Box>
          <Box component="main" sx={{ flex: 1, minWidth: 0, p: { xs: 2, md: 3 } }}>
            <Box sx={{ maxWidth: layout.contentMax, mx: "auto", borderRadius: radiusPx.md }}>{children}</Box>
          </Box>
        </Box>
      </Box>
    </RoleGuard>
  );
}
