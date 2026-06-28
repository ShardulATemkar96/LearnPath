import {
  Box, Card, CardContent, CardHeader,
  Divider, LinearProgress, Stack, Typography,
} from "@mui/material";
import { PathCompletion } from "../../../types/analytics.types";

interface PathProgressChartProps {
  data: PathCompletion[];
}

const PathProgressChart = ({ data }: PathProgressChartProps) => (
  <Card sx={{ borderRadius: 4, boxShadow: "0 4px 24px rgba(0,0,0,0.07)", height: "100%" }}>
    <CardHeader
      title={<Typography variant="h6" fontWeight={600}>Path Progress</Typography>}
      subheader={<Typography variant="caption" color="text.secondary">Completion across enrolled paths</Typography>}
    />
    <Divider />
    <CardContent>
      {data.length === 0 ? (
        <Box sx={{ py: 4, textAlign: "center" }}>
          <Typography variant="body2" color="text.secondary">No path data yet.</Typography>
        </Box>
      ) : (
        <Stack spacing={3} mt={1}>
          {data.map((path, idx) => (
            <Box key={idx}>
              <Stack direction="row" justifyContent="space-between" mb={0.75}>
                <Typography variant="body2" fontWeight={500} noWrap sx={{ maxWidth: "68%" }}>
                  {path.title}
                </Typography>
                <Typography variant="body2" fontWeight={700} color="primary.main">
                  {path.percent}%
                </Typography>
              </Stack>

              <LinearProgress
                variant="determinate"
                value={path.percent}
                sx={{
                  height: 8,
                  borderRadius: 4,
                  bgcolor: "rgba(108,99,255,0.08)",
                  "& .MuiLinearProgress-bar": {
                    borderRadius: 4,
                    background: path.percent === 100
                      ? "linear-gradient(90deg, #22C55E, #4ADE80)"
                      : "linear-gradient(90deg, #6C63FF, #9D97FF)",
                  },
                }}
              />

              <Typography variant="caption" color="text.secondary">
                {path.completedModules} / {path.totalModules} modules
              </Typography>
            </Box>
          ))}
        </Stack>
      )}
    </CardContent>
  </Card>
);

export default PathProgressChart;
