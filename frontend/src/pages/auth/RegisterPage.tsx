import { useState } from "react";
import { Link as RouterLink, useNavigate } from "react-router-dom";
import {
  Alert, Box, Button, CircularProgress, Divider,
  Grid, IconButton, InputAdornment, Link,
  Stack, TextField, Typography,
} from "@mui/material";
import { Visibility, VisibilityOff } from "@mui/icons-material";
import { useAuth } from "../../hooks/useAuth";
import { validateRegister } from "../../validations/authValidation";
import { ROUTES } from "../../constants/routes";
import type { RegisterFormErrors } from "../../validations/authValidation";

const RegisterPage = () => {
  const navigate = useNavigate();
  const { register, loading, error } = useAuth();

  const [form, setForm] = useState({
    firstName: "",
    lastName: "",
    email: "",
    password: "",
    confirmPassword: "",
  });

  const [showPassword, setShowPassword] = useState(false);
  const [showConfirm, setShowConfirm] = useState(false);
  const [fieldErrors, setFieldErrors] = useState<RegisterFormErrors>({});

  const handleChange = (field: keyof typeof form) =>
    (e: React.ChangeEvent<HTMLInputElement>) =>
      setForm((prev) => ({ ...prev, [field]: e.target.value }));

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    const errors = validateRegister(
      form.firstName, form.lastName, form.email, form.password, form.confirmPassword
    );
    if (Object.keys(errors).length > 0) { setFieldErrors(errors); return; }
    setFieldErrors({});
    const result = await register({
      firstName: form.firstName,
      lastName: form.lastName,
      email: form.email,
      password: form.password,
    });
    if ((result as any).meta?.requestStatus === "fulfilled")
      navigate(ROUTES.DASHBOARD, { replace: true });
  };

  return (
    <Box
      sx={{
        width: "100%",
        maxWidth: 520,
        bgcolor: "background.paper",
        borderRadius: 4,
        boxShadow: "0 8px 40px rgba(108,99,255,0.12)",
        p: { xs: 3, sm: 5 },
      }}
    >
      {/* Header */}
      <Stack spacing={1} mb={4}>
        <Typography variant="h4" fontWeight={700} color="primary.main">
          Start learning today 🚀
        </Typography>
        <Typography variant="body2" color="text.secondary">
          Create your free LearnPath account.
        </Typography>
      </Stack>

      {error && (
        <Alert severity="error" sx={{ mb: 3, borderRadius: 2 }}>{error}</Alert>
      )}

      <Box component="form" onSubmit={handleSubmit} noValidate>
        <Stack spacing={2.5}>
          <Grid container spacing={2}>
            <Grid item xs={12} sm={6}>
              <TextField
                label="First name"
                fullWidth
                value={form.firstName}
                onChange={handleChange("firstName")}
                error={!!fieldErrors.firstName}
                helperText={fieldErrors.firstName}
                autoFocus
              />
            </Grid>
            <Grid item xs={12} sm={6}>
              <TextField
                label="Last name"
                fullWidth
                value={form.lastName}
                onChange={handleChange("lastName")}
                error={!!fieldErrors.lastName}
                helperText={fieldErrors.lastName}
              />
            </Grid>
          </Grid>

          <TextField
            label="Email address"
            type="email"
            fullWidth
            value={form.email}
            onChange={handleChange("email")}
            error={!!fieldErrors.email}
            helperText={fieldErrors.email}
            autoComplete="email"
          />

          <TextField
            label="Password"
            type={showPassword ? "text" : "password"}
            fullWidth
            value={form.password}
            onChange={handleChange("password")}
            error={!!fieldErrors.password}
            helperText={fieldErrors.password ?? "Min 8 chars, 1 uppercase, 1 number"}
            InputProps={{
              endAdornment: (
                <InputAdornment position="end">
                  <IconButton onClick={() => setShowPassword((p) => !p)} edge="end">
                    {showPassword ? <VisibilityOff /> : <Visibility />}
                  </IconButton>
                </InputAdornment>
              ),
            }}
          />

          <TextField
            label="Confirm password"
            type={showConfirm ? "text" : "password"}
            fullWidth
            value={form.confirmPassword}
            onChange={handleChange("confirmPassword")}
            error={!!fieldErrors.confirmPassword}
            helperText={fieldErrors.confirmPassword}
            InputProps={{
              endAdornment: (
                <InputAdornment position="end">
                  <IconButton onClick={() => setShowConfirm((p) => !p)} edge="end">
                    {showConfirm ? <VisibilityOff /> : <Visibility />}
                  </IconButton>
                </InputAdornment>
              ),
            }}
          />

          <Button
            type="submit"
            variant="contained"
            fullWidth
            size="large"
            disabled={loading}
            sx={{
              py: 1.5,
              background: "linear-gradient(135deg, #6C63FF 0%, #9D97FF 100%)",
              "&:hover": {
                background: "linear-gradient(135deg, #4B44CC 0%, #6C63FF 100%)",
              },
            }}
          >
            {loading
              ? <CircularProgress size={22} sx={{ color: "#fff" }} />
              : "Create Account"}
          </Button>
        </Stack>
      </Box>

      <Divider sx={{ my: 3 }}>
        <Typography variant="caption" color="text.secondary">OR</Typography>
      </Divider>

      <Typography variant="body2" textAlign="center" color="text.secondary">
        Already have an account?{" "}
        <Link component={RouterLink} to={ROUTES.LOGIN} fontWeight={600} color="primary">
          Sign in
        </Link>
      </Typography>
    </Box>
  );
};

export default RegisterPage;
