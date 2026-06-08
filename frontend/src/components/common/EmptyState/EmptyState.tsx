import { Box, Typography } from "@mui/material";

interface EmptyStateProps {
  title: string;
  description?: string;
  icon?: React.ReactNode;
}

const EmptyState = ({ title, description, icon }: EmptyStateProps) => (
  <Box sx={{ textAlign: "center", py: 8 }}>
    {icon && <Box sx={{ mb: 2, color: "text.secondary" }}>{icon}</Box>}
    <Typography variant="h6" color="text.primary" gutterBottom>{title}</Typography>
    {description && (
      <Typography variant="body2" color="text.secondary">{description}</Typography>
    )}
  </Box>
);

export default EmptyState;