import { Box } from "@mui/material";
import { Outlet } from "react-router-dom";

const SIDEBAR_WIDTH = 260;

const DashboardLayout = () => (
  <Box sx={{ display: "flex", minHeight: "100vh", bgcolor: "background.default" }}>
    {/* Sidebar placeholder — generated in next phase */}
    <Box sx={{ width: SIDEBAR_WIDTH, flexShrink: 0 }} />
    <Box component="main" sx={{ flexGrow: 1, p: 3, minWidth: 0 }}>
      <Outlet />
    </Box>
  </Box>
);

export default DashboardLayout;