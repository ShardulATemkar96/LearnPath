import { Box } from "@mui/material";
import { Outlet } from "react-router-dom";

const AdminLayout = () => (
  <Box sx={{ display: "flex", minHeight: "100vh", bgcolor: "background.default" }}>
    <Box component="main" sx={{ flexGrow: 1, p: 3 }}>
      <Outlet />
    </Box>
  </Box>
);

export default AdminLayout;