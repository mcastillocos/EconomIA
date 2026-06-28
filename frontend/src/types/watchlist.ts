export interface Watchlist {
  id: string;
  name: string;
  description?: string;
  createdAt: string;
  updatedAt: string;
  items: WatchlistItem[];
}

export interface WatchlistItem {
  id: string;
  watchlistId: string;
  entityType: 'fund' | 'company';
  entityId: string;
  priority: number;
  positionType: 'real' | 'watch';
  thesis?: string;
  notes?: string;
  createdAt: string;
}

export interface CreateWatchlistRequest {
  name: string;
  description?: string;
}

export interface AddWatchlistItemRequest {
  entityType: 'fund' | 'company';
  entityId: string;
  priority?: number;
  positionType?: 'real' | 'watch';
  thesis?: string;
  notes?: string;
}
