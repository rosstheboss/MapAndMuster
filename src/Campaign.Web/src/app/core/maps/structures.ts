export const STRUCTURE_TYPES = [
  { id: 'CapitalCity', label: 'Capital City' },
  { id: 'Castle', label: 'Castle' },
  { id: 'City', label: 'City' },
  { id: 'Fortification', label: 'Fortification' },
  { id: 'SupplyDepot', label: 'Supply Depot' },
  { id: 'Town', label: 'Town' },
] as const;

export type StructureId = (typeof STRUCTURE_TYPES)[number]['id'];

export function structureLabel(id: string | null | undefined): string {
  return STRUCTURE_TYPES.find((entry) => entry.id === id)?.label ?? 'None';
}
