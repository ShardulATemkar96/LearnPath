tsimport { Module, ModuleDependency } from "../types/path.types";

export interface GraphNode {
  id: number;
  title: string;
  isCompleted: boolean;
  isUnlocked: boolean;
  contentType: string;
  level: number;
}

export interface GraphEdge {
  from: number;
  to: number;
}

export const graphUtils = {
  buildAdjacency: (
    dependencies: ModuleDependency[]
  ): Map<number, number[]> => {
    const adj = new Map<number, number[]>();
    for (const dep of dependencies) {
      if (!adj.has(dep.moduleId)) adj.set(dep.moduleId, []);
      adj.get(dep.moduleId)!.push(dep.dependsOnModuleId);
    }
    return adj;
  },

  assignLevels: (
    modules: Module[],
    dependencies: ModuleDependency[]
  ): GraphNode[] => {
    const adj       = graphUtils.buildAdjacency(dependencies);
    const inDegree  = new Map<number, number>();
    const children  = new Map<number, number[]>();

    for (const m of modules) {
      inDegree.set(m.id, 0);
      children.set(m.id, []);
    }

    for (const dep of dependencies) {
      inDegree.set(dep.moduleId, (inDegree.get(dep.moduleId) ?? 0) + 1);
      children.get(dep.dependsOnModuleId)?.push(dep.moduleId);
    }

    const level  = new Map<number, number>();
    const queue  = modules
      .filter((m) => (inDegree.get(m.id) ?? 0) === 0)
      .map((m) => m.id);

    for (const id of queue) level.set(id, 0);

    let head = 0;
    while (head < queue.length) {
      const cur = queue[head++];
      for (const child of children.get(cur) ?? []) {
        const newLevel = (level.get(cur) ?? 0) + 1;
        if (!level.has(child) || level.get(child)! < newLevel)
          level.set(child, newLevel);
        queue.push(child);
      }
    }

    return modules.map((m) => ({
      id:          m.id,
      title:       m.title,
      isCompleted: m.isCompleted,
      isUnlocked:  m.isUnlocked,
      contentType: m.contentType,
      level:       level.get(m.id) ?? 0,
    }));
  },

  getEdges: (dependencies: ModuleDependency[]): GraphEdge[] =>
    dependencies.map((d) => ({
      from: d.dependsOnModuleId,
      to:   d.moduleId,
    })),

  getUnlockedModules: (
    modules: Module[],
    dependencies: ModuleDependency[],
    completedIds: Set<number>
  ): number[] => {
    const adj = graphUtils.buildAdjacency(dependencies);
    return modules
      .filter((m) => {
        const deps = adj.get(m.id) ?? [];
        return deps.every((dId) => completedIds.has(dId));
      })
      .map((m) => m.id);
  },
};
