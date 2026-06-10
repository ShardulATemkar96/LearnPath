import {
  Avatar, Box, Card, CardContent,
  CardHeader, Divider, Stack, Typography,
} from "@mui/material";
import { CheckCircleRounded, StarRounded, GroupAddRounded } from "@mui/icons-material";

interface ActivityItem {
  id: number;
  type: "completion" | "achievement" | "join";
  message: string;
  time: string;
}

const ICON_MAP = {
  completion:  { icon: <CheckCircleRounded />, color: "#22C55E" },
  achievement: { icon: <StarRounded />,        color: "#F59E0B" },
  join:        { icon: <GroupAddRounded />,    color: "#6C63FF" },
};

interface ActivityFeedProps {
  items: ActivityItem[];
}

const ActivityFeed = ({ items }: ActivityFeedProps) => (
  <Card sx={{ borderRadius: 4, boxShadow: "0 4px 24px rgba(0,0,0,0.07)", height: "100%" }}>
    <CardHeader
      title={
        <Typography variant="h6" fontWeight={600}>
          Recent Activity
        </Typography>
      }
    />
    <Divider />
    <CardContent sx={{ p: 0 }}>
      {items.length === 0 ? (
        <Box sx={{ p: 4, textAlign: "center" }}>
          <Typography variant="body2" color="text.secondary">
            No activity yet. Start learning!
          </Typography>
        </Box>
      ) : (
        <Stack divider={<Divider />}>
          {items.map((item) => {
            const meta = ICON_MAP[item.type];
            return (
              <Stack
                key={item.id}
                direction="row"
                alignItems="center"
                spacing={2}
                sx={{ px: 3, py: 2 }}
              >
                <Avatar
                  sx={{
                    width: 36,
                    height: 36,
                    bgcolor: `${meta.color}18`,
                    "& svg": { color: meta.color, fontSize: 18 },
                  }}
                >
                  {meta.icon}
                </Avatar>
                <Box sx={{ flexGrow: 1, minWidth: 0 }}>
                  <Typography variant="body2" fontWeight={500} noWrap>
                    {item.message}
                  </Typography>
                  <Typography variant="caption" color="text.secondary">
                    {item.time}
                  </Typography>
                </Box>
              </Stack>
            );
          })}
        </Stack>
      )}
    </CardContent>
  </Card>
);

export default ActivityFeed;
