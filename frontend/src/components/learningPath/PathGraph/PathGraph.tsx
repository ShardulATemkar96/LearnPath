import { useMemo } from "react";
import { Box, Chip, Tooltip, Typography } from "@mui/material";
import {
  CheckCircleRounded, LockRounded, PlayArrowRounded,
} from "@mui/icons-material";
import { Module, ModuleDependency } from "../../../types/path.types";
import { graphUtils, GraphNode } from "../../../utils/graphUtils";

interface PathGraphProps {
  modules: Module[];
  dependencies: ModuleDependency[];
}

const NODE_W  = 160;
const NODE_H  = 70;
const COL_GAP = 200;
const ROW_GAP = 100;

const PathGraph = ({ modules, dependencies }: PathGraphProps) => {
  const nodes = useMemo(
    () => graphUtils.assignLevels(modules, dependencies),
    [modules, dependencies]
  );

  const edges = useMemo(
    () => graphUtils.getEdges(dependencies),
    [dependencies]
  );

  // Group nodes by level
  const levels = useMemo(() => {
    const map = new Map<number, GraphNode[]>();
    for (const n of nodes) {
      if (!map.has(n.level)) map.set(n.level, []);
      map.get(n.level)!.push(n);
    }
    return map;
  }, [nodes]);

  // Compute x/y positions
  const positions = useMemo(() => {
    const pos = new Map<number, { x: number; y: number }>();
    for (const [lvl, lvlNodes] of levels.entries()) {
      lvlNodes.forEach((n, idx) => {
        pos.set(n.id, {
          x: lvl * COL_GAP + 40,
          y: idx * ROW_GAP + 40,
        });
      });
    }
    return pos;
  }, [levels]);

  const maxLevel = Math.max(...Array.from(levels.keys()), 0);
  const maxRows  = Math.max(...Array.from(levels.values()).map((l) => l.length), 1);

  const svgW = (maxLevel + 1) * COL_GAP + NODE_W + 40;
  const svgH = maxRows * ROW_GAP + NODE_H + 40;

  const nodeColor = (node: GraphNode) => {
    if (node.isCompleted) return { bg: "#22C55E", border: "#16A34A", icon: "#fff" };
    if (node.isUnlocked)  return { bg: "#6C63FF", border: "#4B44CC", icon: "#fff" };
    return { bg: "#F3F4F6", border: "#D1D5DB", icon: "#9CA3AF" };
  };

  if (modules.length === 0) {
    return (
      <Box sx={{ py: 6, textAlign: "center" }}>
        <Typography variant="body2" color="text.secondary">
          No modules to display.
        </Typography>
      </Box>
    );
  }

  return (
    <Box sx={{ overflowX: "auto", overflowY: "auto", maxHeight: 500 }}>
      <svg
        width={svgW}
        height={svgH}
        style={{ display: "block" }}
      >
        <defs>
          <marker
            id="arrowhead"
            markerWidth="8"
            markerHeight="6"
            refX="8"
            refY="3"
            orient="auto"
          >
            <polygon points="0 0, 8 3, 0 6" fill="#9CA3AF" />
          </marker>
        </defs>

        {edges.map((edge, idx) => {
          const from = positions.get(edge.from);
          const to   = positions.get(edge.to);
          if (!from || !to) return null;

          const x1 = from.x + NODE_W;
          const y1 = from.y + NODE_H / 2;
          const x2 = to.x;
          const y2 = to.y + NODE_H / 2;
          const mx = (x1 + x2) / 2;

          return (
            <path
              key={idx}
              d={`M ${x1} ${y1} C ${mx} ${y1}, ${mx} ${y2}, ${x2} ${y2}`}
              fill="none"
              stroke="#D1D5DB"
              strokeWidth={2}
              markerEnd="url(#arrowhead)"
            />
          );
        })}

        {nodes.map((node) => {
          const pos    = positions.get(node.id);
          if (!pos) return null;
          const colors = nodeColor(node);

          return (
            <g key={node.id} transform={`translate(${pos.x}, ${pos.y})`}>
              <rect
                x={2} y={4}
                width={NODE_W} height={NODE_H}
                rx={10}
                fill="rgba(0,0,0,0.06)"
              />
              <rect
                width={NODE_W} height={NODE_H}
                rx={10}
                fill={node.isCompleted ? "#F0FDF4" : node.isUnlocked ? "#F5F3FF" : "#F9FAFB"}
                stroke={colors.border}
                strokeWidth={1.5}
              />
              <rect
                width={4} height={NODE_H}
                rx={2}
                fill={colors.bg}
              />

              <text
                x={22} y={NODE_H / 2 + 5}
                textAnchor="middle"
                fontSize={18}
              >
                {node.isCompleted ? "✅" : node.isUnlocked ? "▶️" : "🔒"}
              </text>

              <foreignObject x={36} y={10} width={NODE_W - 44} height={NODE_H - 20}>
                <div
                  style={{
                    fontSize: 11,
                    fontWeight: 600,
                    color: node.isUnlocked || node.isCompleted ? "#1A1D2E" : "#9CA3AF",
                    lineHeight: 1.4,
                    overflow: "hidden",
                    display: "-webkit-box",
                    WebkitLineClamp: 3,
                    WebkitBoxOrient: "vertical",
                    fontFamily: "Inter, sans-serif",
                  }}
                >
                  {node.title}
                </div>
              </foreignObject>
            </g>
          );
        })}
      </svg>
    </Box>
  );
};

export default PathGraph;
