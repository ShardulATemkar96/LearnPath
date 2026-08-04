import { useEffect, useState } from "react";
import { useDispatch, useSelector } from "react-redux";
import {
  Alert, Box, Button, Chip, Paper, Skeleton,
  Snackbar, Stack, Typography,
} from "@mui/material";
import { CheckRounded, CloseRounded, GavelRounded } from "@mui/icons-material";
import { AppDispatch } from "../../redux/store";
import {
  fetchReports, resolveReportThunk, dismissReportThunk,
} from "../../redux/slices/communitySlice";
import {
  selectReports, selectReportsLoading, selectReportsError,
  selectReportsTotalPages, selectReportsCurrentPage, selectReportsTotalCount,
} from "../../redux/selectors/communitySelectors";
import PaginationBar from "../../components/common/PaginationBar/PaginationBar";
import EmptyState from "../../components/common/EmptyState/EmptyState";
import CommunityNav from "../../components/community/CommunityNav/CommunityNav";
import { useAuth } from "../../hooks/useAuth";
import { useNavigate } from "react-router-dom";
import { dateUtils } from "../../utils/dateUtils";

const CommunityModerationPage = () => {
  const dispatch   = useDispatch<AppDispatch>();
  const { isAdmin } = useAuth();
  const navigate   = useNavigate();
  const reports      = useSelector(selectReports);
  const loading      = useSelector(selectReportsLoading);
  const error        = useSelector(selectReportsError);
  const totalPages   = useSelector(selectReportsTotalPages);
  const page         = useSelector(selectReportsCurrentPage);
  const totalCount   = useSelector(selectReportsTotalCount);
  const [toast, setToast] = useState("");

  useEffect(() => {
    if (isAdmin) {
      dispatch(fetchReports({ page: 1, pageSize: 10 }));
    }
  }, [isAdmin, dispatch]);

  const handlePageChange = (p: number) => {
    dispatch(fetchReports({ page: p, pageSize: 10 }));
  };

  const handleResolve = async (reportId: number) => {
    const result = await dispatch(resolveReportThunk(reportId));
    if ((result as any).meta?.requestStatus === "fulfilled") {
      setToast("Report resolved.");
    }
  };

  const handleDismiss = async (reportId: number) => {
    const result = await dispatch(dismissReportThunk(reportId));
    if ((result as any).meta?.requestStatus === "fulfilled") {
      setToast("Report dismissed.");
    }
  };

  const openTarget = (targetType: string, targetId: number) => {
    if (targetType === "Post") {
      navigate(`/community/${targetId}`);
    }
  };

  return (
    <Box>
      <CommunityNav />

      {/* Header */}
      <Stack direction="row" alignItems="center"
        justifyContent="space-between" mb={4} flexWrap="wrap" gap={2}>
        <Box>
          <Typography variant="h4" fontWeight={700}>Moderation</Typography>
          <Typography variant="body2" color="text.secondary" mt={0.5}>
            Pending community reports.
          </Typography>
        </Box>
        <Chip
          icon={<GavelRounded />}
          label={`${totalCount} pending`}
          color="warning"
          variant="outlined"
          sx={{ fontWeight: 600 }}
        />
      </Stack>

      {error && (
        <Alert severity="error" sx={{ mb: 3, borderRadius: 2 }}>{error}</Alert>
      )}

      {/* Reports */}
      {loading ? (
        <Stack spacing={2}>
          {Array.from({ length: 5 }).map((_, i) => (
            <Skeleton key={i} variant="rounded"
              height={100} sx={{ borderRadius: 3 }} />
          ))}
        </Stack>
      ) : reports.length === 0 ? (
        <EmptyState
          title="No pending reports."
          description="The moderation queue is clear."
          icon={<GavelRounded sx={{ fontSize: 52 }} />}
        />
      ) : (
        <Stack spacing={2}>
          {reports.map((report) => (
            <Paper
              key={report.id}
              elevation={0}
              sx={{
                p: 2.5,
                borderRadius: 3,
                border: "1.5px solid",
                borderColor: "divider",
              }}
            >
              <Stack direction="row" alignItems="center"
                justifyContent="space-between" flexWrap="wrap" gap={2}>
                <Stack spacing={0.5} sx={{ flexGrow: 1, minWidth: 250 }}>
                  <Stack direction="row" alignItems="center" spacing={1} flexWrap="wrap">
                    <Chip
                      label={report.targetType}
                      size="small"
                      color={report.targetType === "Post" ? "primary" : "secondary"}
                      sx={{ fontWeight: 600, fontSize: "0.7rem" }}
                    />
                    <Typography variant="body2" fontWeight={600}>
                      Reported by {report.reportedByName}
                    </Typography>
                    <Typography variant="caption" color="text.secondary">
                      · {dateUtils.timeAgo(report.createdAt)}
                    </Typography>
                  </Stack>
                  <Typography variant="body1">{report.reason}</Typography>
                  {report.targetType === "Post" && (
                    <Button
                      size="small"
                      variant="text"
                      sx={{ alignSelf: "flex-start", textTransform: "none" }}
                      onClick={() => openTarget(report.targetType, report.targetId)}
                    >
                      View post
                    </Button>
                  )}
                </Stack>
                <Stack direction="row" spacing={1}>
                  <Button
                    size="small"
                    variant="contained"
                    startIcon={<CheckRounded />}
                    onClick={() => handleResolve(report.id)}
                    sx={{ borderRadius: 2, textTransform: "none" }}
                  >
                    Resolve
                  </Button>
                  <Button
                    size="small"
                    variant="outlined"
                    color="inherit"
                    startIcon={<CloseRounded />}
                    onClick={() => handleDismiss(report.id)}
                    sx={{ borderRadius: 2, textTransform: "none" }}
                  >
                    Dismiss
                  </Button>
                </Stack>
              </Stack>
            </Paper>
          ))}
        </Stack>
      )}

      {!isAdmin && (
        <Alert severity="warning" sx={{ mt: 3, borderRadius: 2 }}>
          You don't have permission to view this page.
        </Alert>
      )}

      <Box sx={{ mt: 3 }}>
        <PaginationBar
          page={page}
          totalPages={totalPages}
          onChange={handlePageChange}
        />
      </Box>

      <Snackbar
        open={!!toast}
        autoHideDuration={3000}
        onClose={() => setToast("")}
        message={toast}
      />
    </Box>
  );
};

export default CommunityModerationPage;
