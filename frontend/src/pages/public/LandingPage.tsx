import { Box, Typography, Button, Stack } from "@mui/material";
import { Link as RouterLink } from "react-router-dom";
import { ROUTES } from "../../constants/routes";

const LandingPage = () => (
  <Box
    sx={{
      minHeight: "100vh",
      display: "flex",
      alignItems: "center",
      justifyContent: "center",
      background: "linear-gradient(135deg, #6C63FF10 0%, #FF658410 100%)",
      textAlign: "center",
      px: 3,
    }}
  >
    <Stack spacing={3} maxWidth={600}>
      <Typography variant="h2" fontWeight={700} color="primary.main">
        LearnPath
      </Typography>
      <Typography variant="h5" color="text.secondary">
        Graph-based personalized learning — built for the future.
      </Typography>
      <Stack direction="row" spacing={2} justifyContent="center">
        <Button
          component={RouterLink}
          to={ROUTES.REGISTER}
          variant="contained"
          size="large"
          sx={{
            background: "linear-gradient(135deg, #6C63FF 0%, #9D97FF 100%)",
          }}
        >
          Get Started Free
        </Button>
        <Button component={RouterLink} to={ROUTES.LOGIN} variant="outlined" size="large">
          Sign In
        </Button>
      </Stack>
    </Stack>
  </Box>
);

export default LandingPage;
