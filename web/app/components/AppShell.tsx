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
import InsightsRounded from "@mui/icons-material/InsightsRounded";
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
import { color, hiphr, layout, nav, typeScale } from "@/theme/tokens";
import LanguageSwitch from "./LanguageSwitch";
import NotificationBell from "./NotificationBell";
import Link from "./Link";
import RoleGuard from "./RoleGuard";

const ICONS: Record<MenuEntry["icon"], typeof AssignmentIndRounded> = {
  dashboard: InsightsRounded,
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

/** Logo tile + name, like the HIPHR header. onHeader = white text on the orange gradient. */
function Brand({ onHeader = false }: { onHeader?: boolean }) {
  const { t } = useTranslation("common");
  return (
    <Stack direction="row" spacing={3} sx={{ alignItems: "center", px: 1, minWidth: 0 }}>
      <Box
        sx={{
          width: 40,
          height: 40,
          flexShrink: 0,
          borderRadius: "13px",
          display: "grid",
          placeItems: "center",
          fontWeight: 700,
          fontSize: 15,
          letterSpacing: ".5px",
          color: onHeader ? "#fff" : hiphr.orange600,
          bgcolor: onHeader ? "rgba(255, 255, 255, .22)" : hiphr.orange100,
          boxShadow: onHeader ? "inset 0 0 0 2px rgba(255, 255, 255, .35)" : "none",
        }}
      >
        SW
      </Box>
      <Stack sx={{ minWidth: 0 }}>
        <Typography sx={{ fontSize: 18, lineHeight: "24px", fontWeight: 700, color: onHeader ? "#fff" : color.text.primary }}>{t("appName")}</Typography>
        <Typography noWrap sx={{ ...typeScale.caption, color: onHeader ? "rgba(255, 255, 255, .9)" : color.text.secondary }}>
          {t("appTagline")}
        </Typography>
      </Stack>
    </Stack>
  );
}

function SideMenu({ onNavigate }: { onNavigate?: () => void }) {
  const { t } = useTranslation("navigation");
  const pathname = usePathname();
  const user = useAuthStore((s) => s.user);
  const counts = useTicketCountsQuery();

  return (
    <Stack spacing={`${nav.gap}px`} sx={{ p: 3 }}>
      {MENU.filter((entry) => hasAnyRole(user, entry.roles)).map((entry, index, entries) => {
        const Icon = ICONS[entry.icon];
        const startsAdmin = entry.section === "admin" && entries[index - 1]?.section !== "admin";
        const selected = pathname === entry.route;
        const badge = entry.badge ? (counts.data?.[entry.badge] ?? 0) : 0;
        return (
          <Fragment key={entry.route}>
            {startsAdmin ? (
              <Typography sx={{ ...typeScale.label, color: color.text.tertiary, px: 3, pt: 4, pb: 1, letterSpacing: ".6px", textTransform: "uppercase" }}>
                {t("adminSection")}
              </Typography>
            ) : null}
            <ListItemButton component={Link} href={entry.route} selected={selected} onClick={onNavigate} sx={{ px: 3 }}>
              <ListItemIcon sx={{ minWidth: 34, color: "inherit", "& svg": { fontSize: 20 } }}>
                <Icon />
              </ListItemIcon>
              <ListItemText primary={t(entry.labelKey)} slotProps={{ primary: { sx: { ...typeScale.body, fontWeight: selected ? 600 : 500 } } }} />
              {badge > 0 ? <Chip label={badge} size="small" color="primary" sx={{ minWidth: 26, fontWeight: 700 }} /> : null}
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
          <Typography sx={typeScale.bodyStrong}>{user?.DisplayName}</Typography>
          <Typography sx={{ ...typeScale.caption, color: color.text.secondary }}>{user?.DepartmentName}</Typography>
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
            height: layout.headerHeight,
            display: "flex",
            alignItems: "center",
            gap: 2,
            px: { xs: 2, md: 5 },
            background: hiphr.headerGradient,
            color: "#fff",
            overflow: "hidden",
            boxShadow: "0 4px 16px rgba(240, 100, 0, .18)",
            // the soft circle of the HIPHR header
            "&::after": {
              content: '""',
              position: "absolute",
              right: -80,
              top: -150,
              width: 300,
              height: 300,
              borderRadius: "50%",
              bgcolor: "rgba(255, 255, 255, .14)",
              pointerEvents: "none",
            },
            "& > *": { position: "relative", zIndex: 1 },
            "& .MuiIconButton-root": { color: "#fff", "&:hover": { bgcolor: "rgba(255, 255, 255, .18)", color: "#fff" } },
          }}
        >
          <IconButton sx={{ display: { md: "none" } }} onClick={() => setDrawerOpen(true)} aria-label={t("openMenu")}>
            <MenuRounded />
          </IconButton>
          <Brand onHeader />
          <Box sx={{ flex: 1 }} />
          <NotificationBell />
          <Box sx={{ display: { xs: "none", md: "block" } }}>
            <LanguageSwitch onHeader />
          </Box>
          <UserMenu />
        </Box>

        <Drawer open={drawerOpen} onClose={() => setDrawerOpen(false)} sx={{ display: { md: "none" } }}>
          <Box sx={{ width: layout.sidebarWidth, pt: 4 }}>
            <Box sx={{ px: 3 }}>
              <Brand />
            </Box>
            <Box sx={{ px: 4, pt: 4 }}>
              <LanguageSwitch />
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
              boxShadow: "1px 0 0 " + color.border.subtle,
              minHeight: `calc(100vh - ${layout.headerHeight}px)`,
            }}
          >
            <Box sx={{ position: "sticky", top: layout.headerHeight }}>
              <SideMenu />
            </Box>
          </Box>
          <Box component="main" sx={{ flex: 1, minWidth: 0, px: { xs: 4, md: 7 }, py: { xs: 5, md: 6 } }}>
            <Box sx={{ maxWidth: layout.contentMax, mx: "auto" }}>{children}</Box>
          </Box>
        </Box>
      </Box>
    </RoleGuard>
  );
}
