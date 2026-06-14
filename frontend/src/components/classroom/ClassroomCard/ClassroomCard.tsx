import {
  Box, Card, CardActionArea, CardContent,
  Chip, Stack, Typography,
} from "@mui/material";
import {
  GroupsRounded, RouteRounded,
  SchoolRounded, PersonRounded,
} from "@mui/icons-material";
import { useNavigate } from "react-router-dom";
import { Classroom } from "../../../types/classroom.types";

interface ClassroomCardProps {
  classroom: Classroom;
}

const ROLE_COLOR: Record<string, "primary" | "secondary" | "default"> = {
  Instructor: "primary",
  Student:    "secondary",
};

const ClassroomCard = ({ classroom }: ClassroomCardProps) => {
  const navigate = useNavigate();

  return (
    <Card
      sx={{
        borderRadius: 4,
        boxShadow: "0 4px 24px rgba(0,0,0,0.07)",
        transition: "transform 0.22s ease, box-shadow 0.22s ease",
        "&:hover": {
          transform: "translateY(-3px)",
          boxShadow: "0 10px 36px rgba(108,99,255,0.14)",
        },
        height: "100%",
      }}
    >
      <CardActionArea
        onClick={() => navigate(`/classrooms/${classroom.id}`)}
        sx={{ height: "100%", display: "flex", flexDirection: "column", alignItems: "stretch" }}
      >
        <Box
          sx={{
            height: 6,
            background: classroom.userRole === "Instructor"
              ? "linear-gradient(90deg, #6C63FF, #9D97FF)"
              : "linear-gradient(90deg, #FF6584, #FF92A8)",
          }}
        />

        <CardContent sx={{ p: 3, flexGrow: 1 }}>
          <Stack spacing={2}>
            <Stack direction="row" alignItems="flex-start" justifyContent="space-between">
              <Box
                sx={{
                  width: 44, height: 44, borderRadius: 2.5, flexShrink: 0,
                  display: "flex", alignItems: "center", justifyContent: "center",
                  background: classroom.userRole === "Instructor"
                    ? "rgba(108,99,255,0.1)" : "rgba(255,101,132,0.1)",
                }}
              >
                {classroom.userRole === "Instructor"
                  ? <PersonRounded sx={{ color: "primary.main" }} />
                  : <SchoolRounded sx={{ color: "secondary.main" }} />}
              </Box>
              <Chip
                label={classroom.userRole}
                size="small"
                color={ROLE_COLOR[classroom.userRole] ?? "default"}
                sx={{ fontWeight: 600, fontSize: "0.7rem" }}
              />
            </Stack>

            <Box>
              <Typography variant="h6" fontWeight={700} gutterBottom
                sx={{
                  display: "-webkit-box",
                  WebkitLineClamp: 2,
                  WebkitBoxOrient: "vertical",
                  overflow: "hidden",
                }}
              >
                {classroom.title}
              </Typography>
              <Typography variant="body2" color="text.secondary"
                sx={{
                  display: "-webkit-box",
                  WebkitLineClamp: 2,
                  WebkitBoxOrient: "vertical",
                  overflow: "hidden",
                }}
              >
                {classroom.description}
              </Typography>
            </Box>

            <Stack direction="row" justifyContent="space-between" alignItems="center">
              <Stack direction="row" alignItems="center" spacing={0.5}>
                <GroupsRounded sx={{ fontSize: 16, color: "text.secondary" }} />
                <Typography variant="caption" color="text.secondary">
                  {classroom.memberCount} members
                </Typography>
              </Stack>
              <Stack direction="row" alignItems="center" spacing={0.5}>
                <RouteRounded sx={{ fontSize: 16, color: "text.secondary" }} />
                <Typography variant="caption" color="text.secondary" noWrap
                  sx={{ maxWidth: 120 }}>
                  {classroom.learningPathTitle}
                </Typography>
              </Stack>
            </Stack>
          </Stack>
        </CardContent>
      </CardActionArea>
    </Card>
  );
};

export default ClassroomCard;
