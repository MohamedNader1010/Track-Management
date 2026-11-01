import { Directive, TemplateRef, ViewContainerRef, effect, inject, input } from '@angular/core';

import { AuthService } from '../../core/auth/auth.service';

/**
 * Structural directive: renders its content only for signed-in users that have the given role.
 * Reacts to sign-in / sign-out automatically because it reads the auth signals inside an effect.
 *
 *   <section *appIfRole="'catalog-manager'">…</section>
 *   <span *appIfRole="'catalog-manager'; else readOnly">…</span>
 */
@Directive({ selector: '[appIfRole]' })
export class IfRoleDirective {
  private readonly auth = inject(AuthService);
  private readonly template = inject(TemplateRef<unknown>);
  private readonly viewContainer = inject(ViewContainerRef);

  readonly appIfRole = input.required<string>();
  readonly appIfRoleElse = input<TemplateRef<unknown> | null>(null);

  constructor() {
    effect(() => {
      this.auth.currentUser(); // track session changes
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
