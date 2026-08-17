export const STRUCTURE_TYPES = [
  {
    id: 'CapitalCity',
    label: 'Capital City',
    isBuildable: false,
    isPillageable: false,
    isDestructible: false,
  },
  { id: 'Castle', label: 'Castle', isBuildable: false, isPillageable: true, isDestructible: false },
  { id: 'City', label: 'City', isBuildable: false, isPillageable: true, isDestructible: false },
  { id: 'Fortification', label: 'Fortification', isBuildable: true, isPillageable: true, isDestructible: true },
  { id: 'SupplyDepot', label: 'Supply Depot', isBuildable: true, isPillageable: true, isDestructible: true },
  { id: 'Town', label: 'Town', isBuildable: false, isPillageable: true, isDestructible: true },
] as const;

export type StructureId = (typeof STRUCTURE_TYPES)[number]['id'];

export function structureLabel(id: string | null | undefined): string {
  return STRUCTURE_TYPES.find((entry) => entry.id === id)?.label ?? 'None';
}

export function defaultStructureFlags(id: string | null | undefined): {
  isBuildable: boolean;
  isPillageable: boolean;
  isDestructible: boolean;
} {
  const match = STRUCTURE_TYPES.find((entry) => entry.id === id);
  return {
    isBuildable: match?.isBuildable ?? true,
    isPillageable: match?.isPillageable ?? true,
    isDestructible: match?.isDestructible ?? true,
  };
}
