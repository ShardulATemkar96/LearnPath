import { useEffect, useState } from "react";
import {
  Alert, Avatar, Box, Button, Card, CardContent,
  CircularProgress, Divider, Grid, Skeleton,
  Stack, TextField, Typography,
} from "@mui/material";
import {
  EditRounded, SaveRounded, CheckCircleRounded,
  RouteRounded, WorkspacePremiumRounded,
} from "@mui/icons-material";
import { userService, UserProfile, UpdateProfilePayload } from "../../services/userService";
import { useAuth } from "../../hooks/useAuth";

const StatBadge = ({
  icon, label, value, color,
}: { icon: React.ReactNode; label: string; value: number; color: string }) => (
  <Box sx={{
    p: 2.5, borderRadius: 3, textAlign: "center",
    border: "1.5px solid", borderColor: "divider",
    bgcolor: "background.paper",
  }}>
    <Box sx={{
      width: 44, height: 44, borderRadius: 2,
      display: "flex", alignItems: "center", justifyContent: "center",
      bgcolor: `${color}18`, mx: "auto", mb: 1.5,
      "& svg": { color, fontSize: 22 },
    }}>
      {icon}
    </Box>
    <Typography variant="h5" fontWeight={700}>{value}</Typography>
    <Typography variant="caption" color="text.secondary">{label}</Typography>
  </Box>
);

const ProfilePage = () => {
  const { user } = useAuth();
  const [profile, setProfile]   = useState<UserProfile | null>(null);
  const [loading, setLoading]   = useState(true);
  const [editing, setEditing]   = useState(false);
  const [saving,  setSaving]    = useState(false);
  const [error,   setError]     = useState("");
  const [success, setSuccess]   = useState("");

  const [form, setForm] = useState<UpdateProfilePayload>({
    firstName: "", lastName: "", avatarUrl: "", bio: "",
  });

  const [pwForm, setPwForm] = useState({
    currentPassword: "", newPassword: "", confirmPassword: "",
  });
  const [pwError,   setPwError]   = useState("");
  const [pwSuccess, setPwSuccess] = useState("");
  const [pwSaving,  setPwSaving]  = useState(false);

  useEffect(() => {
    (async () => {
      try {
        const data = await userService.getProfile();
        setProfile(data);
        setForm({
          firstName: data.firstName,
          lastName:  data.lastName,
          avatarUrl: data.avatarUrl ?? "",
          bio:       data.bio ?? "",
        });
      } finally { setLoading(false); }
    })();
  }, []);

  const handleSave = async () => {
    setSaving(true); setError(""); setSuccess("");
    try {
      const updated = await userService.updateProfile(form);
      setProfile(updated);
      setEditing(false);
      setSuccess("Profile updated successfully.");
    } catch (e: any) {
      setError(e.response?.data?.message ?? "Update failed.");
    } finally { setSaving(false); }
  };

  const handlePasswordChange = async () => {
    setPwError(""); setPwSuccess("");
    if (pwForm.newPassword !== pwForm.confirmPassword) {
      setPwError("Passwords do not match."); return;
    }
    if (pwForm.newPassword.length < 8) {
      setPwError("Minimum 8 characters."); return;
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

  if (loading) return (
    <Box>
      <Skeleton variant="rounded" height={200} sx={{ borderRadius: 4, mb: 3 }} />
      <Skeleton variant="rounded" height={300} sx={{ borderRadius: 4 }} />
    </Box>
  );

  return (
    <Box>
      <Typography variant="h4" fontWeight={700} mb={4}>Profile</Typography>

      {success && <Alert severity="success" sx={{ mb: 3, borderRadius: 2 }}>{success}</Alert>}
      {error   && <Alert severity="error"   sx={{ mb: 3, borderRadius: 2 }}>{error}</Alert>}

      <Grid container spacing={3}>
        {/* Left — Avatar + Stats */}
        <Grid item xs={12} md={4}>
          <Card sx={{ borderRadius: 4, boxShadow: "0 4px 24px rgba(0,0,0,0.07)" }}>
            <CardContent sx={{ p: 3 }}>
              <Stack alignItems="center" spacing={2} mb={3}>
                <Avatar
                  src={profile?.avatarUrl}
                  sx={{
                    width: 96, height: 96, fontSize: "2rem", fontWeight: 700,
                    background: "linear-gradient(135deg, #6C63FF, #FF6584)",
                  }}
                >
                  {profile?.firstName?.[0]}{profile?.lastName?.[0]}
                </Avatar>
                <Box textAlign="center">
                  <Typography variant="h6" fontWeight={700}>
                    {profile?.firstName} {profile?.lastName}
                  </Typography>
                  <Typography variant="body2" color="text.secondary">
                    {profile?.email}
                  </Typography>
                  <Typography variant="caption" color="primary.main" fontWeight={600}>
                    {profile?.roles?.[0]}
                  </Typography>
                </Box>
              </Stack>

              <Divider sx={{ mb: 3 }} />

              <Grid container spacing={1.5}>
                <Grid item xs={12}>
                  <StatBadge
                    icon={<RouteRounded />}
                    label="Paths Created"
                    value={profile?.totalPathsCreated ?? 0}
                    color="#6C63FF"
                  />
                </Grid>
                <Grid item xs={6}>
                  <StatBadge
                    icon={<CheckCircleRounded />}
                    label="Completed"
                    value={profile?.totalModulesCompleted ?? 0}
                    color="#22C55E"
                  />
                </Grid>
                <Grid item xs={6}>
                  <StatBadge
                    icon={<WorkspacePremiumRounded />}
                    label="Certs"
                    value={profile?.totalCertificates ?? 0}
                    color="#F59E0B"
                  />
                </Grid>
              </Grid>
            </CardContent>
          </Card>
        </Grid>

        {/* Right — Edit Form */}
        <Grid item xs={12} md={8}>
          <Stack spacing={3}>
            {/* Profile Info */}
            <Card sx={{ borderRadius: 4, boxShadow: "0 4px 24px rgba(0,0,0,0.07)" }}>
              <CardContent sx={{ p: 3 }}>
                <Stack direction="row" alignItems="center"
                  justifyContent="space-between" mb={3}>
                  <Typography variant="h6" fontWeight={700}>
                    Personal Information
                  </Typography>
                  {!editing ? (
                    <Button startIcon={<EditRounded />} size="small"
                      onClick={() => setEditing(true)} sx={{ borderRadius: 2 }}>
                      Edit
                    </Button>
                  ) : (
                    <Stack direction="row" spacing={1}>
                      <Button size="small" onClick={() => setEditing(false)}
                        sx={{ borderRadius: 2 }}>
                        Cancel
                      </Button>
                      <Button variant="contained" size="small"
                        startIcon={saving
                          ? <CircularProgress size={14} sx={{ color: "#fff" }} />
                          : <SaveRounded />}
                        onClick={handleSave} disabled={saving}
                        sx={{
                          borderRadius: 2,
                          background: "linear-gradient(135deg, #6C63FF, #9D97FF)",
                        }}>
                        Save
                      </Button>
                    </Stack>
                  )}
                </Stack>

                <Grid container spacing={2.5}>
                  <Grid item xs={12} sm={6}>
                    <TextField label="First Name" fullWidth disabled={!editing}
                      value={form.firstName}
                      onChange={(e) => setForm((p) => ({ ...p, firstName: e.target.value }))} />
                  </Grid>
                  <Grid item xs={12} sm={6}>
                    <TextField label="Last Name" fullWidth disabled={!editing}
                      value={form.lastName}
                      onChange={(e) => setForm((p) => ({ ...p, lastName: e.target.value }))} />
                  </Grid>
                  <Grid item xs={12}>
                    <TextField label="Avatar URL" fullWidth disabled={!editing}
                      value={form.avatarUrl}
                      onChange={(e) => setForm((p) => ({ ...p, avatarUrl: e.target.value }))} />
                  </Grid>
                  <Grid item xs={12}>
                    <TextField label="Bio" fullWidth multiline rows={3}
                      disabled={!editing} value={form.bio}
                      onChange={(e) => setForm((p) => ({ ...p, bio: e.target.value }))}
                      placeholder="Tell others about yourself..." />
                  </Grid>
                </Grid>
              </CardContent>
            </Card>

            {/* Change Password */}
            <Card sx={{ borderRadius: 4, boxShadow: "0 4px 24px rgba(0,0,0,0.07)" }}>
              <CardContent sx={{ p: 3 }}>
                <Typography variant="h6" fontWeight={700} mb={3}>
                  Change Password
                </Typography>

                {pwError   && <Alert severity="error"   sx={{ mb: 2, borderRadius: 2 }}>{pwError}</Alert>}
                {pwSuccess && <Alert severity="success" sx={{ mb: 2, borderRadius: 2 }}>{pwSuccess}</Alert>}

                <Stack spacing={2.5}>
                  <TextField label="Current Password" type="password" fullWidth
                    value={pwForm.currentPassword}
                    onChange={(e) => setPwForm((p) => ({ ...p, currentPassword: e.target.value }))} />
                  <TextField label="New Password" type="password" fullWidth
                    value={pwForm.newPassword}
                    onChange={(e) => setPwForm((p) => ({ ...p, newPassword: e.target.value }))} />
                  <TextField label="Confirm New Password" type="password" fullWidth
                    value={pwForm.confirmPassword}
                    onChange={(e) => setPwForm((p) => ({ ...p, confirmPassword: e.target.value }))} />
                  <Button variant="contained" onClick={handlePasswordChange}
                    disabled={pwSaving}
                    sx={{
                      alignSelf: "flex-start", borderRadius: 2,
                      background: "linear-gradient(135deg, #6C63FF, #9D97FF)",
                    }}>
                    {pwSaving ? "Saving..." : "Update Password"}
                  </Button>
                </Stack>
              </CardContent>
            </Card>
          </Stack>
        </Grid>
      </Grid>
    </Box>
  );
};

export default ProfilePage;