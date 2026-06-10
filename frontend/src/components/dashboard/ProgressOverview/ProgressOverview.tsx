import {
  Box, Card, CardContent, CardHeader,
  Divider, LinearProgress, Stack, Typography,
} from "@mui/material";

interface PathProgress {
  id: number;
  title: string;
  completedModules: number;
  totalModules: number;
}

interface ProgressOverviewProps {
  paths: PathProgress[];
}

const ProgressOverview = ({ paths }: ProgressOverviewProps) => (
  <Card sx={{ borderRadius: 4, boxShadow: "0 4px 24px rgba(0,0,0,0.07)", height: "100%" }}>
    <CardHeader
      title={
        <Typography variant="h6" fontWeight={600}>
          Path Progress
        </Typography>
      }
    />
    <Divider />
    <CardContent>
      {paths.length === 0 ? (
        <Box sx={{ py: 4, textAlign: "center" }}>
          <Typography variant="body2" color="text.secondary">
            No paths enrolled yet.
          </Typography>
        </Box>
      ) : (
        <Stack spacing={3}>
          {paths.map((path) => {
            const pct = path.totalModules > 0
              ? Math.round((path.completedModules / path.totalModules) * 100)
              : 0;

            return (
              <Box key={path.id}>
                <Stack direction="row" justifyContent="space-between" mb={0.75}>
                  <Typography variant="body2" fontWeight={500} noWrap sx={{ maxWidth: "70%" }}>
                    {path.title}
                  </Typography>
                  <Typography variant="body2" fontWeight={600} color="primary.main">
                    {pct}%
                  </Typography>
                </Stack>
                <LinearProgress
                  variant="determinate"
                  value={pct}
                  sx={{
                    height: 7,
                    borderRadius: 4,
                    bgcolor: "rgba(108,99,255,0.1)",
                    "& .MuiLinearProgress-bar": {
                      borderRadius: 4,
                      background: "linear-gradient(90deg, #6C63FF, #9D97FF)",
                    },
                  }}
                />
                <Typography variant="caption" color="text.secondary">
                  {path.completedModules} / {path.totalModules} modules
                </Typography>
              </Box>
            );
          })}
        </Stack>
      )}
    </CardContent>
  </Card>
);

export default ProgressOverview;
