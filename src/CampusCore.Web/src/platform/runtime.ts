import { Capacitor } from '@capacitor/core';

export type CampusCoreRuntime = 'android' | 'ios' | 'web';

export const getRuntime = (): CampusCoreRuntime => {
  const platform = Capacitor.getPlatform();
  if (platform === 'android' || platform === 'ios') return platform;
  return 'web';
};

export const isNativeRuntime = (): boolean => Capacitor.isNativePlatform();

export const markRuntimeOnDocument = (documentElement: HTMLElement = document.documentElement): CampusCoreRuntime => {
  const runtime = getRuntime();
  documentElement.dataset.runtime = runtime;
  documentElement.classList.toggle('native-runtime', runtime !== 'web');
  return runtime;
};
