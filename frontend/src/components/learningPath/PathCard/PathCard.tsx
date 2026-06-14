import {
  Box, Card, CardActionArea, CardContent,
  CardMedia, Chip, Stack, Typography,
} from "@mui/material";
import {
  LayersRounded, PersonRounded, PublicRounded, LockRounded,
} from "@mui/icons-material";
import { useNavigate } from "react-router-dom";
import { LearningPath } from "../../../types/path.types";

interface PathCardProps {
  path: LearningPath;
}

const FALLBACK_GRADIENT =
  "linear-gradient(135deg, #6C63FF22 0%, #9D97FF22 100%)";

const PathCard = ({ path }: PathCardProps) => {
  const navigate = useNavigate();

  return (
    <Card
      sx={{
        borderRadius: 4,
        overflow: "hidden",
        boxShadow: "0 4px 24px rgba(0,0,0,0.07)",
        transition: "transform 0.22s ease, box-shadow 0.22s ease",
        "&:hover": {
          transform: "translateY(-3px)",
          boxShadow: "0 10px 36px rgba(108,99,255,0.15)",
        },
        height: "100%",
        display: "flex",
        flexDirection: "column",
      }}
    >
      <CardActionArea
        onClick={() => navigate(`/paths/${path.id}`)}
        sx={{ flexGrow: 1, display: "flex", flexDirection: "column", alignItems: "stretch" }}
      >
        {path.thumbnailUrl ? (
          <CardMedia
            component="img"
            height={160}
            image={path.thumbnailUrl}
            alt={path.title}
            sx={{ objectFit: "cover" }}
          />
        ) : (
          <Box
            sx={{
              height: 160,
              background: FALLBACK_GRADIENT,
              display: "flex",
              alignItems: "center",
              justifyContent: "center",
            }}
          >
            <LayersRounded sx={{ fontSize: 52, color: "primary.main", opacity: 0.5 }} />
          </Box>
        )}

        <CardContent sx={{ flexGrow: 1, p: 2.5 }}>
          <Stack spacing={1.5}>
            <Stack direction="row" spacing={1} flexWrap="wrap">
              {path.isPublic
                ? <Chip icon={<PublicRounded />} label="Public" size="small"
                    sx={{ fontSize: "0.7rem", bgcolor: "primary.light", color: "#fff" }} />
                : <Chip icon={<LockRounded />} label="Private" size="small"
                    sx={{ fontSize: "0.7rem" }} />}
              {path.isPublished
                ? <Chip label="Published" size="small" color="success" sx={{ fontSize: "0.7rem" }} />
                : <Chip label="Draft" size="small" sx={{ fontSize: "0.7rem" }} />}
            </Stack>

            <Typography
              variant="h6"
              fontWeight={700}
              sx={{
                display: "-webkit-box",
                WebkitLineClamp: 2,
                WebkitBoxOrient: "vertical",
                overflow: "hidden",
              }}
            >
              {path.title}
            </Typography>

            <Typography
              variant="body2"
              color="text.secondary"
              sx={{
                display: "-webkit-box",
                WebkitLineClamp: 2,
                WebkitBoxOrient: "vertical",
                overflow: "hidden",
              }}
            >
              {path.description}
            </Typography>

            <Stack
              direction="row"
              justifyContent="space-between"
              alignItems="center"
              pt={1}
            >
              <Stack direction="row" alignItems="center" spacing={0.5}>
                <LayersRounded sx={{ fontSize: 16, color: "text.secondary" }} />
                <Typography variant="caption" color="text.secondary">
                  {path.totalModules} modules
                </Typography>
              </Stack>
              <Stack direction="row" alignItems="center" spacing={0.5}>
                <PersonRounded sx={{ fontSize: 16, color: "text.secondary" }} />
                <Typography variant="caption" color="text.secondary" noWrap>
                  {path.createdByName}
                </Typography>
              </Stack>
            </Stack>
          </Stack>
        </CardContent>
      </CardActionArea>
    </Card>
  );
};

export default PathCard;
