import { Box, CircularProgress, Typography } from "@mui/material";
import { RouteRounded } from "@mui/icons-material";

const SplashScreen = () => (
  <Box
    sx={{
      minHeight: "100vh",
      display: "flex",
      flexDirection: "column",
      alignItems: "center",
      justifyContent: "center",
      gap: 3,
      background: "linear-gradient(135deg, #6C63FF08 0%, #FF658408 100%)",
    }}
  >
    <Box
      sx={{
        width: 64,
        height: 64,
        borderRadius: 3,
        background: "linear-gradient(135deg, #6C63FF, #9D97FF)",
        display: "flex",
        alignItems: "center",
        justifyContent: "center",
        boxShadow: "0 8px 32px rgba(108,99,255,0.35)",
      }}
    >
      <RouteRounded sx={{ color: "#fff", fontSize: 36 }} />
    </Box>

    <Typography
      variant="h5"
      fontWeight={700}
      sx={{
        background: "linear-gradient(135deg, #6C63FF, #9D97FF)",
        WebkitBackgroundClip: "text",
        WebkitTextFillColor: "transparent",
      }}
    >
      LearnPath
    </Typography>

    <CircularProgress
      size={28}
      sx={{ color: "primary.main", opacity: 0.7 }}
    />
  </Box>
);

export default SplashScreen;
