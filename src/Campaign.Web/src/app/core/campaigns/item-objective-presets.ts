export type ItemObjectivePlacement = 'Random' | 'Placed';

export interface ItemObjectivePresetItem {
  name: string;
  isHiddenUntilFound: boolean;
  placement: ItemObjectivePlacement;
  allowOnSpawn: boolean;
}

export interface ItemObjectivePreset {
  id: string;
  name: string;
  items: readonly ItemObjectivePresetItem[];
}

export const HUNT_IN_ESTALIA_ITEM_OBJECTIVES_PRESET_ID = 'the-hunt-in-estalia-item-objectives';

export const ITEM_OBJECTIVE_PRESETS: readonly ItemObjectivePreset[] = [
  {
    id: HUNT_IN_ESTALIA_ITEM_OBJECTIVES_PRESET_ID,
    name: 'The Hunt in Estalia',
    items: [],
  },
];

export function itemObjectivesFromPreset(presetId: string): ItemObjectivePresetItem[] | null {
  const preset = ITEM_OBJECTIVE_PRESETS.find((entry) => entry.id === presetId);
  if (!preset) {
    return null;
  }

  return preset.items.map((item) => ({
    name: item.name,
    isHiddenUntilFound: item.isHiddenUntilFound,
    placement: item.placement,
    allowOnSpawn: item.allowOnSpawn,
  }));
}

export function defaultItemObjective(): ItemObjectivePresetItem {
  return {
    name: '',
    isHiddenUntilFound: true,
    placement: 'Random',
    allowOnSpawn: false,
  };
}
