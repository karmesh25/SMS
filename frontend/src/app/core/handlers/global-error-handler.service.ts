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

      case 403: {
        const apiMessage = this.readApiMessage(error);
        this.toast.error(
          apiMessage ||
            'You do not have permission to add, edit or delete this. Please contact an administrator.'
        );
        return;
      }

      case 0:
        this.toast.error('Unable to reach the server. Check your connection and try again.');
        return;

      default: {
        const message = this.readApiMessage(error) ?? 'Something went wrong. Please try again.';
        this.toast.error(message);
      }
    }
  }

  private readApiMessage(error: { error?: { message?: string } | string }): string | null {
    const body = error.error;
    if (typeof body === 'string' && body.trim()) {
      return body.trim();
    }
    if (body && typeof body === 'object' && typeof body.message === 'string' && body.message.trim()) {
      return body.message.trim();
    }
    return null;
  }

  private isHttpError(error: unknown): error is { status: number; error?: { message?: string } | string } {
    return typeof error === 'object' && error !== null && 'status' in error;
  }
}
