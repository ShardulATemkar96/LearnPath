import { useState } from "react";
import { Link as RouterLink, useNavigate, useLocation } from "react-router-dom";
import {
  Box, Button, Divider, IconButton, InputAdornment,
  Link, Stack, TextField, Typography, Alert, CircularProgress,
} from "@mui/material";
import { Visibility, VisibilityOff } from "@mui/icons-material";
import { useAuth } from "../../hooks/useAuth";
import { validateLogin } from "../../validations/authValidation";
import { ROUTES } from "../../constants/routes";

const LoginPage = () => {
  const navigate = useNavigate();
  const location = useLocation();
  const { login, loading, error } = useAuth();

  const from = (location.state as any)?.from?.pathname ?? ROUTES.DASHBOARD;

  const [email, setEmail] = useState("");
  const [password, setPassword] = useState("");
  const [showPassword, setShowPassword] = useState(false);
  const [fieldErrors, setFieldErrors] = useState<{ email?: string; password?: string }>({});

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    const errors = validateLogin(email, password);
    if (Object.keys(errors).length > 0) { setFieldErrors(errors); return; }
    setFieldErrors({});
    const result = await login({ email, password });
    if ((result as any).meta?.requestStatus === "fulfilled") navigate(from, { replace: true });
  };

  return (
    <Box
      sx={{
        width: "100%",
        maxWidth: 440,
        bgcolor: "background.paper",
        borderRadius: 4,
        boxShadow: "0 8px 40px rgba(108,99,255,0.12)",
        p: { xs: 3, sm: 5 },
      }}
    >
      {/* Header */}
      <Stack spacing={1} mb={4}>
        <Typography variant="h4" fontWeight={700} color="primary.main">
          Welcome back 👋
        </Typography>
        <Typography variant="body2" color="text.secondary">
          Sign in to continue your learning journey.
        </Typography>
      </Stack>

      {/* Error Alert */}
      {error && (
        <Alert severity="error" sx={{ mb: 3, borderRadius: 2 }}>
          {error}
        </Alert>
      )}

      {/* Form */}
      <Box component="form" onSubmit={handleSubmit} noValidate>
        <Stack spacing={2.5}>
          <TextField
            label="Email address"
            type="email"
            fullWidth
            value={email}
            onChange={(e) => setEmail(e.target.value)}
            error={!!fieldErrors.email}
            helperText={fieldErrors.email}
            autoComplete="email"
            autoFocus
          />

          <TextField
            label="Password"
            type={showPassword ? "text" : "password"}
            fullWidth
            value={password}
            onChange={(e) => setPassword(e.target.value)}
            error={!!fieldErrors.password}
            helperText={fieldErrors.password}
            autoComplete="current-password"
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

          <Box sx={{ textAlign: "right" }}>
            <Link component={RouterLink} to="#" variant="caption" color="primary">
              Forgot password?
            </Link>
          </Box>

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
            {loading ? <CircularProgress size={22} sx={{ color: "#fff" }} /> : "Sign In"}
          </Button>
        </Stack>
      </Box>

      <Divider sx={{ my: 3 }}>
        <Typography variant="caption" color="text.secondary">OR</Typography>
      </Divider>

      <Typography variant="body2" textAlign="center" color="text.secondary">
        Don&apos;t have an account?{" "}
        <Link component={RouterLink} to={ROUTES.REGISTER} fontWeight={600} color="primary">
          Create one
        </Link>
      </Typography>
    </Box>
  );
};

export default LoginPage;
