import { useEffect, useState } from "react";
import {
  Box, Button, Chip, CircularProgress, Divider, Stack, Typography,
} from "@mui/material";
import { AutoAwesomeRounded, CheckCircleRounded, ErrorRounded, RefreshRounded } from "@mui/icons-material";
import { AiFeedbackResponse } from "../../../types/classroom.types";
import { classroomService } from "../../../services/classroomService";

const DISCLAIMER =
  "This feedback is AI-generated and is intended only to assist the instructor. " +
  "The final evaluation is determined solely by the instructor.";

const AiFeedbackSection = ({ submissionId }: { submissionId: number }) => {
  const [feedback, setFeedback] = useState<AiFeedbackResponse | null>(null);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState("");
  const [fetching, setFetching] = useState(false);

  useEffect(() => {
    if (!submissionId) return;
    let cancelled = false;
    setFetching(true);
    classroomService.getAiFeedback(submissionId)
      .then((res) => { if (!cancelled) setFeedback(res); })
      .catch(() => { if (!cancelled) setFeedback(null); })
      .finally(() => { if (!cancelled) setFetching(false); });
    return () => { cancelled = true; };
  }, [submissionId]);

  const handleGenerate = async () => {
    setLoading(true);
    setError("");
    try {
      const result = await classroomService.generateAiFeedback(submissionId);
      setFeedback(result);
    } catch (err: any) {
      const msg = err?.response?.data?.message || err?.message || "Failed to generate AI feedback.";
      setError(msg);
    }
    setLoading(false);
  };

  const hasFeedback = !!feedback;

  return (
    <Box>
      <Divider sx={{ my: 2 }} />

      <Stack direction="row" alignItems="center" spacing={1} sx={{ mb: 1.5 }}>
        <Typography variant="subtitle2" fontWeight={700}>
          AI Assignment Feedback
        </Typography>
        {!fetching && hasFeedback && (
          <Chip icon={<CheckCircleRounded />} label="AI Feedback Available"
            size="small" color="success" variant="outlined" />
        )}
        {!fetching && !hasFeedback && !loading && !error && (
          <Chip label="No AI Feedback" size="small" variant="outlined" />
        )}
        {!fetching && error && (
          <Chip icon={<ErrorRounded />} label="Generation Failed"
            size="small" color="error" variant="outlined" />
        )}
      </Stack>

      {fetching ? (
        <Box sx={{ p: 2, textAlign: "center" }}>
          <CircularProgress size={20} sx={{ mr: 1 }} />
          <Typography variant="body2" color="text.secondary" component="span">
            Loading...
          </Typography>
        </Box>
      ) : (
        <>
          {!hasFeedback && !loading && (
            <Button
              variant="outlined"
              size="small"
              startIcon={<AutoAwesomeRounded />}
              onClick={handleGenerate}
              sx={{ borderRadius: 2 }}
            >
              Generate AI Feedback
            </Button>
          )}

          {loading && (
            <Box sx={{ p: 2, bgcolor: "#f8f9fa", borderRadius: 2, mb: 1 }}>
              <Stack direction="row" alignItems="center" spacing={1.5}>
                <CircularProgress size={20} />
                <Typography variant="body2" color="text.secondary">
                  Generating AI Feedback...
                </Typography>
              </Stack>
            </Box>
          )}

          {error && (
            <Typography variant="body2" color="error" sx={{ mb: 1 }}>
              {error}
            </Typography>
          )}

          {hasFeedback && (
            <>
              <Box sx={{ p: 2, bgcolor: "#f8f9fa", borderRadius: 2, mb: 1 }}>
                <Stack spacing={1.5}>
                  <Box>
                    <Typography variant="caption" fontWeight={700} color="text.secondary">
                      Summary
                    </Typography>
                    <Typography variant="body2">{feedback.summary}</Typography>
                  </Box>

                  <Divider />

                  <Box>
                    <Typography variant="caption" fontWeight={700} color="text.secondary">
                      Grammar & Language Feedback
                    </Typography>
                    <Typography variant="body2" sx={{ whiteSpace: "pre-wrap" }}>
                      {feedback.grammarFeedback}
                    </Typography>
                  </Box>

                  <Divider />

                  <Box>
                    <Typography variant="caption" fontWeight={700} color="text.secondary">
                      Rubric Coverage
                    </Typography>
                    <Typography variant="body2" sx={{ whiteSpace: "pre-wrap" }}>
                      {feedback.rubricCoverage}
                    </Typography>
                  </Box>

                  <Divider />

                  <Box>
                    <Typography variant="caption" fontWeight={700} color="text.secondary">
                      Missing Topics
                    </Typography>
                    <Typography variant="body2" sx={{ whiteSpace: "pre-wrap" }}>
                      {feedback.missingTopics}
                    </Typography>
                  </Box>

                  <Divider />

                  <Box>
                    <Typography variant="caption" fontWeight={700} color="text.secondary">
                      Suggested Score
                    </Typography>
                    <Typography variant="body2">
                      <Typography component="span" fontWeight={600}>
                        {feedback.suggestedScore.percentage}%
                      </Typography>
                      {" — "}
                      <Typography component="span" fontWeight={600}>
                        {feedback.suggestedScore.marks} marks
                      </Typography>
                    </Typography>
                    <Typography variant="caption" color="text.secondary">
                      Suggested by AI. Teacher decides final marks.
                    </Typography>
                  </Box>

                  <Divider />

                  <Box>
                    <Typography variant="caption" fontWeight={700} color="text.secondary">
                      Overall Recommendation
                    </Typography>
                    <Typography variant="body2">{feedback.overallRecommendation}</Typography>
                  </Box>
                </Stack>
              </Box>

              <Typography variant="caption" color="text.secondary" sx={{ display: "block", mb: 1 }}>
                Generated at: {new Date(feedback.generatedAt).toLocaleString()}
              </Typography>

              <Typography
                variant="caption"
                color="text.secondary"
                sx={{ display: "block", fontStyle: "italic", mb: 1.5 }}
              >
                {DISCLAIMER}
              </Typography>

              <Button
                variant="outlined"
                size="small"
                startIcon={<RefreshRounded />}
                onClick={handleGenerate}
                disabled={loading}
                sx={{ borderRadius: 2 }}
              >
                Regenerate AI Feedback
              </Button>
            </>
          )}
        </>
      )}
    </Box>
  );
};

export default AiFeedbackSection;
