import { useState, useCallback, useEffect } from 'react';
import type { UserSite, SiteKey } from '../types';
import { getFuntimeClient } from '../api/client';

export interface UseSitesReturn {
  sites: UserSite[];
  siteKeys: SiteKey[];
  isLoading: boolean;
  error: string | null;
  joinSite: (siteKey: SiteKey) => Promise<UserSite>;
  leaveSite: (siteKey: SiteKey) => Promise<void>;
  isMemberOf: (siteKey: SiteKey) => boolean;
  refresh: () => Promise<void>;
}

export function useSites(): UseSitesReturn {
  const [sites, setSites] = useState<UserSite[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  const refresh = useCallback(async () => {
    setIsLoading(true);
    setError(null);
    try {
      const client = getFuntimeClient();
      const data = await client.getMySites();
      setSites(data);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to load sites');
    } finally {
      setIsLoading(false);
    }
  }, []);

  const joinSite = useCallback(async (siteKey: SiteKey): Promise<UserSite> => {
    const client = getFuntimeClient();
    const site = await client.joinSite(siteKey);
    setSites(prev => [...prev.filter(s => s.siteKey !== siteKey), site]);
    return site;
  }, []);

  const leaveSite = useCallback(async (siteKey: SiteKey): Promise<void> => {
    const client = getFuntimeClient();
    await client.leaveSite(siteKey);
    setSites(prev => prev.map(s =>
      s.siteKey === siteKey ? { ...s, isActive: false } : s
    ));
  }, []);

  const isMemberOf = useCallback((siteKey: SiteKey): boolean => {
    return sites.some(s => s.siteKey === siteKey && s.isActive);
  }, [sites]);

  useEffect(() => {
    refresh();
  }, [refresh]);

  const siteKeys = sites.filter(s => s.isActive).map(s => s.siteKey as SiteKey);

  return {
    sites,
    siteKeys,
    isLoading,
    error,
    joinSite,
    leaveSite,
    isMemberOf,
    refresh,
  };
}
