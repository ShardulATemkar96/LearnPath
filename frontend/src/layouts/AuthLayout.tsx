import { Box } from "@mui/material";
import { Outlet } from "react-router-dom";

const AuthLayout = () => (
  <Box
    sx={{
      minHeight: "100vh",
      display: "flex",
      alignItems: "center",
      justifyContent: "center",
      background: "linear-gradient(135deg, #6C63FF15 0%, #FF658415 100%)",
      p: 2,
    }}
  >
    <Outlet />
  </Box>
);

export default AuthLayout;