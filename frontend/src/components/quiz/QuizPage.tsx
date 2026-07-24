import { Box, Button, CircularProgress, Stack, Typography } from "@mui/material";
import { useCallback, useState } from "react";
import { quizService } from "../../services/quizService";
import {
  AttemptQuestionDto, AttemptResponseDto,
} from "../../types/quiz.types";
import QuestionCard from "./QuestionCard";
import QuizResult from "./QuizResult";
import QuizTimer from "./QuizTimer";

interface QuizPageProps {
  moduleId: number;
  onClose: () => void;
}

type QuizPhase = "intro" | "loading" | "active" | "submitting" | "result" | "error";

const QuizPage = ({ moduleId, onClose }: QuizPageProps) => {
  const [phase, setPhase] = useState<QuizPhase>("intro");
  const [attempt, setAttempt] = useState<AttemptResponseDto | null>(null);
  const [selected, setSelected] = useState<Record<number, number>>({});
  const [textAnswers, setTextAnswers] = useState<Record<number, string>>({});
  const [result, setResult] = useState<AttemptResponseDto | null>(null);

  const startQuiz = useCallback(async () => {
    setPhase("loading");
    try {
      const data = await quizService.startAttempt(moduleId);
      setAttempt(data);
      setPhase("active");
    } catch {
      setPhase("error");
    }
  }, [moduleId]);

  const handleSelectOption = (questionId: number, optionId: number) => {
    setSelected((prev) => ({ ...prev, [questionId]: optionId }));
  };

  const handleTextAnswer = (questionId: number, text: string) => {
    setTextAnswers((prev) => ({ ...prev, [questionId]: text }));
  };

  const submitQuiz = useCallback(async () => {
    if (!attempt) return;
    setPhase("submitting");
    try {
      const answers = attempt.questions.map((q) => ({
        questionId: q.questionId,
        selectedOptionId: selected[q.questionId],
        textAnswer: textAnswers[q.questionId],
      }));
      const data = await quizService.submitAttempt(attempt.id, { answers });
      setResult(data);
      setPhase("result");
    } catch {
      setPhase("error");
    }
  }, [attempt, selected, textAnswers]);

  const timeUp = useCallback(() => {
    submitQuiz();
  }, [submitQuiz]);

  if (phase === "intro") {
    return (
      <Box sx={{ textAlign: "center", py: 6 }}>
        <Typography variant="h4" fontWeight={700} gutterBottom>
          Ready for the Quiz?
        </Typography>
        <Typography variant="body1" color="text.secondary" mb={4}>
          Answer the questions to test your knowledge.
        </Typography>
        <Stack direction="row" spacing={2} justifyContent="center">
          <Button variant="contained" size="large" onClick={startQuiz}
            sx={{ background: "linear-gradient(135deg, #6C63FF, #9D97FF)", borderRadius: 2 }}>
            Start Quiz
          </Button>
          <Button variant="outlined" size="large" onClick={onClose}>Cancel</Button>
        </Stack>
      </Box>
    );
  }

  if (phase === "loading" || phase === "submitting") {
    return (
      <Box sx={{ textAlign: "center", py: 8 }}>
        <CircularProgress />
        <Typography mt={2}>{phase === "loading" ? "Loading quiz..." : "Submitting..."}</Typography>
      </Box>
    );
  }

  if (phase === "error") {
    return (
      <Box sx={{ textAlign: "center", py: 6 }}>
        <Typography variant="h5" color="error" gutterBottom>Something went wrong</Typography>
        <Stack direction="row" spacing={2} justifyContent="center">
          <Button variant="contained" onClick={startQuiz}>Try Again</Button>
          <Button variant="outlined" onClick={onClose}>Back</Button>
        </Stack>
      </Box>
    );
  }

  if (phase === "result" && result) {
    return <QuizResult result={result} onRetry={startQuiz} onClose={onClose} />;
  }

  // Active phase
  const hasTimeLimit = false;

  return (
    <Box sx={{ maxWidth: 800, mx: "auto", py: 3 }}>
      <Stack direction="row" justifyContent="space-between" alignItems="center" mb={3}>
        <Typography variant="h5" fontWeight={700}>Quiz</Typography>
      </Stack>

      {attempt?.questions.map((q: AttemptQuestionDto, i: number) => (
        <QuestionCard
          key={q.questionId}
          question={q}
          selectedOptionId={selected[q.questionId]}
          textAnswer={textAnswers[q.questionId]}
          onSelectOption={handleSelectOption}
          onTextAnswer={handleTextAnswer}
          index={i}
        />
      ))}

      <Box sx={{ textAlign: "center", mt: 3 }}>
        <Button variant="contained" size="large" onClick={submitQuiz
        } sx={{ background: "linear-gradient(135deg, #6C63FF, #9D97FF)", borderRadius: 2 }}>
          Submit Answers
        </Button>
      </Box>
    </Box>
  );
};

export default QuizPage;
