import { Component, Input, Output, EventEmitter, OnChanges } from '@angular/core';
import { CommonModule } from '@angular/common';
import { AnalysisSummary, SECURITY_HEADER_TOOLTIPS, LIGHTHOUSE_TOOLTIPS } from '../../models/analysis-results';

@Component({
  selector: 'app-results',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './results.component.html',
  styleUrl: './results.component.css'
})
export class ResultsComponent implements OnChanges {
  @Input() results: AnalysisSummary | null = null;
  @Input() jobId: string | null = null;
  @Output() linkSelected = new EventEmitter<string>();
  @Output() pdfRequested = new EventEmitter<string>();

  // Collapse states for each section
  securityCollapsed = false;
  performanceCollapsed = false;
  uiuxCollapsed = false;

  ngOnChanges(): void {
    if (this.results) {
      console.log('Results component received data:', this.results);
      console.log('Has performance?', !!this.results.performance);
      console.log('Has security?', !!this.results.security);
      console.log('Has uiUx?', !!this.results.uiUx);
    }
  }

  toggleSection(section: 'security' | 'performance' | 'uiux'): void {
    if (section === 'security') this.securityCollapsed = !this.securityCollapsed;
    if (section === 'performance') this.performanceCollapsed = !this.performanceCollapsed;
    if (section === 'uiux') this.uiuxCollapsed = !this.uiuxCollapsed;
  }

  downloadPdf(): void {
    if (this.results?.pdfReportUrl) {
      window.open(this.results.pdfReportUrl, '_blank');
    }
  }

  generatePdfReport(): void {
    if (this.jobId) {
      this.pdfRequested.emit(this.jobId);
    }
  }

  analyzeLink(link: string): void {
    this.linkSelected.emit(link);
  }

  getScoreColor(score: number, max: number = 1): string {
    const normalized = score / max;
    if (normalized >= 0.9) return '#4caf50'; // Green
    if (normalized >= 0.5) return '#ff9800'; // Orange
    return '#f44336'; // Red
  }

  getPriorityColor(priority: string): string {
    switch (priority) {
      case 'low': return '#4caf50';
      case 'medium': return '#ff9800';
      case 'high': return '#f44336';
      default: return '#666';
    }
  }

  getGradeColor(grade: string): string {
    if (['A', 'A+'].includes(grade)) return '#4caf50';
    if (['B', 'B+', 'B-'].includes(grade)) return '#8bc34a';
    if (['C', 'C+', 'C-'].includes(grade)) return '#ff9800';
    if (['D', 'D+', 'D-'].includes(grade)) return '#ff5722';
    return '#f44336'; // F
  }

  getRiskLevelColor(riskLevel: string): string {
    switch (riskLevel.toLowerCase()) {
      case 'low': return '#4caf50';
      case 'moderate': return '#ff9800';
      case 'high': return '#ff5722';
      case 'critical': return '#f44336';
      default: return '#666';
    }
  }

  getViewportScoreColor(score: number): string {
    if (score >= 8) return '#4caf50';
    if (score >= 5) return '#ff9800';
    return '#f44336';
  }

  getHeaderTooltip(headerName: string): string {
    return SECURITY_HEADER_TOOLTIPS[headerName] || 'Security header configuration';
  }

  getLighthouseTooltip(metric: string): string {
    return LIGHTHOUSE_TOOLTIPS[metric] || '';
  }

  getPerformanceCategories(): Array<{name: string, data: any}> {
    if (!this.results?.performance) return [];
    const categories = [
      { name: 'Performance', data: this.results.performance.performance },
      { name: 'Accessibility', data: this.results.performance.accessibility },
      { name: 'SEO', data: this.results.performance.seo },
      { name: 'Best Practices', data: this.results.performance.bestPractices },
      { name: 'Diagnostics', data: this.results.performance.diagnostics },
    ];
    // Filter out null/undefined categories
    return categories.filter(cat => cat.data != null);
  }

  getViewports(): Array<{name: string, data: any}> {
    if (!this.results?.uiUx?.responsiveness) return [];
    const viewports = [
      { name: '🖥️ Desktop', data: this.results.uiUx.responsiveness.desktop },
      { name: '📱 Tablet', data: this.results.uiUx.responsiveness.tablet },
      { name: '📱 Mobile', data: this.results.uiUx.responsiveness.mobile },
    ];
    // Filter out null/undefined viewports
    return viewports.filter(vp => vp.data != null);
  }

  hasAnyResults(): boolean {
    return !!(this.results?.performance || this.results?.security || this.results?.uiUx);
  }

  getAvailableResultsCount(): number {
    let count = 0;
    if (this.results?.performance) count++;
    if (this.results?.security) count++;
    if (this.results?.uiUx) count++;
    return count;
  }

  getFailedTasksMessage(): string {
    const failed: string[] = [];
    if (!this.results?.performance) failed.push('Performance');
    if (!this.results?.security) failed.push('Security');
    if (!this.results?.uiUx) failed.push('UI/UX');
    
    if (failed.length === 0) return '';
    if (failed.length === 3) return 'All analysis tasks failed';
    
    return `${failed.join(', ')} analysis ${failed.length === 1 ? 'was' : 'were'} not available`;
  }
}
