import { Box, Card, CardContent, CardHeader, Divider, Typography } from "@mui/material";
import { PieChart, Pie, Cell, Tooltip, ResponsiveContainer, Legend } from "recharts";
import { ModuleTypeBreakdown } from "../../../types/analytics.types";

const COLORS = ["#6C63FF", "#FF6584", "#22C55E", "#F59E0B", "#3B82F6"];

interface ModuleTypeChartProps {
  data: ModuleTypeBreakdown[];
}

const ModuleTypeChart = ({ data }: ModuleTypeChartProps) => (
  <Card sx={{ borderRadius: 4, boxShadow: "0 4px 24px rgba(0,0,0,0.07)", height: "100%" }}>
    <CardHeader
      title={<Typography variant="h6" fontWeight={600}>Content Breakdown</Typography>}
      subheader={<Typography variant="caption" color="text.secondary">Modules completed by type</Typography>}
    />
    <Divider />
    <CardContent>
      {data.length === 0 ? (
        <Box sx={{ py: 4, textAlign: "center" }}>
          <Typography variant="body2" color="text.secondary">No data yet.</Typography>
        </Box>
      ) : (
        <ResponsiveContainer width="100%" height={220}>
          <PieChart>
            <Pie
              data={data}
              cx="50%"
              cy="50%"
              innerRadius={55}
              outerRadius={85}
              paddingAngle={4}
              dataKey="count"
              nameKey="contentType"
            >
              {data.map((_, idx) => (
                <Cell key={idx} fill={COLORS[idx % COLORS.length]} />
              ))}
            </Pie>

            <Tooltip
              formatter={(value, name) => [`${value} modules`, name]}
              contentStyle={{
                borderRadius: 12,
                fontSize: 12,
                border: "none",
                boxShadow: "0 4px 16px rgba(0,0,0,0.1)",
              }}
            />

            <Legend
              iconType="circle"
              iconSize={8}
              formatter={(value) => (
                <span style={{ fontSize: 12, color: "#6B7280" }}>{value}</span>
              )}
            />
          </PieChart>
        </ResponsiveContainer>
      )}
    </CardContent>
  </Card>
);

export default ModuleTypeChart;
