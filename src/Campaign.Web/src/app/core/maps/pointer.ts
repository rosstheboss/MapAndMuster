export function isAdditiveModifier(event: { ctrlKey: boolean; metaKey: boolean; shiftKey: boolean }): boolean {
  return event.ctrlKey || event.metaKey || event.shiftKey;
}
