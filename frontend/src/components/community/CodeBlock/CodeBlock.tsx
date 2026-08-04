import { Box, Typography } from "@mui/material";
import { Highlight, themes } from "prism-react-renderer";

interface CodeBlockProps {
  code: string;
  language?: string | null;
}

const CodeBlock = ({ code, language }: CodeBlockProps) => (
  <Box
    component="pre"
    sx={{
      m: 0, p: 2.5, borderRadius: 3, overflowX: "auto",
      bgcolor: "#1E1E2E", border: "1px solid", borderColor: "divider",
    }}
  >
    <Typography
      component="div" variant="caption" fontWeight={700}
      sx={{ mb: 1.5, color: "rgba(255,255,255,0.6)", textTransform: "uppercase" }}
    >
      {language ?? "code"}
    </Typography>
    <Highlight
      theme={themes.nightOwl}
      code={code}
      language={language ?? "text"}
    >
      {({ style, tokens, getLineProps, getTokenProps }) => (
        <Box
          component="div"
          sx={{ fontSize: "0.85rem", lineHeight: 1.7, ...style }}
        >
          {tokens.map((line, i) => (
            <Box key={i} component="div" {...getLineProps({ line })}>
              {line.map((token, key) => (
                <Box key={key} component="span" {...getTokenProps({ token })} />
              ))}
            </Box>
          ))}
        </Box>
      )}
    </Highlight>
  </Box>
);

export default CodeBlock;
