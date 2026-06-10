import { Box, Card, CardContent, Stack, Typography } from "@mui/material";
import { TrendingUpRounded, TrendingDownRounded } from "@mui/icons-material";

interface StatCardProps {
  title: string;
  value: string | number;
  subtitle?: string;
  icon: React.ReactNode;
  gradient: string;
  trend?: { value: number; label: string };
}

const StatCard = ({ title, value, subtitle, icon, gradient, trend }: StatCardProps) => {
  const isPositive = (trend?.value ?? 0) >= 0;

  return (
    <Card
      sx={{
        borderRadius: 4,
        overflow: "hidden",
        position: "relative",
        boxShadow: "0 4px 24px rgba(0,0,0,0.07)",
        transition: "transform 0.2s ease, box-shadow 0.2s ease",
        "&:hover": {
          transform: "translateY(-2px)",
          boxShadow: "0 8px 32px rgba(0,0,0,0.12)",
        },
      }}
    >
      {/* Gradient accent bar */}
      <Box sx={{ height: 4, background: gradient }} />

      <CardContent sx={{ p: 3 }}>
        <Stack direction="row" justifyContent="space-between" alignItems="flex-start">
          <Box>
            <Typography variant="body2" color="text.secondary" fontWeight={500} mb={0.5}>
              {title}
            </Typography>
            <Typography variant="h4" fontWeight={700} color="text.primary">
              {value}
            </Typography>
            {subtitle && (
              <Typography variant="caption" color="text.secondary">
                {subtitle}
              </Typography>
            )}
            {trend && (
              <Stack direction="row" alignItems="center" spacing={0.5} mt={1}>
                {isPositive
                  ? <TrendingUpRounded sx={{ fontSize: 16, color: "success.main" }} />
                  : <TrendingDownRounded sx={{ fontSize: 16, color: "error.main" }} />}
                <Typography
                  variant="caption"
                  fontWeight={600}
                  color={isPositive ? "success.main" : "error.main"}
                >
                  {isPositive ? "+" : ""}{trend.value}% {trend.label}
                </Typography>
              </Stack>
            )}
          </Box>

          {/* Icon bubble */}
          <Box
            sx={{
              width: 52,
              height: 52,
              borderRadius: 3,
              background: gradient,
              display: "flex",
              alignItems: "center",
              justifyContent: "center",
              opacity: 0.9,
              "& svg": { color: "#fff", fontSize: 26 },
            }}
          >
            {icon}
          </Box>
        </Stack>
      </CardContent>
    </Card>
  );
};

export default StatCard;
