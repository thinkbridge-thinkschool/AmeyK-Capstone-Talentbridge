import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { TokenService } from '../auth/token.service';

export const roleGuard: CanActivateFn = (route, state) => {
  const tokenService = inject(TokenService);
  const router = inject(Router);

  if (!tokenService.isLoggedIn()) {
    return router.createUrlTree(['/auth/login'], {
      queryParams: { returnUrl: state.url }
    });
  }

  const allowedRoles: string[] = route.data?.['roles'] ?? [];
  const userRole = tokenService.getRole();

  if (allowedRoles.length === 0 || (userRole !== null && allowedRoles.includes(userRole))) {
    return true;
  }

  return router.createUrlTree(['/']);
};
