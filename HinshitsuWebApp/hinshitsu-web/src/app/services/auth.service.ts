import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { BehaviorSubject, Observable, tap } from 'rxjs';
import { Router } from '@angular/router';

export interface LoginRequest {
  email: string;
  password: string;
}

export interface SignUpRequest {
  email: string;
  password: string;
  username: string;
}

export interface VerifyCodeRequest {
  email: string;
  code: string;
}

export interface AuthResponse {
  token?: string;
  message?: string;
  requiresVerification?: boolean;
}

@Injectable({
  providedIn: 'root'
})
export class AuthService {
  private apiUrl = 'https://localhost:7170/api/Auth'; // miro here load from appsettings
  private isAuthenticatedSubject = new BehaviorSubject<boolean>(this.hasToken());
  public isAuthenticated$ = this.isAuthenticatedSubject.asObservable();

  constructor(private http: HttpClient, private router: Router) {}

  private hasToken(): boolean {
    return !!localStorage.getItem('authToken');
  }

  login(credentials: LoginRequest): Observable<AuthResponse> {
    return this.http.post<AuthResponse>(`${this.apiUrl}/login`, credentials).pipe(
      tap(response => {
        if (response.token) {
          localStorage.setItem('authToken', response.token);
          this.isAuthenticatedSubject.next(true);
        }
      })
    );
  }

  signUp(userData: SignUpRequest): Observable<AuthResponse> {
    return this.http.post<AuthResponse>(`${this.apiUrl}/signup`, userData);
  }

  verifyCode(data: VerifyCodeRequest): Observable<AuthResponse> {
    return this.http.post<AuthResponse>(`${this.apiUrl}/verify`, data).pipe(
      tap(response => {
        if (response.token) {
          localStorage.setItem('authToken', response.token);
          this.isAuthenticatedSubject.next(true);
        }
      })
    );
  }

  logout(): void {
    localStorage.removeItem('authToken');
    this.isAuthenticatedSubject.next(false);
    this.router.navigate(['/login']);
  }

  getToken(): string | null {
    return localStorage.getItem('authToken');
  }
}
