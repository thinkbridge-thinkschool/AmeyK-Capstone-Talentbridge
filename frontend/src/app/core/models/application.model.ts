export interface JobApplication {
  id: string;
  candidateId: string;
  jobId: string;
  coverLetter: string;
  resumeUrl: string;
  status: string;
  submittedAtUtc: string;
  lastUpdatedAtUtc: string;
}

export interface ApplyResponse {
  applicationId: string;
  status: string;
}

export interface ApplyRequest {
  candidateId: string;
  jobId: string;
  coverLetter: string;
  resumeUrl: string;
}

export type ApplicationAction = 'StartReview' | 'Shortlist' | 'Accept' | 'Reject' | 'Withdraw';
