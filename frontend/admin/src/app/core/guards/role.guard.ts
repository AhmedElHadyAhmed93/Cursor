import { inject } from '@angular/core';
import { Router, type CanActivateFn } from '@angular/router';
import { AuthService } from '../services/auth.service';

export const roleGuard: CanActivateFn = (route, state) => {
  const authService = inject(AuthService);
  const router = inject(Router);

  const requiredRoles = route.data?.['roles'] as string[];
  const requiredClaims = route.data?.['claims'] as string[];

  if (!authService.isAuthenticated()) {
    router.navigate(['/auth/login']);
    return false;
  }

  // Check roles
  if (requiredRoles && requiredRoles.length > 0) {
    if (!authService.hasRoles(requiredRoles)) {
      router.navigate(['/dashboard']);
      return false;
    }
  }

  // Check claims
  if (requiredClaims && requiredClaims.length > 0) {
    const hasClaim = requiredClaims.some(claim => authService.hasClaim(claim));
    if (!hasClaim) {
      router.navigate(['/dashboard']);
      return false;
    }
  }

  return true;
};