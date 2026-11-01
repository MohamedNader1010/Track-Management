

export type TrackStatus = 'draft' | 'submitted' | 'distributed';
export type DistributionStatus = 'pending' | 'live' | 'rejected';

export const TRACK_STATUSES: readonly TrackStatus[] = ['draft', 'submitted', 'distributed'];

export const GENRES: readonly string[] = [
  'Arabic Pop',
  'Khaleeji',
  'Mahraganat',
  'Rai',
  'Tarab',
  'Pop',
  'Hip-Hop',
  'R&B',
  'Rock',
  'Electronic',
  'Jazz',
  'Classical',
  'Indie',
];

export interface CreateArtistRequest {
  name: string;
  email: string;
  country: string;
}

export interface CreateTrackRequest {
  title: string;
  artistId: string;
  isrc: string;
  releaseDate: string; // yyyy-MM-dd
  genre: string;
}

export interface PagedResponse<T> {
  items: T[];
  page: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
  hasNextPage: boolean;
}

export interface Artist {
  id: string;
  name: string;
  email: string;
  country: string;
  createdAtUtc: string;
}

export interface Dsp {
  id: string;
  name: string;
}

export interface TrackSummary {
  id: string;
  title: string;
  isrc: string;
  artistId: string;
  artistName: string;
  genre: string;
  status: TrackStatus;
  releaseDate: string;
  distributionCount: number;
}

export interface TrackDistribution {
  id: string;
  dspId: string;
  dspName: string;
  status: DistributionStatus;
  submittedAtUtc: string;
}

export interface TrackDetails {
  id: string;
  title: string;
  isrc: string;
  artist: { id: string; name: string; country: string };
  genre: string;
  status: TrackStatus;
  releaseDate: string;
  createdAtUtc: string;
  updatedAtUtc: string;
  allowedStatusTransitions: TrackStatus[];
  distributions: TrackDistribution[];
}

export interface TokenResponse {
  accessToken: string;
  tokenType: string;
  expiresAtUtc: string;
}

export interface ProblemDetails {
  type?: string;
  title?: string;
  status?: number;
  detail?: string;
  errorCode?: string;
  traceId?: string;
  errors?: Record<string, string[]>;
}
