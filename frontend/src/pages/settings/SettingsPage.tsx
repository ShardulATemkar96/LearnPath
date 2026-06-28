import { useState } from "react";
import {
  Alert, Box, Card, CardContent, Divider,
  FormControlLabel, Stack, Switch, Typography,
} from "@mui/material";
import {
  NotificationsRounded, DarkModeRounded,
  LanguageRounded, SecurityRounded,
} from "@mui/icons-material";

interface SettingRowProps {
  icon: React.ReactNode;
  title: string;
  description: string;
  checked: boolean;
  onChange: (checked: boolean) => void;
  color?: string;
}

const SettingRow = ({
  icon, title, description, checked, onChange, color = "#6C63FF",
}: SettingRowProps) => (
  <Stack direction="row" alignItems="center" spacing={2} py={2}>
    <Box sx={{
      width: 42, height: 42, borderRadius: 2, flexShrink: 0,
      display: "flex", alignItems: "center", justifyContent: "center",
      bgcolor: `${color}18`, "& svg": { color, fontSize: 20 },
    }}>
      {icon}
    </Box>
    <Box sx={{ flexGrow: 1, minWidth: 0 }}>
      <Typography variant="body2" fontWeight={600}>{title}</Typography>
      <Typography variant="caption" color="text.secondary">{description}</Typography>
    </Box>
    <FormControlLabel
      control={
        <Switch
          checked={checked}
          onChange={(e) => onChange(e.target.checked)}
          color="primary"
        />
      }
      label=""
      sx={{ m: 0 }}
    />
  </Stack>
);

const SettingsPage = () => {
  const [settings, setSettings] = useState({
    emailNotifications:  true,
    pushNotifications:   false,
    darkMode:            false,
    twoFactor:           false,
    weeklyDigest:        true,
    publicProfile:       true,
  });

  const [saved, setSaved] = useState(false);

  const toggle = (key: keyof typeof settings) => {
    setSettings((prev) => ({ ...prev, [key]: !prev[key] }));
    setSaved(true);
    setTimeout(() => setSaved(false), 2000);
  };

  const SECTIONS = [
    {
      title: "Notifications",
      icon: <NotificationsRounded />,
      color: "#6C63FF",
      rows: [
        {
          key: "emailNotifications" as const,
          title: "Email Notifications",
          description: "Receive updates on progress, assignments, and activity.",
        },
        {
          key: "pushNotifications" as const,
          title: "Push Notifications",
          description: "Get real-time alerts directly in your browser.",
        },
        {
          key: "weeklyDigest" as const,
          title: "Weekly Digest",
          description: "A summary of your weekly learning activity.",
        },
      ],
    },
    {
      title: "Appearance",
      icon: <DarkModeRounded />,
      color: "#1A1D2E",
      rows: [
        {
          key: "darkMode" as const,
          title: "Dark Mode",
          description: "Switch to a darker color scheme. (Coming soon)",
        },
      ],
    },
    {
      title: "Privacy",
      icon: <LanguageRounded />,
      color: "#3B82F6",
      rows: [
        {
          key: "publicProfile" as const,
          title: "Public Profile",
          description: "Allow others to view your profile and learning activity.",
        },
      ],
    },
    {
      title: "Security",
      icon: <SecurityRounded />,
      color: "#22C55E",
      rows: [
        {
          key: "twoFactor" as const,
          title: "Two-Factor Authentication",
          description: "Add an extra layer of security to your account. (Coming soon)",
        },
      ],
    },
  ];

  return (
    <Box>
      <Stack spacing={0.5} mb={4}>
        <Typography variant="h4" fontWeight={700}>Settings</Typography>
        <Typography variant="body2" color="text.secondary">
          Manage your preferences and account settings.
        </Typography>
      </Stack>

      {saved && (
        <Alert severity="success" sx={{ mb: 3, borderRadius: 2 }}>
          Settings saved automatically.
        </Alert>
      )}

      <Stack spacing={3} maxWidth={720}>
        {SECTIONS.map((section) => (
          <Card key={section.title}
            sx={{ borderRadius: 4, boxShadow: "0 4px 24px rgba(0,0,0,0.07)" }}>
            <CardContent sx={{ p: 3 }}>
              <Stack direction="row" alignItems="center" spacing={1.5} mb={2}>
                <Box sx={{
                  width: 36, height: 36, borderRadius: 2,
                  display: "flex", alignItems: "center", justifyContent: "center",
                  bgcolor: `${section.color}18`,
                  "& svg": { color: section.color, fontSize: 18 },
                }}>
                  {section.icon}
                </Box>
                <Typography variant="h6" fontWeight={700} fontSize="1rem">
                  {section.title}
                </Typography>
              </Stack>

              <Divider sx={{ mb: 1 }} />

              {section.rows.map((row, idx) => (
                <Box key={row.key}>
                  <SettingRow
                    icon={section.icon}
                    title={row.title}
                    description={row.description}
                    checked={settings[row.key]}
                    onChange={() => toggle(row.key)}
                    color={section.color}
                  />
                  {idx < section.rows.length - 1 && (
                    <Divider sx={{ opacity: 0.5 }} />
                  )}
                </Box>
              ))}
            </CardContent>
          </Card>
        ))}
      </Stack>
    </Box>
  );
};

export default SettingsPage;
