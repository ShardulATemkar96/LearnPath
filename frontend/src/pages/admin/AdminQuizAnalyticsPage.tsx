import { useEffect, useState } from "react";
import { useSearchParams } from "react-router-dom";
import {
  Alert, Box, Card, CardContent, Chip, CircularProgress, FormControl, Grid,
  InputLabel, MenuItem, Paper, Select, Stack, Table, TableBody, TableCell,
  TableContainer, TableHead, TableRow, Typography,
} from "@mui/material";
import {
  BarChartRounded, HowToRegRounded, PeopleRounded, QuizRounded,
} from "@mui/icons-material";
import { quizService } from "../../services/quizService";
import {
  QuizAnalyticsResponseDto, QuizResponseDto, QuestionAnalyticsDto,
} from "../../types/quiz.types";

const AdminQuizAnalyticsPage = () => {
  const [searchParams] = useSearchParams();
  const preselectedId = searchParams.get("quizId");

  const [quizzes, setQuizzes] = useState<QuizResponseDto[]>([]);
  const [selectedQuizId, setSelectedQuizId] = useState<number>(preselectedId ? Number(preselectedId) : 0);
  const [analytics, setAnalytics] = useState<QuizAnalyticsResponseDto | null>(null);
  const [loading, setLoading] = useState(false);
  const [loadingList, setLoadingList] = useState(true);
  const [error, setError] = useState("");

  useEffect(() => {
    (async () => {
      try {
        const data = await quizService.getAll();
        setQuizzes(data);
        if (!preselectedId && data.length > 0) setSelectedQuizId(data[0].id);
      } catch { setError("Failed to load quizzes."); }
      finally { setLoadingList(false); }
    })();
  }, [preselectedId]);

  useEffect(() => {
    if (!selectedQuizId) return;
    (async () => {
      setLoading(true);
      setError("");
      try {
        const data = await quizService.getAnalytics(selectedQuizId);
        setAnalytics(data);
      } catch { setError("Failed to load analytics."); }
      finally { setLoading(false); }
    })();
  }, [selectedQuizId]);

  const statCards = analytics ? [
    { label: "Total Attempts", value: analytics.totalAttempts, icon: <QuizRounded />, color: "#6C63FF" },
    { label: "Unique Students", value: analytics.uniqueStudents, icon: <PeopleRounded />, color: "#22C55E" },
    { label: "Passed", value: analytics.totalPassed, icon: <HowToRegRounded />, color: "#16A34A" },
    { label: "Failed", value: analytics.totalFailed, icon: <BarChartRounded />, color: "#EF4444" },
  ] : [];

  return (
    <Box>
      <Typography variant="h5" fontWeight={700} mb={3}>Quiz Analytics</Typography>

      {error && <Alert severity="error" sx={{ mb: 2 }} onClose={() => setError("")}>{error}</Alert>}

      {loadingList ? <CircularProgress /> : (
        <FormControl sx={{ mb: 3, minWidth: 320 }}>
          <InputLabel>Select Quiz</InputLabel>
          <Select value={selectedQuizId || ""} label="Select Quiz"
            onChange={(e) => setSelectedQuizId(Number(e.target.value))}>
            {quizzes.map((q) => (
              <MenuItem key={q.id} value={q.id}>{q.title}</MenuItem>
            ))}
          </Select>
        </FormControl>
      )}

      {loading ? (
        <Box sx={{ textAlign: "center", py: 8 }}><CircularProgress /></Box>
      ) : analytics ? (
        <>
          <Typography variant="h6" fontWeight={600} mb={2}>{analytics.quizTitle}</Typography>

          <Grid container spacing={3} mb={4}>
            {statCards.map((card) => (
              <Grid item xs={6} md={3} key={card.label}>
                <Card sx={{ borderRadius: 3 }}>
                  <CardContent>
                    <Stack direction="row" alignItems="center" spacing={1} mb={1}>
                      <Box sx={{ color: card.color }}>{card.icon}</Box>
                      <Typography variant="body2" color="text.secondary">{card.label}</Typography>
                    </Stack>
                    <Typography variant="h4" fontWeight={700}>{card.value}</Typography>
                  </CardContent>
                </Card>
              </Grid>
            ))}
          </Grid>

          <Grid container spacing={3} mb={4}>
            <Grid item xs={12} md={6}>
              <Card sx={{ borderRadius: 3 }}>
                <CardContent>
                  <Typography variant="subtitle1" fontWeight={600} mb={2}>Score Overview</Typography>
                  <Stack spacing={1}>
                    <Stack direction="row" justifyContent="space-between">
                      <Typography color="text.secondary">Average Score</Typography>
                      <Typography fontWeight={600}>{analytics.averageScore}%</Typography>
                    </Stack>
                    <Stack direction="row" justifyContent="space-between">
                      <Typography color="text.secondary">Pass Percentage</Typography>
                      <Typography fontWeight={600}>{analytics.passPercentage}%</Typography>
                    </Stack>
                    <Stack direction="row" justifyContent="space-between">
                      <Typography color="text.secondary">Pass / Fail</Typography>
                      <Typography fontWeight={600}>{analytics.totalPassed} / {analytics.totalFailed}</Typography>
                    </Stack>
                  </Stack>
                </CardContent>
              </Card>
            </Grid>

            <Grid item xs={12} md={6}>
              <Card sx={{ borderRadius: 3 }}>
                <CardContent>
                  <Typography variant="subtitle1" fontWeight={600} mb={2}>Score Distribution</Typography>
                  {analytics.scoreDistribution.map((d) => (
                    <Stack key={d.range} direction="row" justifyContent="space-between" alignItems="center" mb={0.5}>
                      <Typography variant="body2" color="text.secondary">{d.range}</Typography>
                      <Box sx={{ flex: 1, mx: 2, height: 8, bgcolor: "action.hover", borderRadius: 4, overflow: "hidden" }}>
                        <Box sx={{ height: "100%", width: `${analytics.totalAttempts > 0 ? (d.count / analytics.totalAttempts) * 100 : 0}%`, bgcolor: "primary.main", borderRadius: 4 }} />
                      </Box>
                      <Typography variant="body2" fontWeight={600}>{d.count}</Typography>
                    </Stack>
                  ))}
                </CardContent>
              </Card>
            </Grid>
          </Grid>

          <Card sx={{ borderRadius: 3, mb: 3 }}>
            <CardContent>
              <Typography variant="subtitle1" fontWeight={600} mb={2}>Question Performance</Typography>
              {analytics.questionAnalytics.length === 0 ? (
                <Typography color="text.secondary">No data available.</Typography>
              ) : (
                <TableContainer>
                  <Table size="small">
                    <TableHead>
                      <TableRow>
                        <TableCell>Question</TableCell>
                        <TableCell>Difficulty</TableCell>
                        <TableCell align="center">Answered</TableCell>
                        <TableCell align="center">Correct</TableCell>
                        <TableCell align="center">Success Rate</TableCell>
                      </TableRow>
                    </TableHead>
                    <TableBody>
                      {analytics.questionAnalytics.map((q: QuestionAnalyticsDto) => (
                        <TableRow key={q.questionId}
                          sx={q.successRate < 40 ? { bgcolor: "error.light", opacity: 0.1 } : undefined}>
                          <TableCell>{q.questionText}</TableCell>
                          <TableCell><Chip label={q.difficulty} size="small" variant="outlined" /></TableCell>
                          <TableCell align="center">{q.timesAnswered}</TableCell>
                          <TableCell align="center">{q.timesCorrect}</TableCell>
                          <TableCell align="center">
                            <Typography fontWeight={600} color={q.successRate >= 60 ? "success.main" : q.successRate >= 40 ? "warning.main" : "error.main"}>
                              {q.successRate}%
                            </Typography>
                          </TableCell>
                        </TableRow>
                      ))}
                    </TableBody>
                  </Table>
                </TableContainer>
              )}
            </CardContent>
          </Card>
        </>
      ) : !loadingList ? (
        <Box sx={{ textAlign: "center", py: 8, color: "text.secondary" }}>
          <BarChartRounded sx={{ fontSize: 64, mb: 2, opacity: 0.3 }} />
          <Typography variant="h6">Select a quiz to view analytics</Typography>
        </Box>
      ) : null}
    </Box>
  );
};

export default AdminQuizAnalyticsPage;
