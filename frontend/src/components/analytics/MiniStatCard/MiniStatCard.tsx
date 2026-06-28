import { Box, Card, CardContent, Stack, Typography } from "@mui/material";

interface MiniStatCardProps {
  label: string;
  value: string | number;
  icon: React.ReactNode;
  color: string;
}

const MiniStatCard = ({ label, value, icon, color }: MiniStatCardProps) => (
  <Card sx={{
    borderRadius: 3,
    boxShadow: "0 2px 12px rgba(0,0,0,0.06)",
    transition: "transform 0.2s",
    "&:hover": { transform: "translateY(-2px)" },
  }}>
    <CardContent sx={{ p: "20px !important" }}>
      <Stack direction="row" alignItems="center" spacing={2}>
        <Box sx={{
          width: 44,
          height: 44,
          borderRadius: 2,
          display: "flex",
          alignItems: "center",
          justifyContent: "center",
          bgcolor: `${color}18`,
          "& svg": { color, fontSize: 22 },
        }}>
          {icon}
        </Box>
        <Box>
          <Typography variant="h5" fontWeight={700} lineHeight={1.2}>
            {value}
          </Typography>
          <Typography variant="caption" color="text.secondary" fontWeight={500}>
            {label}
          </Typography>
        </Box>
      </Stack>
    </CardContent>
  </Card>
);

export default MiniStatCard;
