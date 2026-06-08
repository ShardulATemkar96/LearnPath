import { Box, CircularProgress } from "@mui/material";

interface LoaderProps {
  fullScreen?: boolean;
}

const Loader = ({ fullScreen = false }: LoaderProps) => (
  <Box
    sx={{
      display: "flex",
      alignItems: "center",
      justifyContent: "center",
      ...(fullScreen && { minHeight: "100vh" }),
      ...(!fullScreen && { p: 4 }),
    }}
  >
    <CircularProgress sx={{ color: "primary.main" }} />
  </Box>
);

export default Loader;