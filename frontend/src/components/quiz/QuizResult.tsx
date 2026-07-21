import {
  Box, Button, Chip, Divider, Stack, Typography,
} from "@mui/material";
import { CheckCircle, Cancel } from "@mui/icons-material";
import { AttemptResultDto } from "../../types/quiz.types";

interface QuizResultProps {
  result: AttemptResultDto;
  onRetry?: () => void;
  onClose: () => void;
}

const QuizResult = ({ result, onRetry, onClose }: QuizResultProps) => {
  const percentage = result.totalPoints > 0
    ? Math.round((result.score / result.totalPoints) * 100)
    : 0;

  return (
    <Box sx={{ maxWidth: 700, mx: "auto", p: 3 }}>
      <Box
        sx={{
          textAlign: "center", p: 4, borderRadius: 4,
          bgcolor: result.isPassed ? "success.light" : "error.light",
          color: "#fff", mb: 3,
        }}
      >
        <Typography variant="h3" fontWeight={800}>
          {percentage}%
        </Typography>
        <Typography variant="h5" fontWeight={600}>
          {result.isPassed ? "Passed!" : "Not Passed"}
        </Typography>
        <Typography variant="body1" mt={1}>
          {result.score} / {result.totalPoints} points
        </Typography>
      </Box>

      <Typography variant="h6" fontWeight={600} mb={2}>
        Results
      </Typography>

      {result.questionResults.map((qr, i) => (
        <Box key={qr.questionId} sx={{ mb: 2, p: 2, borderRadius: 2, border: "1px solid", borderColor: "divider", bgcolor: "background.paper" }}>
          <Stack direction="row" alignItems="center" spacing={1} mb={1}>
            {qr.isCorrect
              ? <CheckCircle color="success" fontSize="small" />
              : <Cancel color="error" fontSize="small" />}
            <Typography fontWeight={600}>
              Question {i + 1}
            </Typography>
            <Chip label={`${qr.pointsAwarded}/${qr.points}`} size="small" color={qr.isCorrect ? "success" : "error"} variant="outlined" />
          </Stack>
          <Typography variant="body1" mb={1}>{qr.questionText}</Typography>

          {qr.correctOptionId && (
            <Typography variant="body2" color="text.secondary">
              Correct answer: Option {qr.correctOptionId}
            </Typography>
          )}
          {qr.correctAnswerText && (
            <Typography variant="body2" color="text.secondary">
              Correct answer: {qr.correctAnswerText}
            </Typography>
          )}
          {qr.explanation && (
            <Typography variant="body2" color="text.secondary" mt={1} fontStyle="italic">
              {qr.explanation}
            </Typography>
          )}
        </Box>
      ))}

      <Divider sx={{ my: 3 }} />

      <Stack direction="row" spacing={2} justifyContent="center">
        {onRetry && (
          <Button variant="contained" onClick={onRetry}>
            Retry Quiz
          </Button>
        )}
        <Button variant="outlined" onClick={onClose}>
          Back to Module
        </Button>
      </Stack>
    </Box>
  );
};

export default QuizResult;
