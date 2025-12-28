import { describe, it, expect, vi, beforeEach } from 'vitest';
import { renderHook, act, waitFor } from '@testing-library/react';
import { useSites } from './useSites';

// Mock the API client
vi.mock('../api/client', () => ({
  getFuntimeClient: vi.fn(() => mockClient),
}));

const mockClient = {
  getMySites: vi.fn(),
  joinSite: vi.fn(),
  leaveSite: vi.fn(),
};

const mockSites = [
  { userSiteId: 1, userId: 1, siteKey: 'community', role: 'member', isActive: true, joinedAt: '2024-01-01' },
  { userSiteId: 2, userId: 1, siteKey: 'college', role: 'member', isActive: true, joinedAt: '2024-01-01' },
  { userSiteId: 3, userId: 1, siteKey: 'date', role: 'member', isActive: false, joinedAt: '2024-01-01' },
];

describe('useSites', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    mockClient.getMySites.mockResolvedValue(mockSites);
  });

  it('initializes with loading state', () => {
    const { result } = renderHook(() => useSites());

    expect(result.current.isLoading).toBe(true);
    expect(result.current.sites).toEqual([]);
    expect(result.current.error).toBe(null);
  });

  it('loads sites on mount', async () => {
    const { result } = renderHook(() => useSites());

    await waitFor(() => {
      expect(result.current.isLoading).toBe(false);
    });

    expect(result.current.sites).toEqual(mockSites);
    expect(mockClient.getMySites).toHaveBeenCalled();
  });

  it('returns only active site keys', async () => {
    const { result } = renderHook(() => useSites());

    await waitFor(() => {
      expect(result.current.isLoading).toBe(false);
    });

    expect(result.current.siteKeys).toEqual(['community', 'college']);
    expect(result.current.siteKeys).not.toContain('date');
  });

  it('handles load error', async () => {
    mockClient.getMySites.mockRejectedValue(new Error('Network error'));

    const { result } = renderHook(() => useSites());

    await waitFor(() => {
      expect(result.current.isLoading).toBe(false);
    });

    expect(result.current.error).toBe('Network error');
    expect(result.current.sites).toEqual([]);
  });

  it('checks membership correctly', async () => {
    const { result } = renderHook(() => useSites());

    await waitFor(() => {
      expect(result.current.isLoading).toBe(false);
    });

    expect(result.current.isMemberOf('community')).toBe(true);
    expect(result.current.isMemberOf('college')).toBe(true);
    expect(result.current.isMemberOf('date')).toBe(false); // inactive
    expect(result.current.isMemberOf('jobs')).toBe(false); // not joined
  });

  it('joins a site', async () => {
    const newSite = { userSiteId: 4, userId: 1, siteKey: 'jobs', role: 'member', isActive: true, joinedAt: '2024-01-15' };
    mockClient.joinSite.mockResolvedValue(newSite);

    const { result } = renderHook(() => useSites());

    await waitFor(() => {
      expect(result.current.isLoading).toBe(false);
    });

    let joinedSite;
    await act(async () => {
      joinedSite = await result.current.joinSite('jobs');
    });

    expect(mockClient.joinSite).toHaveBeenCalledWith('jobs');
    expect(joinedSite).toEqual(newSite);
    expect(result.current.sites).toContainEqual(newSite);
  });

  it('updates existing site when rejoining', async () => {
    const rejoinedSite = { userSiteId: 3, userId: 1, siteKey: 'date', role: 'member', isActive: true, joinedAt: '2024-01-15' };
    mockClient.joinSite.mockResolvedValue(rejoinedSite);

    const { result } = renderHook(() => useSites());

    await waitFor(() => {
      expect(result.current.isLoading).toBe(false);
    });

    await act(async () => {
      await result.current.joinSite('date');
    });

    // Should replace the old entry, not add a duplicate
    const dateSites = result.current.sites.filter(s => s.siteKey === 'date');
    expect(dateSites).toHaveLength(1);
    expect(dateSites[0].isActive).toBe(true);
  });

  it('leaves a site', async () => {
    mockClient.leaveSite.mockResolvedValue(undefined);

    const { result } = renderHook(() => useSites());

    await waitFor(() => {
      expect(result.current.isLoading).toBe(false);
    });

    await act(async () => {
      await result.current.leaveSite('community');
    });

    expect(mockClient.leaveSite).toHaveBeenCalledWith('community');

    const communitySite = result.current.sites.find(s => s.siteKey === 'community');
    expect(communitySite?.isActive).toBe(false);
  });

  it('refreshes sites', async () => {
    const { result } = renderHook(() => useSites());

    await waitFor(() => {
      expect(result.current.isLoading).toBe(false);
    });

    const updatedSites = [
      ...mockSites,
      { userSiteId: 4, userId: 1, siteKey: 'jobs', role: 'admin', isActive: true, joinedAt: '2024-01-15' },
    ];
    mockClient.getMySites.mockResolvedValue(updatedSites);

    await act(async () => {
      await result.current.refresh();
    });

    expect(result.current.sites).toEqual(updatedSites);
    expect(mockClient.getMySites).toHaveBeenCalledTimes(2);
  });
});
