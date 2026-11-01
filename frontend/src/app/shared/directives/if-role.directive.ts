import { Directive, TemplateRef, ViewContainerRef, effect, inject, input } from '@angular/core';

import { AuthService } from '../../core/auth/auth.service';

@Directive({ selector: '[appIfRole]' })
export class IfRoleDirective {
  private readonly auth = inject(AuthService);
  private readonly template = inject(TemplateRef<unknown>);
  private readonly viewContainer = inject(ViewContainerRef);

  readonly appIfRole = input.required<string>();
  readonly appIfRoleElse = input<TemplateRef<unknown> | null>(null);

  constructor() {
    effect(() => {
      this.auth.currentUser();
      const allowed = this.auth.hasRole(this.appIfRole());
      const fallback = this.appIfRoleElse();

      this.viewContainer.clear();
      if (allowed) {
        this.viewContainer.createEmbeddedView(this.template);
      } else if (fallback) {
        this.viewContainer.createEmbeddedView(fallback);
      }
    });
  }
}
