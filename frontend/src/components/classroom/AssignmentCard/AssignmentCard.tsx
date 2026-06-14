import {
  Box, Button, Chip, Stack, Typography,
} from "@mui/material";
import {
  CalendarTodayRounded, CheckCircleRounded, UploadRounded,
} from "@mui/icons-material";
import { Assignment } from "../../../types/classroom.types";

interface AssignmentCardProps {
  assignment: Assignment;
  isInstructor: boolean;
  onSubmit?: (assignment: Assignment) => void;
  onViewSubmissions?: (assignment: Assignment) => void;
}

const AssignmentCard = ({
  assignment, isInstructor, onSubmit, onViewSubmissions,
}: AssignmentCardProps) => {
  const isOverdue = new Date(assignment.dueDate) < new Date();

  return (
    <Box
      sx={{
        p: 3, borderRadius: 3,
        border: "1.5px solid",
        borderColor: assignment.hasSubmitted ? "success.main" : "divider",
        bgcolor: "background.paper",
        transition: "border-color 0.2s, box-shadow 0.2s",
        "&:hover": { boxShadow: "0 4px 16px rgba(0,0,0,0.07)" },
      }}
    >
      <Stack spacing={1.5}>
        <Stack direction="row" alignItems="flex-start" justifyContent="space-between">
          <Typography variant="body1" fontWeight={700} sx={{ flexGrow: 1, pr: 1 }}>
            {assignment.title}
          </Typography>
          {assignment.hasSubmitted && (
            <Chip
              icon={<CheckCircleRounded sx={{ fontSize: "14px !important" }} />}
              label="Submitted"
              size="small"
              color="success"
              sx={{ fontWeight: 600, fontSize: "0.7rem" }}
            />
          )}
        </Stack>

        <Typography variant="body2" color="text.secondary">
          {assignment.description}
        </Typography>

        <Stack direction="row" alignItems="center" justifyContent="space-between" flexWrap="wrap" gap={1}>
          <Stack direction="row" alignItems="center" spacing={0.75}>
            <CalendarTodayRounded sx={{ fontSize: 15, color: isOverdue ? "error.main" : "text.secondary" }} />
            <Typography
              variant="caption"
              fontWeight={500}
              color={isOverdue ? "error.main" : "text.secondary"}
            >
              Due: {new Date(assignment.dueDate).toLocaleDateString("en-US", {
                month: "short", day: "numeric", year: "numeric",
              })}
              {isOverdue && " · Overdue"}
            </Typography>
          </Stack>

          <Stack direction="row" spacing={1}>
            {isInstructor ? (
              <Button
                size="small" variant="outlined"
                onClick={() => onViewSubmissions?.(assignment)}
                sx={{ fontSize: "0.75rem", borderRadius: 2 }}
              >
                {assignment.submissionCount} Submissions
              </Button>
            ) : !assignment.hasSubmitted ? (
              <Button
                size="small" variant="contained"
                startIcon={<UploadRounded />}
                onClick={() => onSubmit?.(assignment)}
                disabled={isOverdue}
                sx={{
                  fontSize: "0.75rem", borderRadius: 2,
                  background: "linear-gradient(135deg, #6C63FF, #9D97FF)",
                }}
              >
                Submit
              </Button>
            ) : null}
          </Stack>
        </Stack>
      </Stack>
    </Box>
  );
};

export default AssignmentCard;
