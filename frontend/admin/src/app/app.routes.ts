import { Routes } from '@angular/router';
import { authGuard } from './core/guards/auth.guard';
import { roleGuard } from './core/guards/role.guard';

export const routes: Routes = [
  {
    path: '',
    redirectTo: '/dashboard',
    pathMatch: 'full'
  },
  {
    path: 'auth',
    loadChildren: () => import('./features/auth/auth.routes').then(m => m.authRoutes)
  },
  {
    path: 'dashboard',
    loadComponent: () => import('./features/dashboard/dashboard.component').then(m => m.DashboardComponent),
    canActivate: [authGuard]
  },
  {
    path: 'users',
    loadChildren: () => import('./features/users/users.routes').then(m => m.usersRoutes),
    canActivate: [authGuard, roleGuard],
    data: { roles: ['Admin', 'SuperAdmin'] }
  },
  {
    path: 'roles',
    loadChildren: () => import('./features/roles/roles.routes').then(m => m.rolesRoutes),
    canActivate: [authGuard, roleGuard],
    data: { roles: ['SuperAdmin'] }
  },
  {
    path: 'cars',
    loadChildren: () => import('./features/cars/cars.routes').then(m => m.carsRoutes),
    canActivate: [authGuard]
  },
  {
    path: 'chat',
    loadComponent: () => import('./features/chat/chat.component').then(m => m.ChatComponent),
    canActivate: [authGuard]
  },
  {
    path: 'firebase',
    loadComponent: () => import('./features/firebase/firebase.component').then(m => m.FirebaseComponent),
    canActivate: [authGuard, roleGuard],
    data: { roles: ['Admin', 'SuperAdmin'] }
  },
  {
    path: '**',
    loadComponent: () => import('./shared/components/not-found/not-found.component').then(m => m.NotFoundComponent)
  }
];