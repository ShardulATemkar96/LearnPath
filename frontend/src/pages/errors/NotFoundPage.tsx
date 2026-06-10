import { Box, Button, Typography } from "@mui/material";
import { Link as RouterLink } from "react-router-dom";
import { ROUTES } from "../../constants/routes";

const NotFoundPage = () => (
  <Box sx={{ minHeight: "100vh", display: "flex", flexDirection: "column", alignItems: "center", justifyContent: "center", gap: 2 }}>
    <Typography variant="h1" fontWeight={700} color="primary.main">404</Typography>
    <Typography variant="h5" color="text.secondary">Page not found.</Typography>
    <Button component={RouterLink} to={ROUTES.HOME} variant="contained">
      Go Home
    </Button>
  </Box>
);

export default NotFoundPage;
