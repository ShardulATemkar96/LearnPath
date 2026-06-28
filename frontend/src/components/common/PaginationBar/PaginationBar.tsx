import { Box, Pagination } from "@mui/material";

interface PaginationBarProps {
  page: number;
  totalPages: number;
  onChange: (page: number) => void;
}

const PaginationBar = ({ page, totalPages, onChange }: PaginationBarProps) => {
  if (totalPages <= 1) return null;

  return (
    <Box sx={{ display: "flex", justifyContent: "center", mt: 4 }}>
      <Pagination
        count={totalPages}
        page={page}
        onChange={(_, p) => onChange(p)}
        color="primary"
        shape="rounded"
        sx={{
          "& .MuiPaginationItem-root": {
            borderRadius: 2,
            fontWeight: 500,
          },
          "& .Mui-selected": {
            background:
              "linear-gradient(135deg, #6C63FF, #9D97FF) !important",
            color: "#fff",
          },
        }}
      />
    </Box>
  );
};

export default PaginationBar;
