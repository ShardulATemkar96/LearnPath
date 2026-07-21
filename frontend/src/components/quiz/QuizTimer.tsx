import { Chip } from "@mui/material";
import { useEffect, useState } from "react";

interface QuizTimerProps {
  timeLimitMinutes: number;
  onTimeUp: () => void;
  isActive: boolean;
}

const QuizTimer = ({ timeLimitMinutes, onTimeUp, isActive }: QuizTimerProps) => {
  const [remaining, setRemaining] = useState(timeLimitMinutes * 60);

  useEffect(() => {
    if (!isActive) return;
    if (remaining <= 0) { onTimeUp(); return; }
    const timer = setTimeout(() => setRemaining((r) => r - 1), 1000);
    return () => clearTimeout(timer);
  }, [remaining, isActive, onTimeUp]);

  const minutes = Math.floor(remaining / 60);
  const seconds = remaining % 60;
  const isLow = remaining < 120;

  return (
    <Chip
      label={`${minutes}:${seconds.toString().padStart(2, "0")}`}
      color={isLow ? "error" : "default"}
      variant={isLow ? "filled" : "outlined"}
      sx={{ fontWeight: 700, fontSize: "1rem", fontFamily: "monospace" }}
    />
  );
};

export default QuizTimer;
