import { useCallback, useEffect, useMemo, useRef, useState } from "react";
import {
  Alert, Box, Card, CardContent, Chip, MenuItem, Skeleton,
  Stack, TextField, Typography,
} from "@mui/material";
import { HistoryRounded, LockRounded } from "@mui/icons-material";
import { historyService } from "../../services/historyService";
import {
  HistoryActionOption, HistoryEntry,
} from "../../types/history.types";
import EmptyState from "../../components/common/EmptyState/EmptyState";
import PaginationBar from "../../components/common/PaginationBar/PaginationBar";
import SearchBar from "../../components/common/SearchBar/SearchBar";

const PAGE_SIZE = 20;

const CATEGORY_COLORS: Record<string, string> = {
  Authentication: "#6C63FF",
  Account:        "#0EA5E9",
  Academic:       "#10B981",
  Assessment:     "#F59E0B",
  Community:      "#FF6584",
};

const categoryColor = (category: string) =>
  CATEGORY_COLORS[category] ?? "#6C63FF";

const formatDateTime = (iso: string) => {
  const date = new Date(iso.endsWith("Z") ? iso : `${iso}Z`);
  return date.toLocaleString("en-US", {
    year: "numeric", month: "short", day: "numeric",
    hour: "2-digit", minute: "2-digit",
  });
};

const TimelineEntry = ({ entry, isLast }: { entry: HistoryEntry; isLast: boolean }) => {
  const color = categoryColor(entry.category);

  return (
    <Stack direction="row" spacing={2} sx={{ position: "relative" }}>
      {/* Rail */}
      <Stack alignItems="center" sx={{ flexShrink: 0, width: 24 }}>
        <Box sx={{
          width: 12, height: 12, borderRadius: "50%", mt: 2.5,
          bgcolor: color, boxShadow: `0 0 0 4px ${color}22`,
        }} />
        {!isLast && (
          <Box sx={{ width: 2, flexGrow: 1, bgcolor: "divider", mt: 1 }} />
        )}
      </Stack>

      <Card sx={{
        flexGrow: 1, mb: 2, borderRadius: 3,
        boxShadow: "0 2px 14px rgba(0,0,0,0.05)",
      }}>
        <CardContent sx={{ p: 2.5, "&:last-child": { pb: 2.5 } }}>
          <Stack spacing={1}>
            <Stack
              direction="row"
              alignItems="center"
              justifyContent="space-between"
              flexWrap="wrap"
              gap={1}
            >
              <Stack direction="row" alignItems="center" spacing={1} flexWrap="wrap" gap={0.5}>
                <Typography variant="subtitle2" fontWeight={700}>
                  {entry.actionName}
                </Typography>
                <Chip
                  label={entry.category}
                  size="small"
                  sx={{
                    height: 20, fontSize: "0.68rem", fontWeight: 600,
                    color, bgcolor: `${color}1A`,
                  }}
                />
              </Stack>
              <Typography variant="caption" color="text.secondary">
                {formatDateTime(entry.timestamp)}
              </Typography>
            </Stack>

            <Typography variant="body2" color="text.secondary">
              {entry.description}
            </Typography>

            {entry.entityType && (
              <Typography variant="caption" color="text.disabled">
                {entry.entityType}
                {entry.entityId ? ` #${entry.entityId}` : ""}
                {entry.additionalData ? ` · ${entry.additionalData}` : ""}
              </Typography>
            )}
          </Stack>
        </CardContent>
      </Card>
    </Stack>
  );
};

const HistoryPage = () => {
  const [entries,    setEntries]    = useState<HistoryEntry[]>([]);
  const [options,    setOptions]    = useState<HistoryActionOption[]>([]);
  const [page,       setPage]       = useState(1);
  const [totalPages, setTotalPages] = useState(0);
  const [totalCount, setTotalCount] = useState(0);
  const [loading,    setLoading]    = useState(true);
  const [error,      setError]      = useState("");

  const [search,     setSearch]     = useState("");
  const [actionType, setActionType] = useState<string>("");
  const [fromDate,   setFromDate]   = useState("");
  const [toDate,     setToDate]     = useState("");

  const [debouncedSearch, setDebouncedSearch] = useState("");
  const isFirstLoad = useRef(true);

  useEffect(() => {
    const timer = setTimeout(() => setDebouncedSearch(search), 400);
    return () => clearTimeout(timer);
  }, [search]);

  useEffect(() => {
    (async () => {
      try {
        setOptions(await historyService.getActionTypes());
      } catch { /* filter list is optional — the feed still works without it */ }
    })();
  }, []);

  const loadHistory = useCallback(async () => {
    setLoading(true);
    try {
      const data = await historyService.getMy({
        page,
        pageSize: PAGE_SIZE,
        search:     debouncedSearch || undefined,
        actionType: actionType !== "" ? Number(actionType) : undefined,
        fromDate:   fromDate || undefined,
        toDate:     toDate || undefined,
      });
      setEntries(data.entries);
      setTotalPages(data.totalPages);
      setTotalCount(data.totalCount);
      setError("");
    } catch {
      setError("Failed to load your history.");
    } finally {
      setLoading(false);
      isFirstLoad.current = false;
    }
  }, [page, debouncedSearch, actionType, fromDate, toDate]);

  useEffect(() => { loadHistory(); }, [loadHistory]);

  // Any filter change restarts at the newest page.
  useEffect(() => {
    if (!isFirstLoad.current) setPage(1);
  }, [debouncedSearch, actionType, fromDate, toDate]);

  const groupedOptions = useMemo(() => {
    const groups = new Map<string, HistoryActionOption[]>();
    options.forEach((o) => {
      const list = groups.get(o.category) ?? [];
      list.push(o);
      groups.set(o.category, list);
    });
    return groups;
  }, [options]);

  const hasFilters = Boolean(debouncedSearch || actionType || fromDate || toDate);

  return (
    <Box>
      <Stack spacing={0.5} mb={3}>
        <Typography variant="h4" fontWeight={700}>History</Typography>
        <Stack direction="row" alignItems="center" spacing={0.75}>
          <LockRounded sx={{ fontSize: 15, color: "text.secondary" }} />
          <Typography variant="body2" color="text.secondary">
            Your permanent activity record — newest first. This history is read-only
            and cannot be edited or removed.
          </Typography>
        </Stack>
      </Stack>

      <Card sx={{ borderRadius: 3, mb: 3, boxShadow: "0 2px 14px rgba(0,0,0,0.05)" }}>
        <CardContent sx={{ p: 2.5, "&:last-child": { pb: 2.5 } }}>
          <Stack
            direction={{ xs: "column", md: "row" }}
            spacing={2}
            alignItems={{ md: "center" }}
          >
            <Box sx={{ flexGrow: 1, minWidth: 0 }}>
              <SearchBar
                value={search}
                onChange={setSearch}
                placeholder="Search your history..."
                fullWidth
              />
            </Box>

            <TextField
              select
              size="small"
              label="Action"
              value={actionType}
              onChange={(e) => setActionType(e.target.value)}
              sx={{ minWidth: 210 }}
            >
              <MenuItem value="">All actions</MenuItem>
              {[...groupedOptions.entries()].map(([category, items]) => [
                <MenuItem key={`${category}-header`} disabled sx={{ opacity: 0.7 }}>
                  {category}
                </MenuItem>,
                ...items.map((o) => (
                  <MenuItem key={o.actionType} value={String(o.actionType)} sx={{ pl: 3 }}>
                    {o.actionName}
                  </MenuItem>
                )),
              ])}
            </TextField>

            <TextField
              size="small"
              type="date"
              label="From"
              value={fromDate}
              onChange={(e) => setFromDate(e.target.value)}
              InputLabelProps={{ shrink: true }}
              sx={{ minWidth: 160 }}
            />
            <TextField
              size="small"
              type="date"
              label="To"
              value={toDate}
              onChange={(e) => setToDate(e.target.value)}
              InputLabelProps={{ shrink: true }}
              sx={{ minWidth: 160 }}
            />
          </Stack>
        </CardContent>
      </Card>

      {error && <Alert severity="error" sx={{ mb: 3, borderRadius: 2 }}>{error}</Alert>}

      {loading ? (
        <Stack spacing={2}>
          {Array.from({ length: 6 }).map((_, i) => (
            <Skeleton key={i} variant="rounded" height={104} sx={{ borderRadius: 3 }} />
          ))}
        </Stack>
      ) : entries.length === 0 ? (
        <EmptyState
          title={hasFilters ? "No matching activity." : "No activity yet."}
          description={
            hasFilters
              ? "Try a different search term, action or date range."
              : "Your actions will appear here as you use LearnPath."
          }
          icon={<HistoryRounded sx={{ fontSize: 52 }} />}
        />
      ) : (
        <>
          <Typography variant="caption" color="text.secondary" sx={{ mb: 1.5, display: "block" }}>
            {totalCount} {totalCount === 1 ? "entry" : "entries"}
          </Typography>

          <Box>
            {entries.map((entry, i) => (
              <TimelineEntry
                key={entry.id}
                entry={entry}
                isLast={i === entries.length - 1}
              />
            ))}
          </Box>

          <PaginationBar page={page} totalPages={totalPages} onChange={setPage} />
        </>
      )}
    </Box>
  );
};

export default HistoryPage;
