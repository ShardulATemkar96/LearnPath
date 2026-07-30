import { useEffect, useState, useCallback } from "react";
import { useParams, useNavigate } from "react-router-dom";
import { useDispatch, useSelector } from "react-redux";
import {
  Alert, Box, Button, Chip, Dialog, DialogContent,
  DialogTitle, Divider, IconButton, Skeleton,
  Stack, Tab, Tabs, TextField, Typography,
} from "@mui/material";
import {
  ArrowBackRounded, CloseRounded,
  ContentCopyRounded, AddRounded, PersonRemoveRounded,
  CheckCircleRounded, VisibilityRounded,
  DownloadRounded, UndoRounded, PictureAsPdfRounded,
  DescriptionRounded, ArticleRounded, InsertDriveFileRounded,
} from "@mui/icons-material";
import { AppDispatch } from "../../redux/store";
import {
  fetchClassroomById, clearSelectedClassroom,
  createAssignmentThunk, updateAssignmentThunk, deleteAssignmentThunk,
} from "../../redux/slices/classroomSlice";
import {
  selectSelectedClassroom,
  selectClassroomDetailLoading,
  selectClassroomError,
} from "../../redux/selectors/classroomSelectors";
import { ROUTES } from "../../constants/routes";
import AssignmentCard from "../../components/classroom/AssignmentCard/AssignmentCard";
import AiFeedbackSection from "../../components/classroom/AiFeedbackSection/AiFeedbackSection";
import { Assignment, Submission, SubmissionStatus, CreateAssignmentRequest } from "../../types/classroom.types";
import { classroomService } from "../../services/classroomService";

const SUBMISSION_STATUS_COLORS: Record<string, "default" | "info" | "success" | "warning" | "primary" | "secondary"> = {
  [SubmissionStatus.NotSubmitted]: "default",
  [SubmissionStatus.Submitted]: "info",
  [SubmissionStatus.SubmittedAgain]: "info",
  [SubmissionStatus.UnderReview]: "warning",
  [SubmissionStatus.Reviewed]: "primary",
  [SubmissionStatus.Graded]: "success",
  [SubmissionStatus.ReturnedForResubmission]: "warning",
};

const formatFileSize = (bytes?: number): string => {
  if (!bytes) return "";
  if (bytes < 1024) return `${bytes} B`;
  if (bytes < 1048576) return `${(bytes / 1024).toFixed(1)} KB`;
  return `${(bytes / 1048576).toFixed(1)} MB`;
};

const formatDateTime = (dateStr: string): string => {
  if (!dateStr) return "";
  const d = new Date(dateStr);
  return d.toLocaleString();
};

const FileIcon = ({ ext }: { ext?: string }) => {
  const extLower = ext?.toLowerCase();
  if (extLower === "pdf") return <PictureAsPdfRounded color="error" />;
  if (extLower === "doc" || extLower === "docx") return <DescriptionRounded color="primary" />;
  if (extLower === "txt") return <ArticleRounded color="action" />;
  return <InsertDriveFileRounded />;
};

const SubmissionsPanel = ({
  open, onClose, classroomId, assignment,
}: {
  open: boolean; onClose: () => void;
  classroomId: number; assignment: Assignment | null;
}) => {
  const [submissions, setSubmissions] = useState<Submission[]>([]);
  const [loading, setLoading] = useState(false);
  const [selectedUserId, setSelectedUserId] = useState<string | null>(null);
  const [gradeInputs, setGradeInputs] = useState<Record<number, { grade: string; feedback: string }>>({});
  const [previewUrl, setPreviewUrl] = useState<string | null>(null);
  const [previewType, setPreviewType] = useState<"pdf" | "txt" | "doc" | null>(null);
  const [previewLoading, setPreviewLoading] = useState(false);
  const [returnDialogOpen, setReturnDialogOpen] = useState(false);
  const [returnFeedback, setReturnFeedback] = useState("");
  const [actionLoading, setActionLoading] = useState(false);
  const [publishLoading, setPublishLoading] = useState(false);

  useEffect(() => {
    if (open && assignment) {
      setLoading(true);
      setSelectedUserId(null);
      setPreviewUrl(null);
      setPreviewType(null);
      classroomService.getSubmissions(classroomId, assignment.id)
        .then(setSubmissions)
        .catch(() => {})
        .finally(() => setLoading(false));
    }
  }, [open, assignment, classroomId]);

  useEffect(() => {
    return () => {
      if (previewUrl) URL.revokeObjectURL(previewUrl);
    };
  }, [previewUrl]);

  const selected = submissions.find((s) => s.userId === selectedUserId) ?? null;

  const refresh = async () => {
    if (!assignment) return;
    const updated = await classroomService.getSubmissions(classroomId, assignment.id);
    setSubmissions(updated);
  };

  const selectSubmission = async (sub: Submission) => {
    setSelectedUserId(sub.userId);
    setPreviewUrl(null);
    setPreviewType(null);
    if (!sub.originalFileName) return;

    const ext = sub.fileExtension?.toLowerCase();
    if (ext === "pdf") {
      setPreviewLoading(true);
      try {
        const blob = await classroomService.previewSubmissionFile(sub.id);
        const url = URL.createObjectURL(blob);
        setPreviewUrl(url);
        setPreviewType("pdf");
      } catch { setPreviewType(null); }
      setPreviewLoading(false);
    } else if (ext === "txt") {
      setPreviewLoading(true);
      try {
        const blob = await classroomService.previewSubmissionFile(sub.id);
        const text = await blob.text();
        const url = URL.createObjectURL(new Blob([text], { type: "text/plain" }));
        setPreviewUrl(url);
        setPreviewType("txt");
      } catch { setPreviewType(null); }
      setPreviewLoading(false);
    } else {
      setPreviewType("doc");
    }
  };

  const handleTransition = async (status: string) => {
    if (!selected?.id) return;
    setActionLoading(true);
    try {
      await classroomService.transitionStatus(selected.id, status);
      await refresh();
    } catch { }
    setActionLoading(false);
  };

  const handleGrade = async () => {
    if (!selected?.id || !assignment) return;
    const inp = gradeInputs[selected.id];
    if (!inp || !inp.grade) return;
    setActionLoading(true);
    try {
      await classroomService.gradeSubmission(classroomId, assignment.id, selected.id, parseInt(inp.grade), inp.feedback);
      await refresh();
    } catch { }
    setActionLoading(false);
  };

  const handleReturn = async () => {
    if (!selected?.id) return;
    setActionLoading(true);
    try {
      await classroomService.returnForResubmission(selected.id, returnFeedback || undefined);
      await refresh();
      setReturnDialogOpen(false);
      setReturnFeedback("");
    } catch { }
    setActionLoading(false);
  };

  const handlePublish = async () => {
    if (!selected?.id || !assignment) return;
    setPublishLoading(true);
    try {
      await classroomService.publishEvaluation(classroomId, assignment.id, selected.id);
      await refresh();
    } catch { }
    setPublishLoading(false);
  };

  const handleDownload = async () => {
    if (!selected) return;
    try {
      const { blob, fileName } = await classroomService.downloadSubmissionFile(selected.id);
      const url = URL.createObjectURL(blob);
      const a = document.createElement("a");
      a.href = url;
      a.download = fileName;
      document.body.appendChild(a);
      a.click();
      document.body.removeChild(a);
      URL.revokeObjectURL(url);
    } catch { }
  };

  const handleOpenFile = () => {
    if (selected && (previewType === "pdf" || previewType === "txt") && previewUrl) {
      window.open(previewUrl, "_blank");
    } else {
      handleDownload();
    }
  };

  const statusChip = (status: string) => (
    <Chip label={status} size="small"
      color={SUBMISSION_STATUS_COLORS[status] ?? "default"}
      variant="outlined" />
  );

  const isSubmittableForReview = selected?.status === SubmissionStatus.Submitted || selected?.status === SubmissionStatus.SubmittedAgain;
  const isUnderReview = selected?.status === SubmissionStatus.UnderReview;
  const isReviewed = selected?.status === SubmissionStatus.Reviewed;
  const isGraded = selected?.status === SubmissionStatus.Graded;
  const isReturned = selected?.status === SubmissionStatus.ReturnedForResubmission;
  const hasFile = !!selected?.originalFileName;

  return (
    <Dialog open={open} onClose={onClose} maxWidth="lg" fullWidth
      PaperProps={{ sx: { borderRadius: 4, minHeight: 560 } }}>
      <DialogTitle sx={{ pb: 1 }}>
        <Stack direction="row" alignItems="center" justifyContent="space-between">
          <Typography variant="h6" fontWeight={700}>
            Submissions {assignment ? `— ${assignment.title}` : ""}
          </Typography>
          <IconButton onClick={onClose} size="small"><CloseRounded /></IconButton>
        </Stack>
      </DialogTitle>
      <DialogContent sx={{ pt: 1, overflow: "auto" }}>
        {loading ? (
          <Box sx={{ p: 4, textAlign: "center" }}>
            <Typography>Loading submissions...</Typography>
          </Box>
        ) : (
          <Box sx={{ display: "flex", gap: 2, height: "100%", minHeight: 480 }}>
            <Box sx={{ width: 300, flexShrink: 0, overflowY: "auto", borderRight: "1px solid #eee", pr: 1 }}>
              <Stack spacing={0.5}>
                {submissions.map((s) => (
                    <Box
                      key={s.userId}
                      onClick={() => selectSubmission(s)}
                      sx={{
                        p: 1.5, borderRadius: 2, cursor: "pointer",
                        bgcolor: selectedUserId === s.userId ? "rgba(108,99,255,0.08)" : "transparent",
                        border: selectedUserId === s.userId ? "1px solid #6C63FF" : "1px solid transparent",
                      "&:hover": { bgcolor: "rgba(108,99,255,0.04)" },
                    }}
                  >
                    <Stack spacing={0.5}>
                      <Typography variant="body2" fontWeight={600} noWrap>
                        {s.userFullName}
                      </Typography>
                      <Stack direction="row" alignItems="center" spacing={1}>
                        {statusChip(s.status)}
                        {s.isLate && s.status !== SubmissionStatus.NotSubmitted && (
                          <Typography variant="caption" color="warning.main">Late</Typography>
                        )}
                      </Stack>
                      {s.originalFileName && (
                        <Typography variant="caption" color="text.secondary" noWrap>
                          <FileIcon ext={s.fileExtension} /> {s.originalFileName}
                        </Typography>
                      )}
                    </Stack>
                  </Box>
                ))}
              </Stack>
            </Box>

            <Box sx={{ flex: 1, overflowY: "auto", pl: 1 }}>
              {!selected ? (
                <Box sx={{ p: 4, textAlign: "center" }}>
                  <Typography color="text.secondary">Select a student to view details</Typography>
                </Box>
              ) : !selected.originalFileName ? (
                <Box sx={{ p: 4, textAlign: "center" }}>
                  <Typography color="text.secondary">No submission yet</Typography>
                </Box>
              ) : (
                <Stack spacing={2}>
                  <Box sx={{ p: 2, bgcolor: "#f8f9fa", borderRadius: 2 }}>
                    <Stack spacing={1}>
                      <Stack direction="row" justifyContent="space-between" alignItems="center">
                        <Typography variant="subtitle1" fontWeight={700}>{selected.userFullName}</Typography>
                        {statusChip(selected.status)}
                      </Stack>
                      <Typography variant="body2" color="text.secondary">
                        Submitted: {formatDateTime(selected.submittedAt)}
                        {selected.isLate && (
                          <Typography component="span" variant="body2" color="warning.main"> (Late)</Typography>
                        )}
                      </Typography>
                      <Typography variant="body2" color="text.secondary">
                        File: {selected.originalFileName} ({formatFileSize(selected.fileSize)})
                      </Typography>
                      {selected.grade !== null && selected.grade !== undefined && (
                        <Typography variant="body2" fontWeight={600}>
                          Grade: {selected.grade}/10
                        </Typography>
                      )}
                      {selected.feedback && (
                        <Typography variant="body2" color="text.secondary">
                          Feedback: {selected.feedback}
                        </Typography>
                      )}
                    </Stack>
                  </Box>

                  <Box sx={{
                    border: "1px solid #ddd", borderRadius: 2, overflow: "hidden",
                    minHeight: previewLoading ? 120 : previewType === "pdf" ? 400 : previewType === "txt" ? 300 : 80,
                  }}>
                    {previewLoading ? (
                      <Box sx={{ p: 4, textAlign: "center" }}><Typography>Loading preview...</Typography></Box>
                    ) : previewType === "pdf" && previewUrl ? (
                      <iframe src={previewUrl} title="Preview" width="100%" height={450} style={{ border: "none" }} />
                    ) : previewType === "txt" && previewUrl ? (
                      <iframe src={previewUrl} title="Preview" width="100%" height={350} style={{ border: "none" }} />
                    ) : previewType === "doc" ? (
                      <Box sx={{ p: 3, textAlign: "center" }}>
                        <DescriptionRounded sx={{ fontSize: 48, color: "primary.main", mb: 1 }} />
                        <Typography color="text.secondary">Preview not available for this file type</Typography>
                        <Typography variant="caption" color="text.secondary">Use Open or Download to view the file</Typography>
                      </Box>
                    ) : null}
                  </Box>

                  <Stack direction="row" spacing={1} flexWrap="wrap">
                    <Button size="small" variant="outlined" startIcon={<VisibilityRounded />}
                      onClick={handleOpenFile} disabled={!hasFile}>
                      Open
                    </Button>
                    <Button size="small" variant="outlined" startIcon={<DownloadRounded />}
                      onClick={handleDownload} disabled={!hasFile}>
                      Download
                    </Button>
                  </Stack>

                  <Divider />

                  {isGraded ? null : (
                    <Stack spacing={1.5}>
                      <Typography variant="subtitle2" fontWeight={600}>Actions</Typography>
                      <Stack direction="row" spacing={1} flexWrap="wrap">
                        {isSubmittableForReview && (
                          <Button size="small" variant="contained" color="info"
                            onClick={() => handleTransition(SubmissionStatus.UnderReview)}
                            disabled={actionLoading}>
                            Mark Under Review
                          </Button>
                        )}
                        {isUnderReview && (
                          <Button size="small" variant="contained" color="primary"
                            onClick={() => handleTransition(SubmissionStatus.Reviewed)}
                            disabled={actionLoading}>
                            Mark Reviewed
                          </Button>
                        )}
                        {(isSubmittableForReview || isUnderReview || isReviewed) && (
                          <Button size="small" variant="outlined" color="warning"
                            startIcon={<UndoRounded />}
                            onClick={() => { setReturnFeedback(""); setReturnDialogOpen(true); }}
                            disabled={actionLoading}>
                            Return for Resubmission
                          </Button>
                        )}
                      </Stack>
                      {(isSubmittableForReview || isUnderReview || isReviewed) && (
                        <Stack direction="row" spacing={1} alignItems="flex-start" pt={1}>
                          <TextField size="small" label="Grade (0-10)" type="number"
                            value={gradeInputs[selected.id]?.grade ?? ""} sx={{ width: 120 }}
                            onChange={(e) => setGradeInputs(p => ({ ...p, [selected.id]: { ...p[selected.id] || { grade: "", feedback: "" }, grade: e.target.value } }))} />
                          <TextField size="small" label="Feedback" value={gradeInputs[selected.id]?.feedback ?? ""} sx={{ width: 200 }}
                            onChange={(e) => setGradeInputs(p => ({ ...p, [selected.id]: { ...p[selected.id] || { grade: "", feedback: "" }, feedback: e.target.value } }))} />
                          <Button size="small" variant="contained" color="success"
                            startIcon={<CheckCircleRounded />}
                            onClick={handleGrade}
                            disabled={!gradeInputs[selected.id]?.grade || actionLoading}>
                            Grade
                          </Button>
                        </Stack>
                      )}
                      {isReturned && (
                        <Typography variant="body2" color="warning.main">
                          Returned for resubmission. Student can upload a replacement.
                        </Typography>
                      )}
                    </Stack>
                  )}

                  {isGraded && !selected.publishedAt && (
                    <>
                      <Divider />
                      <Stack spacing={1.5}>
                        <Typography variant="subtitle2" fontWeight={600}>Publish Evaluation</Typography>
                        <Typography variant="body2" color="text.secondary">
                          Publishing will make the final marks, teacher feedback, and AI feedback visible to the student.
                        </Typography>
                        <Button size="small" variant="contained" color="success"
                          startIcon={<CheckCircleRounded />}
                          onClick={handlePublish}
                          disabled={publishLoading}>
                          {publishLoading ? "Publishing..." : "Publish Evaluation"}
                        </Button>
                      </Stack>
                    </>
                  )}

                  {selected.publishedAt && (
                    <Chip label="Published" size="small" color="success"
                      icon={<CheckCircleRounded />} sx={{ fontWeight: 600 }} />
                  )}

                  {hasFile && (
                    <AiFeedbackSection submissionId={selected.id} />
                  )}
                </Stack>
              )}
            </Box>
          </Box>
        )}
      </DialogContent>

      <Dialog open={returnDialogOpen} onClose={() => setReturnDialogOpen(false)} maxWidth="sm" fullWidth
        PaperProps={{ sx: { borderRadius: 4 } }}>
        <DialogTitle sx={{ pb: 1 }}>
          <Stack direction="row" alignItems="center" justifyContent="space-between">
            <Typography variant="h6" fontWeight={700}>Return for Resubmission</Typography>
            <IconButton onClick={() => setReturnDialogOpen(false)} size="small"><CloseRounded /></IconButton>
          </Stack>
        </DialogTitle>
        <DialogContent>
          <Stack spacing={2} pt={1}>
            <Typography>
              Return <strong>{selected?.userFullName}</strong>'s submission for resubmission?
              The student will be able to upload a replacement.
            </Typography>
            <Typography variant="body2" color="text.secondary">
              Current file will not be deleted until a replacement is uploaded.
            </Typography>
            <TextField label="Feedback (optional)" multiline rows={3} fullWidth
              value={returnFeedback}
              onChange={(e) => setReturnFeedback(e.target.value)}
              placeholder="Provide instructions or reason for resubmission..." />
            <Stack direction="row" spacing={1} justifyContent="flex-end">
              <Button variant="outlined" onClick={() => setReturnDialogOpen(false)} disabled={actionLoading}>
                Cancel
              </Button>
              <Button variant="contained" color="warning"
                onClick={handleReturn} disabled={actionLoading}
                startIcon={<UndoRounded />}>
                {actionLoading ? "Returning..." : "Confirm Return"}
              </Button>
            </Stack>
          </Stack>
        </DialogContent>
      </Dialog>
    </Dialog>
  );
};

const CreateAssignmentModal = ({
  open, onClose, classroomId,
}: { open: boolean; onClose: () => void; classroomId: number }) => {
  const dispatch = useDispatch<AppDispatch>();
  const [form, setForm] = useState({ title: "", description: "", dueDate: "" });
  const [loading, setLoading] = useState(false);

  const handleSubmit = async () => {
    if (!form.title.trim() || !form.dueDate) return;
    setLoading(true);
    await dispatch(createAssignmentThunk({ classroomId, payload: form }));
    setLoading(false);
    setForm({ title: "", description: "", dueDate: "" });
    onClose();
  };

  return (
    <Dialog open={open} onClose={onClose} maxWidth="sm" fullWidth
      PaperProps={{ sx: { borderRadius: 4 } }}>
      <DialogTitle sx={{ pb: 1 }}>
        <Stack direction="row" alignItems="center" justifyContent="space-between">
          <Typography variant="h6" fontWeight={700}>New Assignment</Typography>
          <IconButton onClick={onClose} size="small"><CloseRounded /></IconButton>
        </Stack>
      </DialogTitle>
      <DialogContent>
        <Stack spacing={2.5} pt={1}>
          <TextField label="Title" fullWidth value={form.title}
            onChange={(e) => setForm((p) => ({ ...p, title: e.target.value }))} />
          <TextField label="Description" fullWidth multiline rows={2}
            value={form.description}
            onChange={(e) => setForm((p) => ({ ...p, description: e.target.value }))} />
          <TextField label="Due Date" type="datetime-local" fullWidth
            value={form.dueDate}
            onChange={(e) => setForm((p) => ({ ...p, dueDate: e.target.value }))}
            InputLabelProps={{ shrink: true }} />
          <Button variant="contained" fullWidth size="large"
            onClick={handleSubmit} disabled={loading}
            sx={{ background: "linear-gradient(135deg, #6C63FF, #9D97FF)", borderRadius: 2 }}>
            {loading ? "Creating..." : "Create Assignment"}
          </Button>
        </Stack>
      </DialogContent>
    </Dialog>
  );
};

const EditAssignmentModal = ({
  open, onClose, classroomId, assignment,
}: { open: boolean; onClose: () => void; classroomId: number; assignment: Assignment | null }) => {
  const dispatch = useDispatch<AppDispatch>();
  const [form, setForm] = useState({ title: "", description: "", dueDate: "" });
  const [loading, setLoading] = useState(false);

  useEffect(() => {
    if (assignment) {
      setForm({
        title: assignment.title,
        description: assignment.description,
        dueDate: assignment.dueDate ? new Date(assignment.dueDate).toISOString().slice(0, 16) : "",
      });
    }
  }, [assignment]);

  const handleSubmit = async () => {
    if (!form.title.trim() || !form.dueDate || !assignment) return;
    setLoading(true);
    await dispatch(updateAssignmentThunk({ classroomId, assignmentId: assignment.id, payload: form }));
    setLoading(false);
    onClose();
  };

  return (
    <Dialog open={open} onClose={onClose} maxWidth="sm" fullWidth
      PaperProps={{ sx: { borderRadius: 4 } }}>
      <DialogTitle sx={{ pb: 1 }}>
        <Stack direction="row" alignItems="center" justifyContent="space-between">
          <Typography variant="h6" fontWeight={700}>Edit Assignment</Typography>
          <IconButton onClick={onClose} size="small"><CloseRounded /></IconButton>
        </Stack>
      </DialogTitle>
      <DialogContent>
        <Stack spacing={2.5} pt={1}>
          <TextField label="Title" fullWidth value={form.title}
            onChange={(e) => setForm((p) => ({ ...p, title: e.target.value }))} />
          <TextField label="Description" fullWidth multiline rows={2}
            value={form.description}
            onChange={(e) => setForm((p) => ({ ...p, description: e.target.value }))} />
          <TextField label="Due Date" type="datetime-local" fullWidth
            value={form.dueDate}
            onChange={(e) => setForm((p) => ({ ...p, dueDate: e.target.value }))}
            InputLabelProps={{ shrink: true }} />
          <Button variant="contained" fullWidth size="large"
            onClick={handleSubmit} disabled={loading}
            sx={{ background: "linear-gradient(135deg, #6C63FF, #9D97FF)", borderRadius: 2 }}>
            {loading ? "Updating..." : "Update Assignment"}
          </Button>
        </Stack>
      </DialogContent>
    </Dialog>
  );
};

const ClassroomDetailPage = () => {
  const { id }      = useParams<{ id: string }>();
  const dispatch    = useDispatch<AppDispatch>();
  const navigate    = useNavigate();

  const classroom = useSelector(selectSelectedClassroom);
  const loading   = useSelector(selectClassroomDetailLoading);
  const error     = useSelector(selectClassroomError);

  const [tab, setTab]             = useState(0);
  const [assignmentModal, setAssignmentModal] = useState(false);
  const [editModalOpen, setEditModalOpen] = useState(false);
  const [editingAssignment, setEditingAssignment] = useState<Assignment | null>(null);
  const [submissionsPanel, setSubmissionsPanel] = useState(false);
  const [activeAssignment, setActiveAssignment] = useState<Assignment | null>(null);
  const [copied, setCopied]       = useState(false);

  const refresh = useCallback(() => {
    if (id) dispatch(fetchClassroomById(Number(id)));
  }, [id, dispatch]);

  useEffect(() => {
    if (id) dispatch(fetchClassroomById(Number(id)));
    return () => { dispatch(clearSelectedClassroom()); };
  }, [id, dispatch]);

  const isInstructor = classroom?.userRole === "Instructor";

  const handleCopyCode = () => {
    if (classroom?.inviteCode) {
      navigator.clipboard.writeText(classroom.inviteCode);
      setCopied(true);
      setTimeout(() => setCopied(false), 2000);
    }
  };

  const handleViewSubmissions = (assignment: Assignment) => {
    setActiveAssignment(assignment);
    setSubmissionsPanel(true);
  };

  const handleEditAssignment = (assignment: Assignment) => {
    setEditingAssignment(assignment);
    setEditModalOpen(true);
  };

  const handleDeleteAssignment = async (assignment: Assignment) => {
    if (!window.confirm(`Delete "${assignment.title}"? This action cannot be undone.`)) return;
    await dispatch(deleteAssignmentThunk({ classroomId: classroom.id, assignmentId: assignment.id }));
  };

  if (loading) return (
    <Box>
      <Skeleton height={40} width={200} sx={{ mb: 2 }} />
      <Skeleton variant="rounded" height={160} sx={{ borderRadius: 4, mb: 3 }} />
      <Skeleton variant="rounded" height={300} sx={{ borderRadius: 4 }} />
    </Box>
  );

  if (error) return <Alert severity="error" sx={{ borderRadius: 2 }}>{error}</Alert>;
  if (!classroom) return null;

  return (
    <Box>
      <Button startIcon={<ArrowBackRounded />}
        onClick={() => navigate(ROUTES.CLASSROOM)}
        sx={{ mb: 3, color: "text.secondary" }}>
        Classrooms
      </Button>

      <Box sx={{
        p: 4, borderRadius: 4, mb: 4,
        background: "linear-gradient(135deg, #6C63FF18 0%, #9D97FF10 100%)",
        border: "1px solid rgba(108,99,255,0.12)",
      }}>
        <Stack spacing={2}>
          <Stack direction="row" alignItems="center" spacing={1.5} flexWrap="wrap">
            <Chip
              label={classroom.userRole}
              size="small"
              color={classroom.userRole === "Instructor" ? "primary" : "secondary"}
              sx={{ fontWeight: 600 }}
            />
            <Typography variant="caption" color="text.secondary">
              {classroom.learningPathTitle}
            </Typography>
          </Stack>

          <Typography variant="h4" fontWeight={700}>{classroom.title}</Typography>
          <Typography variant="body1" color="text.secondary">{classroom.description}</Typography>

          <Divider />

          <Stack direction="row" alignItems="center" spacing={3} flexWrap="wrap">
            <Stack direction="row" alignItems="center" spacing={1}>
              <Typography variant="body2" color="text.secondary">Invite Code:</Typography>
              <Chip
                label={classroom.inviteCode}
                size="small"
                sx={{ fontWeight: 700, letterSpacing: 2, fontFamily: "monospace" }}
              />
              <IconButton size="small" onClick={handleCopyCode}>
                <ContentCopyRounded sx={{ fontSize: 16, color: copied ? "success.main" : "text.secondary" }} />
              </IconButton>
            </Stack>
            <Typography variant="body2" color="text.secondary">
              {classroom.memberCount} members
            </Typography>
          </Stack>
        </Stack>
      </Box>

      <Tabs value={isInstructor ? tab : 0} onChange={(_, v) => setTab(v)} sx={{ mb: 3 }}>
        <Tab label={`Assignments (${classroom.assignments.length})`} />
        {isInstructor && <Tab label={`Members (${classroom.members.length})`} />}
      </Tabs>

      {tab === 0 && (
        <Box>
          {isInstructor && (
            <Button variant="contained" startIcon={<AddRounded />}
              onClick={() => setAssignmentModal(true)}
              sx={{ mb: 2 }}>
              Add Assignment
            </Button>
          )}

          <Stack spacing={2}>
            {classroom.assignments.map((a) => (
              <AssignmentCard
                key={a.id}
                assignment={a}
                classroomId={classroom.id}
                isInstructor={isInstructor}
                onRefresh={refresh}
                onViewSubmissions={handleViewSubmissions}
                onEdit={handleEditAssignment}
                onDelete={handleDeleteAssignment}
              />
            ))}
          </Stack>
        </Box>
      )}

      {tab === 1 && isInstructor && (
        <Stack spacing={2}>
          {classroom.members.map((m) => (
            <Box key={m.userId} sx={{ p: 2, border: "1px solid #ddd", borderRadius: 2 }}>
              <Stack direction="row" alignItems="center" justifyContent="space-between">
                <Box>
                  <Typography>{m.fullName}</Typography>
                  <Typography variant="caption">{m.email} — {m.role}</Typography>
                </Box>
                {m.role !== "Instructor" && (
                  <IconButton size="small" color="error"
                    onClick={async () => {
                      if (window.confirm(`Remove ${m.fullName} from this classroom?`)) {
                        await classroomService.removeMember(classroom.id, m.userId);
                        dispatch(fetchClassroomById(classroom.id));
                      }
                    }}
                    title="Remove member">
                    <PersonRemoveRounded />
                  </IconButton>
                )}
              </Stack>
            </Box>
          ))}
        </Stack>
      )}

      <CreateAssignmentModal
        open={assignmentModal}
        onClose={() => setAssignmentModal(false)}
        classroomId={classroom.id}
      />

      <EditAssignmentModal
        open={editModalOpen}
        onClose={() => { setEditModalOpen(false); setEditingAssignment(null); }}
        classroomId={classroom.id}
        assignment={editingAssignment}
      />

      <SubmissionsPanel
        open={submissionsPanel}
        onClose={() => { setSubmissionsPanel(false); refresh(); }}
        classroomId={classroom.id}
        assignment={activeAssignment}
      />
    </Box>
  );
};

export default ClassroomDetailPage;
