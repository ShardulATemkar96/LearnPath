import { useState } from "react";
import {
  Box, Button, Chip, Stack, TextField, Typography, IconButton,
} from "@mui/material";
import {
  CalendarTodayRounded, CheckCircleRounded, UploadRounded,
  CloseRounded, GradingRounded, AutoGraphRounded,
} from "@mui/icons-material";
import { Assignment } from "../../../types/classroom.types";
import { classroomService } from "../../../services/classroomService";

interface AssignmentCardProps {
  assignment: Assignment;
  classroomId: number;
  isInstructor: boolean;
  onRefresh: () => void;
  onViewSubmissions?: (assignment: Assignment) => void;
}

const AssignmentCard = ({
  assignment, classroomId, isInstructor, onRefresh, onViewSubmissions,
}: AssignmentCardProps) => {
  const [submitUrl, setSubmitUrl] = useState(assignment.myContentUrl || "");
  const [submitting, setSubmitting] = useState(false);
  const [error, setError] = useState("");

  const isOverdue = new Date(assignment.dueDate) < new Date();
  const status = assignment.mySubmissionStatus;

  const handleSubmit = async () => {
    if (!submitUrl.trim()) { setError("URL is required."); return; }
    setSubmitting(true); setError("");
    try {
      await classroomService.submit(classroomId, assignment.id, submitUrl);
      onRefresh();
    } catch (e: any) {
      setError(e.response?.data?.message ?? "Submission failed.");
    } finally { setSubmitting(false); }
  };

  const handleCancel = async () => {
    if (!window.confirm("Cancel your submission?")) return;
    try {
      await classroomService.deleteSubmission(classroomId, assignment.id);
      onRefresh();
    } catch { setError("Failed to cancel."); }
  };

  const getBorderColor = () => {
    if (!status) return "divider";
    if (status === "Completed") return "success.main";
    if (status === "Resubmit") return "warning.main";
    if (status === "Pending" || status === "Verified") return "info.main";
    return "divider";
  };

  const getStatusChip = () => {
    if (!status) return null;
    if (status === "Pending")
      return <Chip label="Awaiting Review" size="small" color="info" variant="outlined" />;
    if (status === "Verified")
      return <Chip label="Under Review" size="small" color="default" variant="outlined" />;
    if (status === "Completed")
      return <Chip icon={<CheckCircleRounded sx={{ fontSize: "14px !important" }} />}
        label="Completed" size="small" color="success" sx={{ fontWeight: 600 }} />;
    if (status === "Resubmit")
      return <Chip label="Resubmit Required" size="small" color="warning" variant="outlined" />;
    return null;
  };

  return (
    <Box sx={{
      p: 3, borderRadius: 3,
      border: "2px solid",
      borderColor: getBorderColor(),
      bgcolor: "background.paper",
      transition: "border-color 0.2s, box-shadow 0.2s",
      "&:hover": { boxShadow: "0 4px 16px rgba(0,0,0,0.07)" },
    }}>
      <Stack spacing={1.5}>
        <Stack direction="row" alignItems="flex-start" justifyContent="space-between">
          <Typography variant="body1" fontWeight={700} sx={{ flexGrow: 1, pr: 1 }}>
            {assignment.title}
          </Typography>
          <Stack direction="row" spacing={1} alignItems="center">
            {getStatusChip()}
            {/* Show low grade on right side for Resubmit */}
            {status === "Resubmit" && assignment.myGrade !== undefined && (
              <Chip label={`${assignment.myGrade}/10`} size="small" color="error" variant="filled" sx={{ fontWeight: 700 }} />
            )}
            {status === "Completed" && assignment.myGrade !== undefined && (
              <Chip label={`${assignment.myGrade}/10`} size="small"
                color={assignment.myGrade >= 5 ? "success" : "error"} variant="filled" sx={{ fontWeight: 700 }} />
            )}
          </Stack>
        </Stack>

        <Typography variant="body2" color="text.secondary">
          {assignment.description}
        </Typography>

        {status === "Completed" && assignment.myGrade !== undefined && (
          <Box sx={{ p: 1.5, bgcolor: "#f5f5f5", borderRadius: 2 }}>
            <Typography variant="body2" fontWeight={600}>Grade: {assignment.myGrade}/10</Typography>
            {assignment.myFeedback && (
              <Typography variant="body2" color="text.secondary">Remarks: {assignment.myFeedback}</Typography>
            )}
          </Box>
        )}

        {!isInstructor && !status && !isOverdue && (
          <Stack spacing={1}>
            <TextField size="small" label="Submission URL" fullWidth value={submitUrl}
              onChange={(e) => setSubmitUrl(e.target.value)}
              placeholder="https://github.com/your-repo" />
            {error && <Typography variant="caption" color="error">{error}</Typography>}
            <Button variant="contained" size="small" startIcon={<UploadRounded />}
              onClick={handleSubmit} disabled={submitting}
              sx={{ alignSelf: "flex-start", borderRadius: 2,
                background: "linear-gradient(135deg, #6C63FF, #9D97FF)" }}>
              {submitting ? "Submitting..." : "Submit"}
            </Button>
          </Stack>
        )}

        {!isInstructor && status === "Resubmit" && (
          <Stack spacing={1}>
            <Typography variant="caption" color="warning.main" fontWeight={600}>
              Score too low. You can resubmit.
            </Typography>
            <TextField size="small" label="New Submission URL" fullWidth value={submitUrl}
              onChange={(e) => setSubmitUrl(e.target.value)}
              placeholder="https://github.com/your-repo" />
            {error && <Typography variant="caption" color="error">{error}</Typography>}
            <Button variant="contained" size="small" startIcon={<UploadRounded />}
              onClick={handleSubmit} disabled={submitting}
              sx={{ alignSelf: "flex-start", borderRadius: 2,
                background: "linear-gradient(135deg, #6C63FF, #9D97FF)" }}>
              {submitting ? "Resubmitting..." : "Resubmit"}
            </Button>
          </Stack>
        )}

        {!isInstructor && status === "Pending" && (
          <Stack direction="row" spacing={1}>
            <IconButton size="small" color="error" onClick={handleCancel} title="Cancel Submission">
              <CloseRounded fontSize="small" />
            </IconButton>
            <Typography variant="caption" color="text.secondary" sx={{ alignSelf: "center" }}>
              Cancel submission
            </Typography>
          </Stack>
        )}

        <Stack direction="row" alignItems="center" justifyContent="space-between" flexWrap="wrap" gap={1}>
          <Stack direction="row" alignItems="center" spacing={0.75}>
            <CalendarTodayRounded sx={{ fontSize: 15, color: isOverdue ? "error.main" : "text.secondary" }} />
            <Typography variant="caption" fontWeight={500}
              color={isOverdue ? "error.main" : "text.secondary"}>
              Due: {new Date(assignment.dueDate).toLocaleDateString("en-US", {
                month: "short", day: "numeric", year: "numeric",
              })}
              {isOverdue && " · Overdue"}
            </Typography>
          </Stack>

          {isInstructor && (
            <Button size="small" variant="outlined" startIcon={<GradingRounded />}
              onClick={() => onViewSubmissions?.(assignment)}
              sx={{ fontSize: "0.75rem", borderRadius: 2 }}>
              {assignment.submissionCount} Submissions
            </Button>
          )}
        </Stack>
      </Stack>
    </Box>
  );
};

export default AssignmentCard;