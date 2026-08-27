import { useState } from "react";
import { useNavigate } from "react-router-dom";
import {
  Box, Button, Chip, IconButton, Stack, Typography, Snackbar,
} from "@mui/material";
import {
  CalendarTodayRounded, CheckCircleRounded,
  GradingRounded, EditRounded, DeleteRounded, LockRounded,
} from "@mui/icons-material";
import { Assignment, SubmissionStatus } from "../../../types/classroom.types";
import { ROUTES } from "../../../constants/routes";

interface AssignmentCardProps {
  assignment: Assignment;
  classroomId: number;
  isInstructor: boolean;
  onViewSubmissions?: (assignment: Assignment) => void;
  onEdit?: (assignment: Assignment) => void;
  onDelete?: (assignment: Assignment) => void;
}

const AssignmentCard = ({
  assignment, classroomId, isInstructor, onViewSubmissions, onEdit, onDelete,
}: AssignmentCardProps) => {
  const navigate = useNavigate();
  const [showLockedToast, setShowLockedToast] = useState(false);

  const isUnlocked = assignment.isUnlocked ?? true;
  const isLockedForStudent = !isInstructor && !isUnlocked;

  const isOverdue = new Date(assignment.dueDate) < new Date();
  const status = assignment.mySubmissionStatus;

  const getBorderColor = () => {
    if (isLockedForStudent) return "divider";
    if (!status) return "divider";
    if (status === SubmissionStatus.Graded) return "success.main";
    if (status === SubmissionStatus.ReturnedForResubmission) return "warning.main";
    return "info.main";
  };

  const getStatusChip = () => {
    if (!status) return null;
    if (status === SubmissionStatus.Submitted)
      return <Chip label="Submitted" size="small" color="info" variant="outlined" />;
    if (status === SubmissionStatus.UnderReview)
      return <Chip label="Under Review" size="small" color="default" variant="outlined" />;
    if (status === SubmissionStatus.Reviewed)
      return <Chip label="Reviewed" size="small" color="default" variant="outlined" />;
    if (status === SubmissionStatus.Graded)
      return <Chip icon={<CheckCircleRounded sx={{ fontSize: "14px !important" }} />}
        label="Graded" size="small" color="success" sx={{ fontWeight: 600 }} />;
    if (status === SubmissionStatus.ReturnedForResubmission)
      return <Chip label="Resubmit Required" size="small" color="warning" variant="outlined" />;
    if (status === SubmissionStatus.SubmittedAgain)
      return <Chip label="Resubmitted" size="small" color="info" variant="outlined" />;
    return null;
  };

  const handleCardClick = () => {
    if (isLockedForStudent) {
      setShowLockedToast(true);
      return;
    }
    if (!isInstructor) {
      navigate(ROUTES.ASSIGNMENT_DETAIL
        .replace(":classroomId", String(classroomId))
        .replace(":assignmentId", String(assignment.id)));
    }
  };

  return (
    <>
    <Box sx={{
      p: 3, borderRadius: 3,
      border: "2px solid",
      borderColor: getBorderColor(),
      bgcolor: isLockedForStudent ? "rgba(0,0,0,0.02)" : "background.paper",
      opacity: isLockedForStudent ? 0.65 : 1,
      cursor: isLockedForStudent ? "not-allowed" : isInstructor ? "default" : "pointer",
      transition: "border-color 0.2s, box-shadow 0.2s",
      "&:hover": {
        boxShadow: "0 4px 16px rgba(0,0,0,0.07)",
        ...(isLockedForStudent || isInstructor ? {} : { borderColor: "primary.light" }),
      },
    }}
    onClick={handleCardClick}>
      <Stack spacing={1.5}>
        <Stack direction="row" alignItems="flex-start" justifyContent="space-between">
          <Stack direction="row" alignItems="center" spacing={1} sx={{ flexGrow: 1, pr: 1 }}>
            {isLockedForStudent && <LockRounded sx={{ fontSize: 18, color: "text.disabled" }} />}
            <Typography variant="body1" fontWeight={700} sx={{ flexGrow: 1 }}>
              {assignment.title}
            </Typography>
          </Stack>
          <Stack direction="row" spacing={1} alignItems="center">
            {isLockedForStudent ? (
              <Chip icon={<LockRounded sx={{ fontSize: 14 }} />} label="Locked" size="small" sx={{ fontWeight: 600 }} />
            ) : (
              <>
                {getStatusChip()}
                {status === SubmissionStatus.Graded && assignment.myGrade !== undefined && (
                  <Chip label={`${assignment.myGrade}/10`} size="small"
                    color={assignment.myGrade >= 5 ? "success" : "error"} variant="filled" sx={{ fontWeight: 700 }} />
                )}
                {status === SubmissionStatus.ReturnedForResubmission && assignment.myGrade !== undefined && (
                  <Chip label={`${assignment.myGrade}/10`} size="small" color="error" variant="filled" sx={{ fontWeight: 700 }} />
                )}
              </>
            )}
          </Stack>
        </Stack>

        {isLockedForStudent ? (
          <Typography variant="body2" color="text.disabled" sx={{ fontStyle: "italic" }}>
            Complete the previous assignment to unlock this assignment.
          </Typography>
        ) : (
          <Typography variant="body2" color="text.secondary" sx={{ display: "-webkit-box", WebkitLineClamp: 2, WebkitBoxOrient: "vertical", overflow: "hidden" }}>
            {assignment.description || "No description"}
          </Typography>
        )}

        {status === SubmissionStatus.Graded && assignment.myGrade !== undefined && (
          <Box sx={{ p: 1.5, bgcolor: "#f5f5f5", borderRadius: 2 }}>
            <Typography variant="body2" fontWeight={600}>Grade: {assignment.myGrade}/10</Typography>
            {assignment.myFeedback && (
              <Typography variant="body2" color="text.secondary">Remarks: {assignment.myFeedback}</Typography>
            )}
          </Box>
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
            <Stack direction="row" spacing={0.5}>
              <IconButton size="small" color="primary"
                onClick={(e) => { e.stopPropagation(); onEdit?.(assignment); }}
                sx={{ border: "1px solid", borderColor: "divider", borderRadius: 1.5 }}>
                <EditRounded sx={{ fontSize: 16 }} />
              </IconButton>
              <IconButton size="small" color="error"
                onClick={(e) => { e.stopPropagation(); onDelete?.(assignment); }}
                sx={{ border: "1px solid", borderColor: "divider", borderRadius: 1.5 }}>
                <DeleteRounded sx={{ fontSize: 16 }} />
              </IconButton>
              <Button size="small" variant="outlined" startIcon={<GradingRounded />}
                onClick={(e) => { e.stopPropagation(); onViewSubmissions?.(assignment); }}
                sx={{ fontSize: "0.75rem", borderRadius: 2 }}>
                {assignment.submissionCount} Submissions
              </Button>
            </Stack>
          )}
        </Stack>
      </Stack>
    </Box>
    <Snackbar
      open={showLockedToast}
      autoHideDuration={3000}
      onClose={() => setShowLockedToast(false)}
      message="Complete the previous assignment to unlock this assignment."
      anchorOrigin={{ vertical: "bottom", horizontal: "center" }}
    />
    </>
  );
};

export default AssignmentCard;
