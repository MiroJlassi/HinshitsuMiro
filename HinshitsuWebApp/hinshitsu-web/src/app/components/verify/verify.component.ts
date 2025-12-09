import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { AuthService } from '../../services/auth.service';

@Component({
  selector: 'app-verify',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink],
  templateUrl: './verify.component.html',
  styleUrl: './verify.component.css'
})
export class VerifyComponent implements OnInit {
  email = '';
  code = '';
  errorMessage = '';
  loading = false;

  constructor(
    private readonly authService: AuthService,
    private readonly router: Router,
    private readonly route: ActivatedRoute
  ) {}

  ngOnInit(): void {
    // Get email from query params
    this.route.queryParams.subscribe(params => {
      this.email = params['email'] || '';
    });
  }

  onSubmit(): void {
    if (!this.code) {
      this.errorMessage = 'Please enter the verification code';
      return;
    }

    if (!this.email) {
      this.errorMessage = 'Email is missing. Please try signing up again.';
      return;
    }

    this.loading = true;
    this.errorMessage = '';

    this.authService.verifyCode({ email: this.email, code: this.code }).subscribe({
      next: () => {
        this.loading = false;
        this.router.navigate(['/home']);
      },
      error: (error) => {
        this.loading = false;
        this.errorMessage = error.error?.message || 'Verification failed. Please check your code.';
      }
    });
  }

  // todo
  resendCode(): void {
    console.log('Resend code for:', this.email);
  }
}
