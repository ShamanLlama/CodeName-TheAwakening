export function seedToState(seed: string): number {
  let hash = 2166136261;
  for (let index = 0; index < seed.length; index += 1) {
    hash ^= seed.charCodeAt(index);
    hash = Math.imul(hash, 16777619);
  }
  return hash >>> 0 || 1;
}

export function nextRandom(state: number): { value: number; state: number } {
  let next = state >>> 0 || 1;
  next ^= next << 13;
  next ^= next >>> 17;
  next ^= next << 5;
  const unsigned = next >>> 0;
  return { value: unsigned / 4294967296, state: unsigned || 1 };
}

export function randomInt(state: number, minimum: number, maximum: number): { value: number; state: number } {
  const random = nextRandom(state);
  return { value: Math.floor(random.value * (maximum - minimum + 1)) + minimum, state: random.state };
}
