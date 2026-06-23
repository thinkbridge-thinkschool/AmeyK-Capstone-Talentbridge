export interface Job {
  id: string;
  title: string;
  description: string;
  companyId: string;
  postedByHRId: string;
  salaryMin: number;
  salaryMax: number;
  location: string;
  status: string;
  createdAtUtc: string;
  publishedAtUtc?: string;
  expiresAtUtc?: string;
  requiredSkills?: string[];
  employmentType?: string;
  experienceLevel?: string;
  companyName?: string;
  applicationDeadline?: string;
}

export interface JobSearchResult {
  items: Job[];
  totalCount: number;
}

const STATUS_MAP: Record<number, string> = {
  0: 'Draft', 1: 'Active', 2: 'Closed', 3: 'Expired'
};

// eslint-disable-next-line @typescript-eslint/no-explicit-any
export function normalizeJobStatus(status: any): string {
  if (typeof status === 'string') return status;
  return STATUS_MAP[status as number] ?? String(status);
}

export interface PostJobRequest {
  title: string;
  description: string;
  companyId: string;
  postedByHRId: string;
  salaryMin: number;
  salaryMax: number;
  location: string;
}
