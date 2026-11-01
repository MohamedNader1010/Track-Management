import { Component } from '@angular/core';
import { RouterLink } from '@angular/router';

@Component({
  selector: 'app-not-found',
  imports: [RouterLink],
  template: `
    <div class="card empty">
      <h1>Page not found</h1>
      <p class="muted">The page you're looking for doesn't exist.</p>
      <a routerLink="/tracks" class="btn">Go to tracks</a>
    </div>
  `,
})
export class NotFound {}
