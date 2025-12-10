import { Component, OnInit, OnDestroy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { AuthService } from '../../services/auth.service';
import { HttpClient } from '@angular/common/http';
import { SignalRService, TaskProgressNotification, JobCompletionNotification } from '../../services/signalr.service';
import { Subscription } from 'rxjs';
import { ResultsComponent } from '../results/results.component';
import { AnalysisSummary } from '../../models/analysis-results';
import { environment } from '../../../environments/environment';

interface Message {
  content: string;
  displayContent?: string;
  isUser: boolean;
  timestamp: Date;
  isTyping?: boolean;
}

interface TaskProgress {
  security: string;
  performance: string;
  uiux: string;
}

interface TaskTiming {
  security: number;
  performance: number;
  uiux: number;
}

@Component({
  selector: 'app-home',
  standalone: true,
  imports: [CommonModule, FormsModule, ResultsComponent],
  templateUrl: './home.component.html',
  styleUrl: './home.component.css'
})
export class HomeComponent implements OnInit, OnDestroy {
  userMessage = '';
  userName = 'John Doe';
  userInitials = 'JD';
  messages: Message[] = [];
  isLoading = false;
  private readonly apiUrl = `${environment.apiUrl}/api`;
  
  // SignalR and Task Tracking
  currentJobId: string | null = null;
  taskProgress: TaskProgress = {
    security: 'Pending',
    performance: 'Pending',
    uiux: 'Pending'
  };
  taskTimings: TaskTiming = {
    security: 0,
    performance: 0,
    uiux: 0
  };
  taskStartTimes: Map<string, number> = new Map();
  totalProcessingTime: number = 0;
  jobStartTime: number = 0;
  isJobComplete = false;
  isSignalRConnected = false;
  analysisResults: AnalysisSummary | null = null;
  
  private readonly signalRSubscriptions: Subscription[] = [];
  private readonly typingIntervals: Map<number, any> = new Map();

  constructor(
    private readonly authService: AuthService,
    private readonly router: Router,
    private readonly http: HttpClient,
    private readonly signalRService: SignalRService
  ) {
    this.userInitials = this.userName
      .split(' ')
      .map(n => n[0])
      .join('')
      .toUpperCase()
      .substring(0, 2);
  }

  ngOnInit(): void {
    this.initializeSignalR();
  }

  private async initializeSignalR(): Promise<void> {
    try {
      // Connect to SignalR
      await this.signalRService.connect();
      
      // Subscribe to all observables
      this.signalRSubscriptions.push(
        this.signalRService.isConnected$.subscribe(connected => {
          this.isSignalRConnected = connected;
        }),
        this.signalRService.taskProgress$.subscribe(notification => {
          if (notification) {
            this.handleTaskProgress(notification);
          }
        }),
        this.signalRService.jobCompleted$.subscribe(notification => {
          if (notification) {
            this.handleJobCompleted(notification);
          }
        })
      );
      
      // Join user group
      await this.signalRService.joinUserGroup('user123');
    } catch (error) {
      console.error('Failed to initialize SignalR:', error);
    }
  }

  ngOnDestroy(): void {
    // Unsubscribe from all subscriptions
    for (const sub of this.signalRSubscriptions) {
      sub.unsubscribe();
    }
    
    // Clear all typing intervals
    for (const interval of this.typingIntervals.values()) {
      clearInterval(interval);
    }
    this.typingIntervals.clear();
    
    // Leave job group if active
    if (this.currentJobId) {
      this.signalRService.leaveJobGroup(this.currentJobId);
    }
  }

  handleTaskProgress(notification: TaskProgressNotification): void {
    const taskKey = notification.taskType.toLowerCase() as keyof TaskProgress;
    this.taskProgress[taskKey] = notification.status;
    
    // Track timing
    if (notification.status === 'Started') {
      this.taskStartTimes.set(taskKey, Date.now());
    } else if (notification.status === 'Completed' || notification.status === 'Failed') {
      const startTime = this.taskStartTimes.get(taskKey);
      if (startTime) {
        const duration = (Date.now() - startTime) / 1000; // Convert to seconds
        this.taskTimings[taskKey as keyof TaskTiming] = duration;
        this.taskStartTimes.delete(taskKey);
      }
    }
  }

  addBotMessageWithTyping(content: string): void {
    const message: Message = {
      content,
      displayContent: '',
      isUser: false,
      timestamp: new Date(),
      isTyping: true
    };
    
    this.messages.push(message);
    const messageIndex = this.messages.length - 1;
    
    let charIndex = 0;
    const typingSpeed = 20; // milliseconds per character
    
    const interval = setInterval(() => {
      if (charIndex < content.length) {
        message.displayContent = content.substring(0, charIndex + 1);
        charIndex++;
      } else {
        message.isTyping = false;
        clearInterval(interval);
        this.typingIntervals.delete(messageIndex);
      }
    }, typingSpeed);
    
    this.typingIntervals.set(messageIndex, interval);
  }

  handleJobCompleted(notification: JobCompletionNotification): void {
    this.isJobComplete = true;
    this.isLoading = false;
    
    // Calculate total processing time
    if (this.jobStartTime) {
      this.totalProcessingTime = (Date.now() - this.jobStartTime) / 1000;
    }
    
    if (notification.status === 'Completed') {
      // Count successful tasks
      const tasks = notification.tasks;
      let successCount = 0;
      let failCount = 0;
      
      if (tasks.securityStatus === 'Completed') successCount++;
      else if (tasks.securityStatus === 'Failed') failCount++;
      
      if (tasks.performanceStatus === 'Completed') successCount++;
      else if (tasks.performanceStatus === 'Failed') failCount++;
      
      if (tasks.uiUxStatus === 'Completed') successCount++;
      else if (tasks.uiUxStatus === 'Failed') failCount++;
      
      const timingDetails = `Security: ${this.taskTimings.security.toFixed(2)}s, Performance: ${this.taskTimings.performance.toFixed(2)}s, UI/UX: ${this.taskTimings.uiux.toFixed(2)}s`;
      
      if (successCount === 3) {
        this.addBotMessageWithTyping(`All checks completed successfully! Total time: ${this.totalProcessingTime.toFixed(2)}s (${timingDetails})`);
      } else {
        const failedTasks: string[] = [];
        if (tasks.securityStatus === 'Failed') failedTasks.push('Security');
        if (tasks.performanceStatus === 'Failed') failedTasks.push('Performance');
        if (tasks.uiUxStatus === 'Failed') failedTasks.push('UI/UX');
        
        this.addBotMessageWithTyping(
          `Analysis completed with ${successCount} of 3 tasks successful. ` +
          `${failedTasks.join(', ')} ${failedTasks.length === 1 ? 'task' : 'tasks'} failed, but you can still view available results. ` +
          `Total time: ${this.totalProcessingTime.toFixed(2)}s (${timingDetails})`
        );
      }
      
      // Fetch analysis results (even with partial failures)
      if (this.currentJobId) {
        this.fetchAnalysisResults(this.currentJobId);
      }
    } else {
      // All tasks failed
      this.addBotMessageWithTyping('All analysis tasks failed. Please check the URL and try again, or contact support if the issue persists.');
    }
  }

  private async joinJobGroup(jobId: string): Promise<void> {
    try {
      await this.signalRService.joinJobGroup(jobId);
      this.addBotMessageWithTyping('Connected to real-time updates. You\'ll see progress as tasks complete.');
    } catch (error) {
      console.error('Failed to join job group:', error);
    }
  }

  extractUrls(text: string): string[] {
    const urlRegex = /(https?:\/\/[^\s]+)|(www\.[^\s]+)/gi;
    const matches = text.match(urlRegex);
    return matches || [];
  }

  normalizeUrl(url: string): string {
    // Add https:// if URL starts with www. but has no protocol
    if (url.startsWith('www.')) {
      return `https://${url}`;
    }
    return url;
  }

  async sendMessage(): Promise<void> {
    if (!this.userMessage.trim()) {
      return;
    }

    const messageContent = this.userMessage.trim();
    const urls = this.extractUrls(messageContent);

    // Add user message to chat
    this.messages.push({
      content: messageContent,
      isUser: true,
      timestamp: new Date()
    });

    // Validate URL count
    if (urls.length === 0) {
      this.addBotMessageWithTyping('Oh sorry, no available URL in your message. Please provide a URL to analyze.');
      this.userMessage = '';
      return;
    }

    if (urls.length > 1) {
      this.addBotMessageWithTyping('Please provide only one URL at a time.');
      this.userMessage = '';
      return;
    }

    // Normalize the URL
    const normalizedUrl = this.normalizeUrl(urls[0]);
    console.log('Sending URL to backend:', normalizedUrl);

    // Reset task progress and timings
    this.taskProgress = {
      security: 'Pending',
      performance: 'Pending',
      uiux: 'Pending'
    };
    this.taskTimings = {
      security: 0,
      performance: 0,
      uiux: 0
    };
    this.taskStartTimes.clear();
    this.totalProcessingTime = 0;
    this.jobStartTime = Date.now();
    this.isJobComplete = false;

    // Call API with single URL
    this.isLoading = true;
    this.userMessage = '';

    const payload = { Url: normalizedUrl };
    console.log('Request payload:', JSON.stringify(payload));

    this.http.post<any>(`${this.apiUrl}/Analysis/submit`, payload).subscribe({
      next: (response) => {
        console.log('Backend response:', response);
        this.addBotMessageWithTyping(response.message || 'URL submitted successfully! Starting analysis...');
        
        // Store job ID and join SignalR group
        if (response.jobId) {
          this.currentJobId = response.jobId;
          this.joinJobGroup(response.jobId);
        } else {
          console.warn('No jobId received from backend');
        }
      },
      error: (error) => {
        console.error('Backend error:', error);
        const errorMessage = error.error?.message || error.message || 'Failed to process the URL. Please try again.';
        this.addBotMessageWithTyping(errorMessage);
        this.isLoading = false;
      }
    });
  }

  logout(): void {
    this.authService.logout();
  }

  fetchAnalysisResults(jobId: string): void {
    this.http.get<AnalysisSummary>(`${this.apiUrl}/analysis/${jobId}/results`).subscribe({
      next: (results) => {
        console.log('Analysis results received:', results);
        console.log('Performance data:', results.performance);
        console.log('Security data:', results.security);
        console.log('UI/UX data:', results.uiUx);
        this.analysisResults = results;
      },
      error: (error) => {
        console.error('Failed to fetch analysis results:', error);
        this.addBotMessageWithTyping('Failed to load analysis results. Please try again.');
      }
    });
  }

  onLinkSelected(link: string): void {
    // Set the link as user message and trigger analysis
    this.userMessage = link;
    this.sendMessage();
  }

  onPdfRequested(jobId: string): void {
    this.http.post<{pdfUrl: string}>(`${this.apiUrl}/analysis/${jobId}/generate-report`, {}).subscribe({
      next: (response) => {
        if (this.analysisResults) {
          this.analysisResults.pdfReportUrl = response.pdfUrl;
        }
        this.addBotMessageWithTyping('PDF report generated successfully! Click the download button to get it.');
      },
      error: (error) => {
        console.error('Failed to generate PDF:', error);
        this.addBotMessageWithTyping('Failed to generate PDF report. Please try again.');
      }
    });
  }
}
