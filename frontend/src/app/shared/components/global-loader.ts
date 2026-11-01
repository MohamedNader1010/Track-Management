import { Component, inject } from '@angular/core';

import { LoadingService } from '../../core/loading/loading.service';

@Component({
  selector: 'app-global-loader',
  template: `
    @if (loading.isLoading()) {
      <div class="bar" role="progressbar" aria-label="Loading"></div>
    }
  `,
  styles: `
    :host { position: fixed; inset: 0 0 auto 0; height: 3px; z-index: 100; overflow: hidden; pointer-events: none; }
    .bar {
      height: 100%;
      width: 40%;
      background: var(--accent);
      /* Delay so very fast requests don't flash the bar. */
      animation: slide 1s ease-in-out 150ms infinite both;
    }
    @keyframes slide {
      from { transform: translateX(-100%); }
      to { transform: translateX(260%); }
    }
  `,
})
export class GlobalLoader {
  protected readonly loading = inject(LoadingService);
}
