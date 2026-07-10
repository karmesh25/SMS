import { ErrorHandler, Injectable, inject } from '@angular/core';

import { ToastService } from '../services/toast.service';

@Injectable()
export class GlobalErrorHandlerService implements ErrorHandler {
  private readonly toast = inject(ToastService);

  handleError(error: unknown): void {
    console.error(error);

    if (!this.isHttpError(error)) {
      return;
    }

    switch (error.status) {
      case 401:
        // Handled by the auth interceptor (token refresh / redirect to login).
        return;

      case 403:
        this.toast.error(
          'You do not have permission to add, edit or delete this. Please contact an administrator.'
        );
        return;

      case 0:
        this.toast.error('Unable to reach the server. Check your connection and try again.');
        return;

      default: {
        const message = error.error?.message ?? 'Something went wrong. Please try again.';
        this.toast.error(message);
      }
    }
  }

  private isHttpError(error: unknown): error is { status: number; error?: { message?: string } } {
    return typeof error === 'object' && error !== null && 'status' in error;
  }
}
