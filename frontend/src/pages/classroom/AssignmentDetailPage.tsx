import { useEffect, useState } from "react";
import { useParams, useNavigate } from "react-router-dom";
import { useDispatch, useSelector } from "react-redux";
import {
  Alert, Box, Button, Card, CardContent, Chip, CircularProgress, Stack, Typography, Divider,
} from "@mui/material";
import {
  ArrowBackRounded, CalendarTodayRounded, CheckCircleRounded,
  CloudUploadRounded, UploadRounded,
  DescriptionRounded, InsertDriveFileRounded, LockRounded,
} from "@mui/icons-material";
import { AppDispatch } from "../../redux/store";
import { fetchClassroomById } from "../../redux/slices/classroomSlice";
import {
  selectSelectedClassroom,
  selectClassroomDetailLoading,
  selectClassroomError,
} from "../../redux/selectors/classroomSelectors";
import { Assignment, Submission, SubmissionStatus } from "../../types/classroom.types";
import { ROUTES } from "../../constants/routes";
import { classroomService } from "../../services/classroomService";
import AssignmentUploadCard from "../../components/classroom/AssignmentUploadCard/AssignmentUploadCard";

const FormattedDescription = ({ text }: { text: string }) => {
  if (!text?.trim()) return <Typography variant="body2" color="text.secondary">No description provided.</Typography>;

  const lines = text.split("\n");
  const elements: React.ReactNode[] = [];
  let bulletGroup: string[] = [];
  let numberedGroup: string[] = [];

  const flushBullets = () => {
    if (bulletGroup.length) {
      elements.push(
        <Box key={`b-${elements.length}`} component="ul" sx={{ pl: 3, mb: 1.5, mt: 1 }}>
          {bulletGroup.map((t, i) => <li key={i}><Typography variant="body2" sx={{ mb: 0.5 }}>{t}</Typography></li>)}
        </Box>
      );
      bulletGroup = [];
    }
  };
  const flushNumbered = () => {
    if (numberedGroup.length) {
      elements.push(
        <Box key={`n-${elements.length}`} component="ol" sx={{ pl: 3, mb: 1.5, mt: 1 }}>
          {numberedGroup.map((t, i) => <li key={i}><Typography variant="body2" sx={{ mb: 0.5 }}>{t}</Typography></li>)}
        </Box>
      );
      numberedGroup = [];
    }
  };

  lines.forEach((raw, idx) => {
    const line = raw.trim();
    if (!line) {
      flushBullets();
      flushNumbered();
      elements.push(<Box key={`s-${idx}`} sx={{ height: 8 }} />);
      return;
    }
    if (line.startsWith("# ")) {
      flushBullets(); flushNumbered();
      elements.push(<Typography key={idx} variant="h6" fontWeight={700} sx={{ mt: 2, mb: 1 }}>{line.slice(2)}</Typography>);
    } else if (line.startsWith("## ")) {
      flushBullets(); flushNumbered();
      elements.push(<Typography key={idx} variant="subtitle1" fontWeight={700} sx={{ mt: 1.5, mb: 1 }}>{line.slice(3)}</Typography>);
    } else if (/^[-*•]\s+/.test(line)) {
      flushNumbered();
      bulletGroup.push(line.replace(/^[-*•]\s+/, ""));
    } else if (/^\d+\.\s+/.test(line)) {
      flushBullets();
      numberedGroup.push(line.replace(/^\d+\.\s+/, ""));
    } else if (line.endsWith(":") && line.length < 80 && !line.includes(".")) {
      flushBullets(); flushNumbered();
      elements.push(<Typography key={idx} variant="subtitle2" fontWeight={700} sx={{ mt: 1.5, mb: 0.5 }}>{line}</Typography>);
    } else {
      flushBullets(); flushNumbered();
      elements.push(<Typography key={idx} variant="body2" color="text.secondary" sx={{ mb: 1, lineHeight: 1.7, whiteSpace: "pre-wrap" }}>{raw}</Typography>);
    }
  });
  flushBullets(); flushNumbered();
  return <Box>{elements}</Box>;
};

const AssignmentDetailPage = () => {
  const { classroomId, assignmentId } = useParams<{ classroomId: string; assignmentId: string }>();
  const navigate = useNavigate();
  const dispatch = useDispatch<AppDispatch>();

  const classroom = useSelector(selectSelectedClassroom);
  const loading = useSelector(selectClassroomDetailLoading);
  const error = useSelector(selectClassroomError);

  const [showUpload, setShowUpload] = useState(false);
  const [uploadError, setUploadError] = useState("");
  const [submission, setSubmission] = useState<Submission | null>(null);

  const cid = Number(classroomId);
  const aid = Number(assignmentId);

  useEffect(() => {
    if (cid) dispatch(fetchClassroomById(cid));
  }, [cid, dispatch]);

  useEffect(() => {
    if (!cid || !aid) return;
    classroomService.getMySubmission(cid, aid)
      .then(setSubmission)
      .catch(() => setSubmission(null));
  }, [cid, aid, classroom?.assignments]);

  const assignment: Assignment | undefined = classroom?.assignments.find((a) => a.id === aid);

  const isInstructor = classroom?.userRole === "Instructor";
  const isOverdue = assignment ? new Date(assignment.dueDate) < new Date() : false;
  const status = assignment?.mySubmissionStatus;

  const hasSubmittedFile = !!assignment?.myOriginalFileName;

  const needsUpload =
    !hasSubmittedFile
    || status === SubmissionStatus.ReturnedForResubmission;

  const showReplace =
    hasSubmittedFile
    && (status === SubmissionStatus.Submitted || status === SubmissionStatus.SubmittedAgain)
    && !showUpload;

  const showUploadArea = !isInstructor && (showUpload || needsUpload);

  const renderStatusChip = () => {
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

  const handleReplaceClick = () => {
    setShowUpload(true);
    setUploadError("");
  };

  const handleUploadSuccess = () => {
    setShowUpload(false);
    setUploadError("");
    dispatch(fetchClassroomById(cid));
  };

  const handleCancel = async () => {
    if (!window.confirm("Cancel your submission?")) return;
    try {
      await classroomService.deleteSubmission(cid, aid);
      dispatch(fetchClassroomById(cid));
    } catch { setUploadError("Failed to cancel."); }
  };

  if (loading && !classroom) return (
    <Box sx={{ textAlign: "center", py: 12 }}><CircularProgress /></Box>
  );

  if (error && !assignment) return (
    <Box sx={{ maxWidth: 600, mx: "auto", py: 8, textAlign: "center" }}>
      <Alert severity="error" sx={{ mb: 3, borderRadius: 2 }}>{error}</Alert>
      <Button variant="outlined" onClick={() => navigate(ROUTES.CLASSROOM_DETAIL.replace(":id", String(cid)))}
        sx={{ borderRadius: 2 }}>Go Back to Classroom</Button>
    </Box>
  );

  if (!assignment) return null;

  const isUnlocked = assignment.isUnlocked ?? true;
  const isLockedForStudent = !isInstructor && !isUnlocked;

  if (isLockedForStudent) {
    return (
      <Box sx={{ maxWidth: 600, mx: "auto", py: 8, textAlign: "center" }}>
        <LockRounded sx={{ fontSize: 64, color: "text.disabled", mb: 2 }} />
        <Alert severity="warning" sx={{ mb: 3, maxWidth: 500, mx: "auto", borderRadius: 2 }}>
          Complete the previous assignment to unlock this assignment.
        </Alert>
        <Button variant="outlined" startIcon={<ArrowBackRounded />}
          onClick={() => navigate(ROUTES.CLASSROOM_DETAIL.replace(":id", String(cid)))}
          sx={{ borderRadius: 2 }}>
          Back to Classroom
        </Button>
      </Box>
    );
  }

  return (
    <Box sx={{ maxWidth: 700, mx: "auto", py: 6, px: 2 }}>
      <Button startIcon={<ArrowBackRounded />}
        onClick={() => navigate(ROUTES.CLASSROOM_DETAIL.replace(":id", String(cid)))}
        sx={{ mb: 3, color: "text.secondary" }}>
        Back to Classroom
      </Button>

      <Box sx={{ textAlign: "center", mb: 4 }}>
        <DescriptionRounded sx={{ fontSize: 64, color: "primary.main", mb: 2 }} />
        <Typography variant="h4" fontWeight={700}>{assignment.title}</Typography>
        {classroom && (
          <Typography variant="body1" color="text.secondary" sx={{ mt: 1 }}>
            {classroom.title}
          </Typography>
        )}
      </Box>

      {uploadError && (
        <Alert severity="error" sx={{ mb: 3, borderRadius: 2 }} onClose={() => setUploadError("")}>
          {uploadError}
        </Alert>
      )}

      <Card sx={{ borderRadius: 4, mb: 3 }}>
        <CardContent sx={{ p: 3 }}>
          <Typography variant="h6" fontWeight={600} mb={2}>Assignment Information</Typography>
          <Stack spacing={2}>
            <FormattedDescription text={assignment.description} />
            <Divider />

            <Stack direction="row" spacing={1.5} alignItems="center">
              <CalendarTodayRounded color={isOverdue ? "error" : "primary"} />
              <Typography variant="body2">
                Due: <strong>{new Date(assignment.dueDate).toLocaleDateString("en-US", {
                  month: "long", day: "numeric", year: "numeric",
                  hour: "2-digit", minute: "2-digit",
                })}</strong>
                {isOverdue && <Typography component="span" color="error.main"> (Overdue)</Typography>}
              </Typography>
            </Stack>

            <Stack direction="row" spacing={1.5} alignItems="center">
              {renderStatusChip() || (
                <Typography variant="body2" color="text.secondary">Status: Not Submitted</Typography>
              )}
            </Stack>

            <Stack direction="row" spacing={1.5} alignItems="center">
              <InsertDriveFileRounded color="primary" />
              <Typography variant="body2">Supported files: <strong>PDF, DOC, DOCX, TXT</strong></Typography>
            </Stack>

            <Stack direction="row" spacing={1.5} alignItems="center">
              <DescriptionRounded color="primary" />
              <Typography variant="body2">Maximum file size: <strong>5 MB</strong></Typography>
            </Stack>
          </Stack>
        </CardContent>
      </Card>

      {status === SubmissionStatus.Graded && assignment.myGrade !== undefined && (
        <Card sx={{ borderRadius: 4, mb: 3 }}>
          <CardContent sx={{ p: 3 }}>
            <Typography variant="h6" fontWeight={600} mb={1}>Result</Typography>
            <Stack direction="row" alignItems="center" spacing={2}>
              <Box sx={{
                width: 56, height: 56, borderRadius: "50%",
                display: "flex", alignItems: "center", justifyContent: "center",
                bgcolor: assignment.myGrade >= 5 ? "success.main" : "error.main",
                color: "#fff",
                fontWeight: 700, fontSize: 20,
              }}>
                {assignment.myGrade}/10
              </Box>
              {assignment.myFeedback && (
                <Typography variant="body2" color="text.secondary">
                  {assignment.myFeedback}
                </Typography>
              )}
            </Stack>
          </CardContent>
        </Card>
      )}

      {submission?.aiFeedback && (
        <Card sx={{ borderRadius: 4, mb: 3 }}>
          <CardContent sx={{ p: 3 }}>
            <Stack spacing={1.5}>
              <Typography variant="h6" fontWeight={600}>AI Feedback</Typography>

              <Box>
                <Typography variant="caption" fontWeight={700} color="text.secondary">Summary</Typography>
                <Typography variant="body2">{submission.aiFeedback.summary}</Typography>
              </Box>

              <Box>
                <Typography variant="caption" fontWeight={700} color="text.secondary">Grammar & Language Feedback</Typography>
                <Typography variant="body2" sx={{ whiteSpace: "pre-wrap" }}>{submission.aiFeedback.grammarFeedback}</Typography>
              </Box>

              <Box>
                <Typography variant="caption" fontWeight={700} color="text.secondary">Rubric Coverage</Typography>
                <Typography variant="body2" sx={{ whiteSpace: "pre-wrap" }}>{submission.aiFeedback.rubricCoverage}</Typography>
              </Box>

              <Box>
                <Typography variant="caption" fontWeight={700} color="text.secondary">Missing Topics</Typography>
                <Typography variant="body2" sx={{ whiteSpace: "pre-wrap" }}>{submission.aiFeedback.missingTopics}</Typography>
              </Box>

              <Box>
                <Typography variant="caption" fontWeight={700} color="text.secondary">Suggested Score</Typography>
                <Typography variant="body2">
                  <Typography component="span" fontWeight={600}>{submission.aiFeedback.suggestedScore.percentage}%</Typography>
                  {" — "}
                  <Typography component="span" fontWeight={600}>{submission.aiFeedback.suggestedScore.marks} marks</Typography>
                </Typography>
              </Box>

              <Box>
                <Typography variant="caption" fontWeight={700} color="text.secondary">Overall Recommendation</Typography>
                <Typography variant="body2">{submission.aiFeedback.overallRecommendation}</Typography>
              </Box>

              <Typography variant="caption" color="text.secondary" sx={{ fontStyle: "italic", pt: 1 }}>
                {submission.aiFeedback.disclaimer}
              </Typography>
            </Stack>
          </CardContent>
        </Card>
      )}

      {!isInstructor && (
        <Card sx={{ borderRadius: 4, mb: 3 }}>
          <CardContent sx={{ p: 3 }}>
            <Box sx={{ mb: 2 }}>
              <Typography variant="h6" fontWeight={600}>Upload Assignment</Typography>
            </Box>

            {showReplace && assignment.myOriginalFileName && (
              <Stack spacing={1.5} mb={2}>
                <Box sx={{ p: 1.5, bgcolor: "grey.50", borderRadius: 2 }}>
                  <Stack direction="row" alignItems="center" spacing={1}>
                    <CloudUploadRounded sx={{ fontSize: 20, color: "primary.main" }} />
                    <Box sx={{ flexGrow: 1, minWidth: 0 }}>
                      <Typography variant="body2" fontWeight={600} noWrap>
                        {assignment.myOriginalFileName}
                      </Typography>
                      <Typography variant="caption" color="text.secondary">
                        {status === SubmissionStatus.SubmittedAgain ? "Resubmitted" : "Submitted"}
                      </Typography>
                    </Box>
                  </Stack>
                </Box>
                <Stack direction="row" spacing={1}>
                  <Button size="small" variant="outlined"
                    startIcon={<UploadRounded />}
                    onClick={handleReplaceClick}
                    sx={{ borderRadius: 2 }}>
                    Replace Submission
                  </Button>
                  {status === SubmissionStatus.Submitted && (
                    <Button size="small" variant="text" color="error"
                      onClick={handleCancel}
                      sx={{ borderRadius: 2 }}>
                      Cancel
                    </Button>
                  )}
                </Stack>
              </Stack>
            )}

            {status === SubmissionStatus.ReturnedForResubmission && (
              <Box sx={{ p: 1.5, bgcolor: "warning.light", borderRadius: 2, mb: 2, color: "warning.dark" }}>
                <Typography variant="body2" fontWeight={600}>
                  Returned for Resubmission — Please upload a revised file.
                </Typography>
              </Box>
            )}

            {showUploadArea && (
              <AssignmentUploadCard
                classroomId={cid}
                assignmentId={aid}
                onUploadSuccess={handleUploadSuccess}
              />
            )}

            {!showUploadArea && !showReplace
              && status !== SubmissionStatus.Graded
              && status !== SubmissionStatus.UnderReview
              && status !== SubmissionStatus.Reviewed
              && status !== SubmissionStatus.ReturnedForResubmission
              && !hasSubmittedFile && (
              <Typography variant="body2" color="text.secondary">
                No file uploaded yet. Use the upload area above to submit your assignment.
              </Typography>
            )}

            {(status === SubmissionStatus.UnderReview || status === SubmissionStatus.Reviewed) && (
              <Typography variant="body2" color="text.secondary">
                Your submission is being reviewed.
              </Typography>
            )}

            {status === SubmissionStatus.Graded && (
              <Typography variant="body2" color="text.secondary">
                This assignment has been graded. No further uploads are allowed.
              </Typography>
            )}
          </CardContent>
        </Card>
      )}
    </Box>
  );
};

export default AssignmentDetailPage;
