import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterOutlet } from '@angular/router';
import { MatToolbarModule } from '@angular/material/toolbar';
import { MatSidenavModule } from '@angular/material/sidenav';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';
import { MatListModule } from '@angular/material/list';

import { AuthService } from './core/services/auth.service';
import { SignalRService } from './core/services/signalr.service';
import { NavigationComponent } from './shared/components/navigation/navigation.component';

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [
    CommonModule,
    RouterOutlet,
    MatToolbarModule,
    MatSidenavModule,
    MatIconModule,
    MatButtonModule,
    MatListModule,
    NavigationComponent
  ],
  template: `
    <div class="app-container">
      <app-navigation *ngIf="authService.isAuthenticated()"></app-navigation>
      <main class="main-content" [class.authenticated]="authService.isAuthenticated()">
        <router-outlet></router-outlet>
      </main>
    </div>
  `,
  styles: [`
    .app-container {
      height: 100vh;
      display: flex;
      flex-direction: column;
    }

    .main-content {
      flex: 1;
      overflow: auto;
      padding: 20px;
    }

    .main-content.authenticated {
      margin-left: 250px;
    }

    @media (max-width: 768px) {
      .main-content.authenticated {
        margin-left: 0;
      }
    }
  `]
})
export class AppComponent implements OnInit {
  constructor(
    public authService: AuthService,
    private signalRService: SignalRService
  ) {}

  ngOnInit() {
    // Initialize SignalR connection when user is authenticated
    if (this.authService.isAuthenticated()) {
      this.signalRService.startConnection();
    }

    // Listen for authentication changes
    this.authService.authStatus$.subscribe(isAuthenticated => {
      if (isAuthenticated) {
        this.signalRService.startConnection();
      } else {
        this.signalRService.stopConnection();
      }
    });
  }
}