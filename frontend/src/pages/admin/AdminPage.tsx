import { useEffect, useState } from "react";
import {
  Alert, Avatar, Box, Card, CardContent,
  Chip, CircularProgress, Grid, IconButton,
  InputAdornment, MenuItem, Select, Skeleton,
  Stack, Tab, Tabs, TextField, Tooltip, Typography,
} from "@mui/material";
import {
  SearchRounded, BlockRounded, CheckCircleRounded,
  DeleteRounded, PeopleRounded, RouteRounded,
  ClassRounded, WorkspacePremiumRounded,
} from "@mui/icons-material";
import {
  BarChart, Bar, XAxis, YAxis, CartesianGrid,
  Tooltip as RechartTooltip, ResponsiveContainer,
} from "recharts";
import { adminService } from "../../services/adminService";
import { AdminStats, AdminUser } from "../../types/admin.types";
