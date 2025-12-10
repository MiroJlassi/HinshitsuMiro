// Analysis Results Models

export interface AnalysisSummary {
  requestId: string;
  userId: string;
  completedAt: string;
  performance: PerformanceSummary | null;
  security: SecuritySummary | null;
  uiUx: UiUxSummary | null;
  pdfReportUrl: string | null;
}

// Performance Models
export interface PerformanceSummary {
  lighthouseScore: LighthouseScore | null;
  performance: CategoryDetail | null;
  accessibility: CategoryDetail | null;
  seo: CategoryDetail | null;
  bestPractices: CategoryDetail | null;
  diagnostics: CategoryDetail | null;
}

export interface LighthouseScore {
  performance: number;
  accessibility: number;
  seo: number;
  bestPractices: number;
}

export interface CategoryDetail {
  summary: string;
  scoreInterpretation: string;
  priority: 'low' | 'medium' | 'high';
}

// Security Models
export interface SecuritySummary {
  structuredAnalysis: StructuredAnalysis | null;
  securityHeaders: { [key: string]: SecurityHeader } | null;
}

export interface StructuredAnalysis {
  summary: SecuritySummaryDetail | null;
  issuesAndRisks: SecurityIssue[] | null;
  recommendations: string[] | null;
}

export interface SecuritySummaryDetail {
  grade: string;
  riskLevel: string;
  configuredHeaders: number;
  missingHeaders: number;
  overallAssessment: string;
}

export interface SecurityIssue {
  header: string;
  issue: string;
  risk: string;
  attackVectors: string[] | null;
}

export interface SecurityHeader {
  present: boolean;
  value: string | null;
  status: 'configured' | 'missing';
}

// UI/UX Models
export interface UiUxSummary {
  links: LinkData[] | null;
  responsiveness: ResponsivenessAnalysis | null;
  grammar: GrammarAnalysis | null;
  overallScore: string | null;
  criticalIssues: string[] | null;
  recommendations: string[] | null;
  encouragement: string | null;
}

export interface LinkData {
  text: string | null;
  href: string | null;
  title: string | null;
}

export interface ResponsivenessAnalysis {
  desktop: ViewportAnalysis | null;
  tablet: ViewportAnalysis | null;
  mobile: ViewportAnalysis | null;
}

export interface ViewportAnalysis {
  scores: ViewportScores | null;
  issues: string[] | null;
  recommendations: string[] | null;
}

export interface ViewportScores {
  accessibility: number;
  semanticHTML: number;
  navigation: number;
  responsiveness: number;
  overall: number;
}

export interface GrammarAnalysis {
  score: number;
  summary: string | null;
  issues: GrammarIssue[] | null;
}

export interface GrammarIssue {
  text: string;
  type: string;
  suggestion: string;
}

// Header tooltips for security section
export const SECURITY_HEADER_TOOLTIPS: { [key: string]: string } = {
  'Strict-Transport-Security': 'HSTS ensures that browsers only connect via HTTPS, preventing man-in-the-middle attacks.',
  'Content-Security-Policy': 'CSP helps prevent XSS attacks by controlling which resources can be loaded.',
  'X-Frame-Options': 'Prevents clickjacking by controlling whether the page can be embedded in frames.',
  'X-Content-Type-Options': 'Prevents MIME-sniffing attacks by enforcing declared content types.',
  'Referrer-Policy': 'Controls how much referrer information is shared when navigating to other sites.',
  'Permissions-Policy': 'Controls which browser features and APIs can be used on the page.',
  'X-XSS-Protection': 'Legacy header that enables browser XSS filtering (deprecated in modern browsers).',
  'Content-Type': 'Declares the MIME type of the response to ensure proper handling.',
  'Cross-Origin-Embedder-Policy': 'Controls resource loading from other origins.',
  'Cross-Origin-Opener-Policy': 'Isolates browsing context from cross-origin windows.',
  'Cross-Origin-Resource-Policy': 'Prevents other domains from reading the response.',
};

// Lighthouse score tooltips
export const LIGHTHOUSE_TOOLTIPS: { [key: string]: string } = {
  'performance': 'Measures how quickly content loads and becomes interactive. Higher scores mean faster page loads.',
  'accessibility': 'Checks if the site is usable by people with disabilities. Includes keyboard navigation, screen readers, and color contrast.',
  'seo': 'Evaluates search engine optimization best practices. Higher scores help search engines understand and rank your content.',
  'bestPractices': 'Checks for modern web development standards, security practices, and proper resource usage.',
};
