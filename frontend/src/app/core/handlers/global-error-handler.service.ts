import { ErrorHandler, Injectable, inject } from '@angular/core';

import { ToastService } from '../services/toast.service';



@Injectable()

export class GlobalErrorHandlerService implements ErrorHandler {

  private readonly toast = inject(ToastService);



  handleError(error: unknown): void {

    console.error(error);



    if (this.isHttpError(error) && error.status === 401) {

      return;

    }



    if (this.isHttpError(error) && error.status === 0) {

      this.toast.error('Unable to reach the server.');

      return;

    }



    if (this.isHttpError(error)) {

      const message = error.error?.message ?? 'An unexpected error occurred.';

      this.toast.error(message);

    }

  }



  private isHttpError(error: unknown): error is { status: number; error?: { message?: string } } {

    return typeof error === 'object' && error !== null && 'status' in error;

  }

}


