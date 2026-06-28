import { useEffect, useState } from "react";
import {
  Alert, Box, Card, CardContent, Chip,
  Grid, Skeleton, Stack, Typography,
} from "@mui/material";
import {
  WorkspacePremiumRounded, RouteRounded, CalendarTodayRounded,
} from "@mui/icons-material";
import {
  certificateService, Certificate,
} from "../../services/certificateService";
import EmptyState from "../../components/common/EmptyState/EmptyState";

const CertificateCard = ({ cert }: { cert: Certificate }) => (
  <Card sx={{
    borderRadius: 4,
    boxShadow: "0 4px 24px rgba(0,0,0,0.07)",
    overflow: "hidden",
    transition: "transform 0.2s, box-shadow 0.2s",
    "&:hover": {
      transform: "translateY(-3px)",
      boxShadow: "0 10px 36px rgba(245,158,11,0.15)",
    },
  }}>
    <Box sx={{ height: 6, background: "linear-gradient(90deg, #F59E0B, #FCD34D)" }} />
    <CardContent sx={{ p: 3 }}>
      <Stack spacing={2}>
        <Stack direction="row" alignItems="flex-start" spacing={1.5}>
          <Box sx={{
            width: 48, height: 48, borderRadius: 2.5, flexShrink: 0,
            display: "flex", alignItems: "center", justifyContent: "center",
            background: "linear-gradient(135deg, #F59E0B22, #FCD34D22)",
          }}>
            <WorkspacePremiumRounded sx={{ color: "#F59E0B", fontSize: 26 }} />
          </Box>
          <Box sx={{ flexGrow: 1, minWidth: 0 }}>
            <Typography variant="h6" fontWeight={700} noWrap>
              {cert.learningPathTitle}
            </Typography>
            <Chip
              label="Completed"
              size="small"
              color="success"
              sx={{ mt: 0.5, fontWeight: 600, fontSize: "0.7rem" }}
            />
          </Box>
        </Stack>

        <Stack spacing={1}>
          <Stack direction="row" alignItems="center" spacing={0.75}>
            <RouteRounded sx={{ fontSize: 15, color: "text.secondary" }} />
            <Typography variant="caption" color="text.secondary">
              Learning Path ID: {cert.learningPathId}
            </Typography>
          </Stack>
          <Stack direction="row" alignItems="center" spacing={0.75}>
            <CalendarTodayRounded sx={{ fontSize: 15, color: "text.secondary" }} />
            <Typography variant="caption" color="text.secondary">
              Issued: {new Date(cert.issuedAt).toLocaleDateString("en-US", {
                year: "numeric", month: "long", day: "numeric",
              })}
            </Typography>
          </Stack>
        </Stack>
      </Stack>
    </CardContent>
  </Card>
);

const CertificatesPage = () => {
  const [certs,   setCerts]   = useState<Certificate[]>([]);
  const [loading, setLoading] = useState(true);
  const [error,   setError]   = useState("");

  useEffect(() => {
    (async () => {
      try {
        const data = await certificateService.getMy();
        setCerts(data);
      } catch { setError("Failed to load certificates."); }
      finally { setLoading(false); }
    })();
  }, []);

  return (
    <Box>
      <Stack spacing={0.5} mb={4}>
        <Typography variant="h4" fontWeight={700}>Certificates</Typography>
        <Typography variant="body2" color="text.secondary">
          Your earned completion certificates.
        </Typography>
      </Stack>

      {error && <Alert severity="error" sx={{ mb: 3, borderRadius: 2 }}>{error}</Alert>}

      {loading ? (
        <Grid container spacing={3}>
          {Array.from({ length: 4 }).map((_, i) => (
            <Grid item xs={12} sm={6} lg={4} key={i}>
              <Skeleton variant="rounded" height={180} sx={{ borderRadius: 4 }} />
            </Grid>
          ))}
        </Grid>
      ) : certs.length === 0 ? (
        <EmptyState
          title="No certificates yet."
          description="Complete all modules in a learning path to earn your certificate."
          icon={<WorkspacePremiumRounded sx={{ fontSize: 52 }} />}
        />
      ) : (
        <Grid container spacing={3}>
          {certs.map((c) => (
            <Grid item xs={12} sm={6} lg={4} key={c.id}>
              <CertificateCard cert={c} />
            </Grid>
          ))}
        </Grid>
      )}
    </Box>
  );
};

export default CertificatesPage;
