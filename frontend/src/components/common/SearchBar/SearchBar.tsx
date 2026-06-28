import { InputAdornment, TextField } from "@mui/material";
import { SearchRounded, CloseRounded } from "@mui/icons-material";
import { IconButton } from "@mui/material";

interface SearchBarProps {
  value: string;
  onChange: (value: string) => void;
  placeholder?: string;
  fullWidth?: boolean;
}

const SearchBar = ({
  value, onChange,
  placeholder = "Search...",
  fullWidth = false,
}: SearchBarProps) => (
  <TextField
    size="small"
    value={value}
    onChange={(e) => onChange(e.target.value)}
    placeholder={placeholder}
    fullWidth={fullWidth}
    InputProps={{
      startAdornment: (
        <InputAdornment position="start">
          <SearchRounded sx={{ color: "text.secondary", fontSize: 20 }} />
        </InputAdornment>
      ),
      endAdornment: value ? (
        <InputAdornment position="end">
          <IconButton size="small" onClick={() => onChange("")} edge="end">
            <CloseRounded sx={{ fontSize: 16 }} />
          </IconButton>
        </InputAdornment>
      ) : undefined,
    }}
    sx={{
      "& .MuiOutlinedInput-root": {
        borderRadius: 3,
        bgcolor: "background.default",
        fontSize: "0.875rem",
      },
    }}
  />
);

export default SearchBar;
