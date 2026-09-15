import type { Difficulty } from "../game/types";
export const DIFFICULTIES: Record<Difficulty, { name: string; recruitment: number; pressure: number; warning: number }> = {
  easy: { name: "Easy", recruitment: 1.2, pressure: 0.75, warning: 2 },
  normal: { name: "Normal", recruitment: 1, pressure: 1, warning: 0 },
  hard: { name: "Hard", recruitment: 0.85, pressure: 1.25, warning: -1 },
};
