import { describe, expect, it } from 'vitest';

import {
  UPDATE_STREAM_RECONNECT_MAX_MS,
  UPDATE_STREAM_RECONNECT_MIN_MS,
  nextReconnectDelayMs,
  parseUpdateEvent,
  updateStreamStatusDetail,
  updateStreamStatusLabel,
} from './update-stream';

describe('parseUpdateEvent', () => {
  it('reads the revision from a well-formed frame', () => {
    expect(parseUpdateEvent('{"revision":42}')).toEqual({ revision: 42 });
  });

  it('reports revision 0 for a frame with no revision, so the page still refreshes', () => {
    expect(parseUpdateEvent('{}')).toEqual({ revision: 0 });
  });

  it('reports revision 0 for malformed JSON rather than throwing', () => {
    expect(parseUpdateEvent('not json')).toEqual({ revision: 0 });
  });

  it('rejects a non-numeric revision', () => {
    expect(parseUpdateEvent('{"revision":"12"}')).toEqual({ revision: 0 });
  });
});

describe('updateStreamStatusLabel', () => {
  it('uses Live, Connecting, and Reconnecting', () => {
    expect(updateStreamStatusLabel('open')).toBe('Live');
    expect(updateStreamStatusLabel('connecting')).toBe('Connecting');
    expect(updateStreamStatusLabel('disconnected')).toBe('Reconnecting');
  });

  it('explains the disconnected fallback in the detail text', () => {
    expect(updateStreamStatusDetail('disconnected')).toContain('every minute');
  });
});

describe('nextReconnectDelayMs', () => {
  it('doubles the delay', () => {
    expect(nextReconnectDelayMs(UPDATE_STREAM_RECONNECT_MIN_MS)).toBe(UPDATE_STREAM_RECONNECT_MIN_MS * 2);
  });

  it('caps the delay so a down backend is not hammered', () => {
    expect(nextReconnectDelayMs(UPDATE_STREAM_RECONNECT_MAX_MS)).toBe(UPDATE_STREAM_RECONNECT_MAX_MS);
    expect(nextReconnectDelayMs(UPDATE_STREAM_RECONNECT_MAX_MS * 4)).toBe(UPDATE_STREAM_RECONNECT_MAX_MS);
  });
});
