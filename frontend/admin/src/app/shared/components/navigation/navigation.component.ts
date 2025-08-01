import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { MatSidenavModule } from '@angular/material/sidenav';
import { MatListModule } from '@angular/material/list';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';
import { MatToolbarModule } from '@angular/material/toolbar';
import { MatMenuModule } from '@angular/material/menu';

import { AuthService, UserProfile } from '../../../core/services/auth.service';

export interface MenuItem {
  label: string;
  icon: string;
  path: string;
  roles?: string[];
  claims?: string[];
}

@Component({
  selector: 'app-navigation',
  standalone: true,
  imports: [
    CommonModule,
    RouterModule,
    MatSidenavModule,
    MatListModule,
    MatIconModule,
    MatButtonModule,
    MatToolbarModule,
    MatMenuModule
  ],
  template: `
    <mat-toolbar color="primary" class="toolbar">
      <span>Clean Architecture Admin</span>
      <span class="spacer"></span>
      
      <button mat-button [matMenuTriggerFor]="userMenu" *ngIf="currentUser">
        <mat-icon>account_circle</mat-icon>
        {{ currentUser.fullName }}
      </button>
      
      <mat-menu #userMenu="matMenu">
        <button mat-menu-item routerLink="/profile">
          <mat-icon>person</mat-icon>
          Profile
        </button>
        <button mat-menu-item (click)="logout()">
          <mat-icon>logout</mat-icon>
          Logout
        </button>
      </mat-menu>
    </mat-toolbar>

    <mat-sidenav-container class="sidenav-container">
      <mat-sidenav mode="side" opened class="sidenav">
        <mat-nav-list>
          <ng-container *ngFor="let item of visibleMenuItems">
            <a mat-list-item [routerLink]="item.path" routerLinkActive="active">
              <mat-icon matListItemIcon>{{ item.icon }}</mat-icon>
              <span matListItemTitle>{{ item.label }}</span>
            </a>
          </ng-container>
        </mat-nav-list>
      </mat-sidenav>
    </mat-sidenav-container>
  `,
  styles: [`
    .toolbar {
      position: fixed;
      top: 0;
      left: 0;
      right: 0;
      z-index: 1000;
    }

    .spacer {
      flex: 1 1 auto;
    }

    .sidenav-container {
      position: fixed;
      top: 64px;
      left: 0;
      bottom: 0;
      width: 250px;
      z-index: 999;
    }

    .sidenav {
      width: 250px;
      border-right: 1px solid #e0e0e0;
    }

    .active {
      background-color: rgba(0, 0, 0, 0.04);
    }

    @media (max-width: 768px) {
      .sidenav-container {
        display: none;
      }
    }
  `]
})
export class NavigationComponent implements OnInit {
  currentUser: UserProfile | null = null;
  visibleMenuItems: MenuItem[] = [];

  private readonly menuItems: MenuItem[] = [
    { label: 'Dashboard', icon: 'dashboard', path: '/dashboard' },
    { label: 'Users', icon: 'group', path: '/users', roles: ['Admin', 'SuperAdmin'] },
    { label: 'Roles & Claims', icon: 'security', path: '/roles', roles: ['SuperAdmin'] },
    { label: 'Cars', icon: 'directions_car', path: '/cars' },
    { label: 'Chat', icon: 'chat', path: '/chat' },
    { label: 'Firebase Demo', icon: 'notifications', path: '/firebase', roles: ['Admin', 'SuperAdmin'] }
  ];

  constructor(public authService: AuthService) {}

  ngOnInit() {
    this.authService.currentUser$.subscribe(user => {
      this.currentUser = user;
      this.updateVisibleMenuItems();
    });
  }

  logout() {
    this.authService.logout();
  }

  private updateVisibleMenuItems() {
    this.visibleMenuItems = this.menuItems.filter(item => {
      // Show item if no roles required
      if (!item.roles && !item.claims) {
        return true;
      }

      // Check roles
      if (item.roles && !this.authService.hasRoles(item.roles)) {
        return false;
      }

      // Check claims
      if (item.claims && !item.claims.some(claim => this.authService.hasClaim(claim))) {
        return false;
      }

      return true;
    });
  }
}