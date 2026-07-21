import { Box, FormControlLabel, Radio, RadioGroup, TextField, Typography } from "@mui/material";
import { AttemptQuestionDto, AttemptOptionDto } from "../../types/quiz.types";

interface QuestionCardProps {
  question: AttemptQuestionDto;
  selectedOptionId?: number;
  textAnswer?: string;
  onSelectOption: (questionId: number, optionId: number) => void;
  onTextAnswer: (questionId: number, text: string) => void;
  index: number;
}

const QuestionCard = ({
  question, selectedOptionId, textAnswer,
  onSelectOption, onTextAnswer, index,
}: QuestionCardProps) => {
  const label = question.questionType === 2 ? "Short Answer" : question.questionType === 1 ? "True / False" : "Multiple Choice";

  return (
    <Box
      sx={{
        p: 3, mb: 2, borderRadius: 3,
        bgcolor: "background.paper",
        boxShadow: "0 2px 12px rgba(0,0,0,0.06)",
        border: "1px solid",
        borderColor: "divider",
      }}
    >
      <Typography variant="overline" color="text.secondary" fontWeight={600}>
        Question {index + 1} &middot; {label} &middot; {question.points} pt{question.points !== 1 ? "s" : ""}
      </Typography>
      <Typography variant="h6" fontWeight={600} mt={0.5} mb={2}>
        {question.questionText}
      </Typography>

      {question.questionType === 2 ? (
        <TextField
          fullWidth
          multiline
          minRows={2}
          placeholder="Type your answer..."
          value={textAnswer ?? ""}
          onChange={(e) => onTextAnswer(question.questionId, e.target.value)}
        />
      ) : (
        <RadioGroup
          value={selectedOptionId ?? ""}
          onChange={(e) => onSelectOption(question.questionId, Number(e.target.value))}
        >
          {question.options.map((opt: AttemptOptionDto) => (
            <FormControlLabel
              key={opt.id}
              value={opt.id}
              control={<Radio />}
              label={opt.optionText}
              sx={{
                mb: 0.5, p: 1, borderRadius: 2,
                border: "1px solid",
                borderColor: selectedOptionId === opt.id ? "primary.main" : "divider",
                bgcolor: selectedOptionId === opt.id ? "primary.light" : "transparent",
                color: selectedOptionId === opt.id ? "primary.contrastText" : "text.primary",
                transition: "all 0.15s ease",
                "&:hover": { borderColor: "primary.main" },
              }}
            />
          ))}
        </RadioGroup>
      )}
    </Box>
  );
};

export default QuestionCard;
