import { useState, useRef, useCallback } from "react";
import {
  Box, Button, Stack, Typography, CircularProgress,
  IconButton,
} from "@mui/material";
import {
  CloudUploadRounded, CloseRounded, InsertDriveFileRounded,
  PictureAsPdfRounded, DescriptionRounded, ArticleRounded,
  CheckCircleRounded,
} from "@mui/icons-material";
import { classroomService } from "../../../services/classroomService";
import { SubmissionStatus } from "../../../types/classroom.types";

const ALLOWED_EXTENSIONS = [".pdf", ".doc", ".docx", ".txt"];
const MAX_FILE_SIZE = 5 * 1024 * 1024;

const formatFileSize = (bytes: number): string => {
  if (bytes < 1024) return `${bytes} B`;
  if (bytes < 1024 * 1024) return `${(bytes / 1024).toFixed(1)} KB`;
  return `${(bytes / (1024 * 1024)).toFixed(1)} MB`;
};

const getFileIcon = (name: string) => {
  const ext = name.split(".").pop()?.toLowerCase();
  if (ext === "pdf") return <PictureAsPdfRounded sx={{ fontSize: 36, color: "error.main" }} />;
  if (ext === "doc" || ext === "docx") return <DescriptionRounded sx={{ fontSize: 36, color: "primary.main" }} />;
  if (ext === "txt") return <ArticleRounded sx={{ fontSize: 36, color: "text.secondary" }} />;
  return <InsertDriveFileRounded sx={{ fontSize: 36, color: "text.secondary" }} />;
};

interface AssignmentUploadCardProps {
  classroomId: number;
  assignmentId: number;
  onUploadSuccess: () => void;
}

const AssignmentUploadCard = ({
  classroomId, assignmentId, onUploadSuccess,
}: AssignmentUploadCardProps) => {
  const [selectedFile, setSelectedFile] = useState<File | null>(null);
  const [uploading, setUploading] = useState(false);
  const [error, setError] = useState("");
  const [successMsg, setSuccessMsg] = useState("");
  const [isDragOver, setIsDragOver] = useState(false);
  const fileInputRef = useRef<HTMLInputElement>(null);
  const dragCounter = useRef(0);

  const isFileValid = (file: File): string | null => {
    const ext = "." + file.name.split(".").pop()?.toLowerCase();
    if (!ALLOWED_EXTENSIONS.includes(ext)) {
      return `Unsupported file type. Accepted: PDF, DOC, DOCX, TXT`;
    }
    if (file.size > MAX_FILE_SIZE) {
      return `File exceeds 5 MB limit.`;
    }
    return null;
  };

  const handleFile = useCallback((file: File) => {
    setError("");
    setSuccessMsg("");
    const validationError = isFileValid(file);
    if (validationError) {
      setError(validationError);
      return;
    }
    setSelectedFile(file);
  }, []);

  const handleDragEnter = (e: React.DragEvent) => {
    e.preventDefault();
    e.stopPropagation();
    dragCounter.current++;
    if (e.dataTransfer.items && e.dataTransfer.items.length > 0) {
      setIsDragOver(true);
    }
  };

  const handleDragLeave = (e: React.DragEvent) => {
    e.preventDefault();
    e.stopPropagation();
    dragCounter.current--;
    if (dragCounter.current === 0) {
      setIsDragOver(false);
    }
  };

  const handleDragOver = (e: React.DragEvent) => {
    e.preventDefault();
    e.stopPropagation();
  };

  const handleDrop = (e: React.DragEvent) => {
    e.preventDefault();
    e.stopPropagation();
    setIsDragOver(false);
    dragCounter.current = 0;
    const files = e.dataTransfer.files;
    if (files.length > 0) {
      handleFile(files[0]);
    }
  };

  const handleBrowse = () => {
    fileInputRef.current?.click();
  };

  const handleFileInputChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    const files = e.target.files;
    if (files && files.length > 0) {
      handleFile(files[0]);
    }
    if (fileInputRef.current) {
      fileInputRef.current.value = "";
    }
  };

  const handleRemove = () => {
    setSelectedFile(null);
    setError("");
  };

  const handleUpload = async () => {
    if (!selectedFile) return;
    setUploading(true);
    setError("");
    setSuccessMsg("");
    try {
      await classroomService.uploadSubmission(classroomId, assignmentId, selectedFile);
      setSuccessMsg("Assignment Submitted Successfully");
      setTimeout(() => {
        setSelectedFile(null);
        setSuccessMsg("");
        onUploadSuccess();
      }, 1500);
    } catch (e: any) {
      const msg = e.response?.data?.message
        || e.response?.data?.errors?.[0]
        || "Upload failed. Please try again.";
      setError(msg);
    } finally {
      setUploading(false);
    }
  };

  const statusIsUploadable = (selectedFile || uploading || error || successMsg) ? "idle" : "empty";

  return (
    <Box>
      <input
        ref={fileInputRef}
        type="file"
        hidden
        accept=".pdf,.doc,.docx,.txt"
        onChange={handleFileInputChange}
      />

      {successMsg ? (
        <Box sx={{
          p: 3, borderRadius: 3,
          border: "2px solid",
          borderColor: "success.main",
          bgcolor: "success.main",
          color: "#fff",
          textAlign: "center",
        }}>
          <CheckCircleRounded sx={{ fontSize: 48, mb: 1 }} />
          <Typography variant="h6" fontWeight={700}>{successMsg}</Typography>
        </Box>
      ) : uploading ? (
        <Box sx={{
          p: 3, borderRadius: 3,
          border: "2px dashed",
          borderColor: "primary.light",
          bgcolor: "primary.main",
          color: "#fff",
          textAlign: "center",
        }}>
          <CircularProgress size={40} sx={{ color: "#fff", mb: 1 }} />
          <Typography variant="body1" fontWeight={600}>Uploading...</Typography>
        </Box>
      ) : selectedFile ? (
        <Stack spacing={1.5}>
          {error && (
            <Box sx={{
              p: 1.5, borderRadius: 2,
              bgcolor: "error.light",
              color: "error.dark",
            }}>
              <Typography variant="body2" fontWeight={600}>{error}</Typography>
            </Box>
          )}

          <Box sx={{
            p: 2, borderRadius: 3,
            border: "1px solid",
            borderColor: "divider",
            bgcolor: "grey.50",
          }}>
            <Stack direction="row" alignItems="center" spacing={2}>
              {getFileIcon(selectedFile.name)}
              <Box sx={{ flexGrow: 1, minWidth: 0 }}>
                <Typography variant="body2" fontWeight={600} noWrap>
                  {selectedFile.name}
                </Typography>
                <Typography variant="caption" color="text.secondary">
                  {formatFileSize(selectedFile.size)}
                </Typography>
              </Box>
              <IconButton size="small" onClick={handleRemove} disabled={uploading}>
                <CloseRounded fontSize="small" />
              </IconButton>
            </Stack>
          </Box>

          <Button
            variant="contained"
            fullWidth
            size="large"
            onClick={handleUpload}
            disabled={uploading}
            startIcon={<CloudUploadRounded />}
            sx={{ borderRadius: 2, background: "linear-gradient(135deg, #6C63FF, #9D97FF)" }}
          >
            Submit Assignment
          </Button>
        </Stack>
      ) : (
        <Box
          onDragEnter={handleDragEnter}
          onDragLeave={handleDragLeave}
          onDragOver={handleDragOver}
          onDrop={handleDrop}
          sx={{
            p: 4, borderRadius: 3,
            border: "2px dashed",
            borderColor: isDragOver ? "primary.main" : "divider",
            bgcolor: isDragOver ? "primary.main" : "background.paper",
            color: isDragOver ? "#fff" : "text.secondary",
            textAlign: "center",
            cursor: "pointer",
            transition: "all 0.2s ease",
            "&:hover": {
              borderColor: "primary.light",
              bgcolor: "grey.50",
            },
          }}
          onClick={handleBrowse}
        >
          <Stack spacing={1.5} alignItems="center">
            <CloudUploadRounded sx={{ fontSize: 48, opacity: 0.6 }} />
            <Typography variant="body1" fontWeight={600}>
              {isDragOver ? "Drop your file here" : "Drag & Drop Here"}
            </Typography>
            <Typography variant="body2" color={isDragOver ? "#fff" : "text.secondary"}>
              OR
            </Typography>
            <Button
              variant="outlined"
              size="small"
              onClick={(e) => { e.stopPropagation(); handleBrowse(); }}
              sx={{ borderRadius: 2 }}
            >
              Choose File
            </Button>
            <Typography variant="caption" color={isDragOver ? "rgba(255,255,255,0.7)" : "text.secondary"}>
              Supported: PDF, DOC, DOCX, TXT &middot; Max Size: 5 MB
            </Typography>
          </Stack>
        </Box>
      )}
    </Box>
  );
};

export default AssignmentUploadCard;
