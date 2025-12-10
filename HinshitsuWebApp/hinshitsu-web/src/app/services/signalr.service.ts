import { Injectable } from '@angular/core';
import * as signalR from '@microsoft/signalr';
import { BehaviorSubject, Observable } from 'rxjs';
import { environment } from '../../environments/environment';

export interface TaskProgressNotification {
  jobId: string;
  userId: string;
  taskType: 'Security' | 'Performance' | 'UiUx';
  status: 'Started' | 'Completed' | 'Failed';
  data?: any;
  errorMessage?: string;
  timestamp: string;
}

export interface JobCompletionNotification {
  jobId: string;
  userId: string;
  status: 'Completed' | 'Failed';
  tasks: {
    securityStatus: string;
    performanceStatus: string;
    uiUxStatus: string;
  };
  completedAt: string;
}

@Injectable({
  providedIn: 'root'
})
export class SignalRService {
  private connection: signalR.HubConnection | null = null;
  private isConnectedSubject = new BehaviorSubject<boolean>(false);
  public isConnected$ = this.isConnectedSubject.asObservable();

  private taskProgressSubject = new BehaviorSubject<TaskProgressNotification | null>(null);
  public taskProgress$ = this.taskProgressSubject.asObservable();

  private jobCompletedSubject = new BehaviorSubject<JobCompletionNotification | null>(null);
  public jobCompleted$ = this.jobCompletedSubject.asObservable();

  private serviceUrl = environment.apiUrl;

  constructor() {}

  async connect(): Promise<void> {
    if (this.isConnectedSubject.value) {
      console.log('Already connected to SignalR');
      return;
    }

    try {
      this.connection = new signalR.HubConnectionBuilder()
        .withUrl(`${this.serviceUrl}/hubs/notifications`, {
          skipNegotiation: false,
          transport: signalR.HttpTransportType.WebSockets | signalR.HttpTransportType.ServerSentEvents,
        })
        .withAutomaticReconnect({
          nextRetryDelayInMilliseconds: () => {
            return Math.random() * 10000; // Retry with jitter
          }
        })
        .configureLogging(signalR.LogLevel.Information)
        .build();

      // Handle reconnection
      this.connection.onreconnecting((error) => {
        console.warn('SignalR reconnecting...', error);
        this.isConnectedSubject.next(false);
      });

      this.connection.onreconnected((connectionId) => {
        console.log('SignalR reconnected:', connectionId);
        this.isConnectedSubject.next(true);
      });

      this.connection.onclose((error) => {
        console.error('SignalR connection closed:', error);
        this.isConnectedSubject.next(false);
      });

      // Register event handlers
      this.connection.on('TaskProgress', (notification: TaskProgressNotification) => {
        console.log('TaskProgress received:', notification);
        this.taskProgressSubject.next(notification);
      });

      this.connection.on('JobCompleted', (notification: JobCompletionNotification) => {
        console.log('JobCompleted received:', notification);
        this.jobCompletedSubject.next(notification);
      });

      await this.connection.start();
      this.isConnectedSubject.next(true);
      console.log('SignalR connected successfully');
    } catch (error) {
      console.error('Error connecting to SignalR:', error);
      this.isConnectedSubject.next(false);
      throw error;
    }
  }

  async joinJobGroup(jobId: string): Promise<void> {
    if (this.connection && this.isConnectedSubject.value) {
      try {
        await this.connection.invoke('JoinJobGroup', jobId);
        console.log(`Joined job group: ${jobId}`);
      } catch (error) {
        console.error('Error joining job group:', error);
        throw error;
      }
    }
  }

  async leaveJobGroup(jobId: string): Promise<void> {
    if (this.connection && this.isConnectedSubject.value) {
      try {
        await this.connection.invoke('LeaveJobGroup', jobId);
        console.log(`Left job group: ${jobId}`);
      } catch (error) {
        console.error('Error leaving job group:', error);
      }
    }
  }

  async joinUserGroup(userId: string): Promise<void> {
    if (this.connection && this.isConnectedSubject.value) {
      try {
        await this.connection.invoke('JoinUserGroup', userId);
        console.log(`Joined user group: ${userId}`);
      } catch (error) {
        console.error('Error joining user group:', error);
        throw error;
      }
    }
  }

  async disconnect(): Promise<void> {
    if (this.connection) {
      try {
        await this.connection.stop();
        this.isConnectedSubject.next(false);
        console.log('SignalR disconnected');
      } catch (error) {
        console.error('Error disconnecting from SignalR:', error);
      }
    }
  }

  getConnectionState(): signalR.HubConnectionState | null {
    return this.connection?.state || null;
  }
}
