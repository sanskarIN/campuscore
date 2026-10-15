import { beforeEach, describe, expect, it, vi } from 'vitest';

const capacitor = vi.hoisted(() => ({
  getPlatform: vi.fn<() => string>(),
  isNativePlatform: vi.fn<() => boolean>(),
}));

vi.mock('@capacitor/core', () => ({
  Capacitor: capacitor,
}));

import { getRuntime, isNativeRuntime, markRuntimeOnDocument } from './runtime';

describe('platform runtime boundary', () => {
  beforeEach(() => {
    capacitor.getPlatform.mockReset();
    capacitor.isNativePlatform.mockReset();
  });

  it('maps Capacitor Android to the Android runtime', () => {
    capacitor.getPlatform.mockReturnValue('android');
    expect(getRuntime()).toBe('android');
  });

  it('treats unknown Capacitor platforms as web', () => {
    capacitor.getPlatform.mockReturnValue('electron');
    expect(getRuntime()).toBe('web');
  });

  it('delegates native detection to Capacitor', () => {
    capacitor.isNativePlatform.mockReturnValue(true);
    expect(isNativeRuntime()).toBe(true);
  });

  it('marks a supplied document element without requiring a browser DOM', () => {
    capacitor.getPlatform.mockReturnValue('android');
    const toggles = new Map<string, boolean>();
    const element = {
      dataset: {},
      classList: {
        toggle: (name: string, force?: boolean) => {
          toggles.set(name, Boolean(force));
          return Boolean(force);
        },
      },
    } as unknown as HTMLElement;

    expect(markRuntimeOnDocument(element)).toBe('android');
    expect(element.dataset.runtime).toBe('android');
    expect(toggles.get('native-runtime')).toBe(true);
  });
});
