import { Component } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { SidebarComponent } from '../../shared/components/sidebar-component/sidebar-component';
import { NavbarComponent } from '../../shared/components/navbar-component/navbar-component';
import { ToastComponent } from '../../shared/components/toast-component/toast-component';

@Component({
  selector: 'app-admin-layout-component',
  standalone: true,
  imports: [RouterOutlet, SidebarComponent, NavbarComponent, ToastComponent],
  templateUrl: './admin-layout-component.html',
  styleUrl: './admin-layout-component.scss'
})
export class AdminLayoutComponent {
  mobileMenuOpen = false;

  onMobileMenuToggle(isOpen: boolean): void {
    this.mobileMenuOpen = isOpen;
  }
}
