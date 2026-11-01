import { Directive, ElementRef, afterNextRender, inject } from '@angular/core';

/** Focuses the host element once it is rendered (the native `autofocus` attribute is ignored on route changes). */
@Directive({ selector: '[appAutofocus]' })
export class AutofocusDirective {
  constructor() {
    const element = inject<ElementRef<HTMLElement>>(ElementRef);
    afterNextRender(() => element.nativeElement.focus());
  }
}
