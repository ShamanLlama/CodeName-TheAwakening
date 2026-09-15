import type { Difficulty } from "../game/types";

export interface DifficultyDefinition {
  id: Difficulty;
  name: string;
  influenceTarget: number;
  suppressionMultiplier: number;
  eventPressureMultiplier: number;
  pressureModifier: number;
}

export const DIFFICULTIES: Record<Difficulty, DifficultyDefinition> = {
  easy: { id: "easy", name: "Easy", influenceTarget: 60, suppressionMultiplier: 0.8, eventPressureMultiplier: 0.85, pressureModifier: -5 },
  normal: { id: "normal", name: "Normal", influenceTarget: 70, suppressionMultiplier: 1, eventPressureMultiplier: 1, pressureModifier: 0 },
  hard: { id: "hard", name: "Hard", influenceTarget: 80, suppressionMultiplier: 1.2, eventPressureMultiplier: 1.2, pressureModifier: 10 },
};
