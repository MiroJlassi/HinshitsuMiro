import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { AuthService } from '../../services/auth.service';
import { HttpClient } from '@angular/common/http';

interface Message {
  content: string;
  isUser: boolean;
  timestamp: Date;
}

@Component({
  selector: 'app-home',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './home.component.html',
  styleUrl: './home.component.css'
})
export class HomeComponent {
  sidebarCollapsed = false;
  userMessage = '';
  userName = 'John Doe';
  userInitials = 'JD';
  messages: Message[] = [];
  isLoading = false;
  private readonly apiUrl = 'https://localhost:7170/api';

  constructor(
    private readonly authService: AuthService,
    private readonly router: Router,
    private readonly http: HttpClient
  ) {
    this.userInitials = this.getInitials(this.userName);
  }

  toggleSidebar(): void {
    this.sidebarCollapsed = !this.sidebarCollapsed;
  }

  getInitials(name: string): string {
    return name
      .split(' ')
      .map(n => n[0])
      .join('')
      .toUpperCase()
      .substring(0, 2);
  }

  extractUrls(text: string): string[] {
    const urlRegex = /(https?:\/\/[^\s]+)|(www\.[^\s]+)/gi;
    const matches = text.match(urlRegex);
    return matches || [];
  }
  // Miro Here: we should move messaging system in the server side later (for chat persistance)
  sendMessage(): void {
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
      this.messages.push({
        content: 'Oh sorry, no available URL in your message. Please provide a URL to analyze.',
        isUser: false,
        timestamp: new Date()
      });
      this.userMessage = '';
      return;
    }

    if (urls.length > 1) {
      this.messages.push({
        content: 'Please provide only one URL at a time.',
        isUser: false,
        timestamp: new Date()
      });
      this.userMessage = '';
      return;
    }

    // Call API with single URL
    this.isLoading = true;
    this.userMessage = '';

    this.http.post<any>(`${this.apiUrl}/Analysis/submit`, { url: urls[0] }).subscribe({
      next: (response) => {
        this.messages.push({
          content: response.message || 'URL submitted successfully!',
          isUser: false,
          timestamp: new Date()
        });
        this.isLoading = false;
      },
      error: (error) => {
        this.messages.push({
          content: error.error?.message || 'Failed to process the URL. Please try again.',
          isUser: false,
          timestamp: new Date()
        });
        this.isLoading = false;
      }
    });
  }

  logout(): void {
    this.authService.logout();
  }
}
