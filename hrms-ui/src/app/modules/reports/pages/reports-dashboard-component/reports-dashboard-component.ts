import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { FormsModule } from '@angular/forms';

@Component({
  selector: 'app-reports-dashboard',
  standalone: true,
  imports: [CommonModule, RouterModule, FormsModule],
  templateUrl: './reports-dashboard-component.html',
  styleUrl: './reports-dashboard-component.scss'
})
export class ReportsDashboardComponent implements OnInit {
  currentYear = new Date().getFullYear();
  currentMonth = new Date().getMonth() + 1;

  ngOnInit(): void {}
}
