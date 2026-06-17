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
}

export interface JobSearchResult {
  items: Job[];
  totalCount: number;
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
