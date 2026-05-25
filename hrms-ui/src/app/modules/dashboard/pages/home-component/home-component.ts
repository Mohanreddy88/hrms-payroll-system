import { Component, OnInit } from '@angular/core';
import { Router, RouterModule } from '@angular/router';
import { CommonModule } from '@angular/common';
import { AuthService } from '../../../../core/services/auth.service';

@Component({
  selector: 'app-home-component',
  standalone: true,
  imports: [RouterModule, CommonModule],
  templateUrl: './home-component.html',
  styleUrl: './home-component.scss',
})
export class HomeComponent implements OnInit {
  currentYear = new Date().getFullYear();

  constructor(
    private authService: AuthService,
    private router: Router
  ) {}

  ngOnInit(): void {
    // Redirect employees to their dashboard, only admins see home
    const user = this.authService.getUser();
    if (user?.role !== 'Admin') {
      this.router.navigate(['/self-service/dashboard']);
    }
  }
}
