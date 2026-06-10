import { Box } from "@mui/material";
import { Outlet, useLocation } from "react-router-dom";
import Sidebar from "../components/dashboard/Sidebar/Sidebar";
import Topbar from "../components/dashboard/Topbar/Topbar";
import { ROUTES } from "../constants/routes";

const PAGE_TITLES: Record<string, string> = {
  [ROUTES.DASHBOARD]:      "Dashboard",
  [ROUTES.LEARNING_PATHS]: "Learning Paths",
  [ROUTES.CLASSROOM]:      "Classrooms",
  [ROUTES.ANALYTICS]:      "Analytics",
  [ROUTES.COMMUNITY]:      "Community",
  [ROUTES.CERTIFICATES]:   "Certificates",
  [ROUTES.PROFILE]:        "Profile",
  [ROUTES.SETTINGS]:       "Settings",
  [ROUTES.ADMIN]:          "Admin Panel",
};

const DashboardLayout = () => {
  const { pathname } = useLocation();
  const pageTitle = PAGE_TITLES[pathname];

  return (
    <Box sx={{ display: "flex", minHeight: "100vh", bgcolor: "background.default" }}>
      <Sidebar />
      <Box sx={{ display: "flex", flexDirection: "column", flexGrow: 1, minWidth: 0 }}>
        <Topbar pageTitle={pageTitle} />
        <Box component="main" sx={{ flexGrow: 1, p: 3 }}>
          <Outlet />
        </Box>
      </Box>
    </Box>
  );
};

export default DashboardLayout;
