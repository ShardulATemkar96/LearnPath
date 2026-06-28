import { Box, Card, CardContent, CardHeader, Divider, Typography } from "@mui/material";
import {
  BarChart, Bar, XAxis, YAxis, CartesianGrid,
  Tooltip, ResponsiveContainer,
} from "recharts";
import { WeeklyActivity } from "../../../types/analytics.types";

interface WeeklyBarChartProps {
  data: WeeklyActivity[];
}

const CustomTooltip = ({ active, payload, label }: any) => {
  if (!active || !payload?.length) return null;

  return (
    <Box sx={{
      bgcolor: "background.paper",
      p: 1.5,
      borderRadius: 2,
      boxShadow: "0 4px 16px rgba(0,0,0,0.1)",
      border: "1px solid",
      borderColor: "divider",
    }}>
      <Typography variant="caption" fontWeight={700}>{label}</Typography>
      <Typography variant="body2" color="primary.main" fontWeight={600}>
        {payload[0].value} modules
      </Typography>
    </Box>
  );
};

const WeeklyBarChart = ({ data }: WeeklyBarChartProps) => (
  <Card sx={{ borderRadius: 4, boxShadow: "0 4px 24px rgba(0,0,0,0.07)", height: "100%" }}>
    <CardHeader
      title={<Typography variant="h6" fontWeight={600}>Weekly Activity</Typography>}
      subheader={<Typography variant="caption" color="text.secondary">Modules completed per day</Typography>}
    />
    <Divider />
    <CardContent>
      <ResponsiveContainer width="100%" height={220}>
        <BarChart data={data} margin={{ top: 8, right: 8, left: -20, bottom: 0 }}>
          <CartesianGrid strokeDasharray="3 3" stroke="rgba(0,0,0,0.06)" vertical={false} />
          <XAxis dataKey="day" tick={{ fontSize: 12, fill: "#6B7280" }} axisLine={false} tickLine={false} />
          <YAxis allowDecimals={false} tick={{ fontSize: 12, fill: "#6B7280" }} axisLine={false} tickLine={false} />
          <Tooltip content={<CustomTooltip />} cursor={{ fill: "rgba(108,99,255,0.06)" }} />
          <Bar dataKey="modulesCompleted" radius={[6, 6, 0, 0]} fill="url(#barGradient)" />
          <defs>
            <linearGradient id="barGradient" x1="0" y1="0" x2="0" y2="1">
              <stop offset="0%" stopColor="#6C63FF" stopOpacity={1} />
              <stop offset="100%" stopColor="#9D97FF" stopOpacity={0.7} />
            </linearGradient>
          </defs>
        </BarChart>
      </ResponsiveContainer>
    </CardContent>
  </Card>
);

export default WeeklyBarChart;
