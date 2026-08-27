import { useState } from "react";
import {
  Alert, Box, Button, Card, CardContent, Collapse, Divider,
  FormControlLabel, Stack, Switch, TextField, Typography,
} from "@mui/material";
import {
  DarkModeRounded, SecurityRounded, LockRounded,
  ExpandMoreRounded,
} from "@mui/icons-material";
import { useThemeMode } from "../../context/ThemeModeContext";
import { userService } from "../../services/userService";

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
  const { isDark, toggle: toggleTheme } = useThemeMode();

  const [saved, setSaved] = useState(false);

  const [pwExpanded, setPwExpanded] = useState(false);
  const [pwForm, setPwForm] = useState({ currentPassword: "", newPassword: "", confirmPassword: "" });
  const [pwError, setPwError] = useState("");
  const [pwSuccess, setPwSuccess] = useState("");
  const [pwSaving, setPwSaving] = useState(false);

  const handleNightModeToggle = () => {
    toggleTheme();
    setSaved(true);
    setTimeout(() => setSaved(false), 2000);
  };

  const handlePasswordChange = async () => {
    setPwError(""); setPwSuccess("");
    if (pwForm.newPassword !== pwForm.confirmPassword) {
      setPwError("Passwords do not match."); return;
    }
    if (pwForm.newPassword.length < 8) {
      setPwError("Minimum 8 characters."); return;
    }
    if (!pwForm.currentPassword) {
      setPwError("Current password is required."); return;
    }
    setPwSaving(true);
    try {
      await userService.changePassword(pwForm.currentPassword, pwForm.newPassword);
      setPwSuccess("Password changed successfully.");
      setPwForm({ currentPassword: "", newPassword: "", confirmPassword: "" });
    } catch (e: any) {
      setPwError(e.response?.data?.message ?? "Password change failed.");
    } finally { setPwSaving(false); }
  };

  const SECTIONS = [
    {
      title: "Appearance",
      icon: <DarkModeRounded />,
      color: isDark ? "#9D97FF" : "#1A1D2E",
      rows: [] as any[],
    },
    {
      title: "Security",
      icon: <SecurityRounded />,
      color: "#22C55E",
      rows: [] as any[],
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

              {/* Appearance — functional Night Mode */}
              {section.title === "Appearance" && (
                <SettingRow
                  icon={<DarkModeRounded />}
                  title="Night Mode"
                  description={isDark ? "Dark theme enabled." : "Switch to a darker color scheme."}
                  checked={isDark}
                  onChange={handleNightModeToggle}
                  color={section.color}
                />
              )}

              {/* Security — Change Password expandable */}
              {section.title === "Security" && (
                <Box>
                  <Stack
                    direction="row"
                    alignItems="center"
                    spacing={2}
                    py={2}
                    onClick={() => setPwExpanded((v) => !v)}
                    sx={{
                      cursor: "pointer",
                      borderRadius: 2,
                      "&:hover": { bgcolor: "action.hover" },
                      px: 0.5, mx: -0.5,
                    }}
                  >
                    <Box sx={{
                      width: 42, height: 42, borderRadius: 2, flexShrink: 0,
                      display: "flex", alignItems: "center", justifyContent: "center",
                      bgcolor: `${section.color}18`, "& svg": { color: section.color, fontSize: 20 },
                    }}>
                      <LockRounded />
                    </Box>
                    <Box sx={{ flexGrow: 1, minWidth: 0 }}>
                      <Typography variant="body2" fontWeight={600}>Change Password</Typography>
                      <Typography variant="caption" color="text.secondary">Update your account password.</Typography>
                    </Box>
                    <ExpandMoreRounded
                      sx={{
                        color: "text.secondary",
                        transform: pwExpanded ? "rotate(180deg)" : "rotate(0deg)",
                        transition: "transform 0.2s",
                      }}
                    />
                  </Stack>

                  <Collapse in={pwExpanded} timeout="auto" unmountOnExit>
                    <Box sx={{ pt: 1, pb: 1 }}>
                      <Divider sx={{ mb: 2 }} />
                      {pwError && <Alert severity="error" sx={{ mb: 2, borderRadius: 2 }}>{pwError}</Alert>}
                      {pwSuccess && <Alert severity="success" sx={{ mb: 2, borderRadius: 2 }}>{pwSuccess}</Alert>}
                      <Stack spacing={2}>
                        <TextField label="Current Password" type="password" fullWidth size="small"
                          value={pwForm.currentPassword}
                          onChange={(e) => setPwForm((p) => ({ ...p, currentPassword: e.target.value }))} />
                        <TextField label="New Password" type="password" fullWidth size="small"
                          value={pwForm.newPassword}
                          onChange={(e) => setPwForm((p) => ({ ...p, newPassword: e.target.value }))} />
                        <TextField label="Confirm New Password" type="password" fullWidth size="small"
                          value={pwForm.confirmPassword}
                          onChange={(e) => setPwForm((p) => ({ ...p, confirmPassword: e.target.value }))} />
                        <Button variant="contained" onClick={handlePasswordChange} disabled={pwSaving}
                          sx={{
                            alignSelf: "flex-start", borderRadius: 2,
                            background: "linear-gradient(135deg, #6C63FF, #9D97FF)",
                          }}>
                          {pwSaving ? "Saving..." : "Change Password"}
                        </Button>
                      </Stack>
                    </Box>
                  </Collapse>
                </Box>
              )}
            </CardContent>
          </Card>
        ))}
      </Stack>
    </Box>
  );
};

export default SettingsPage;
