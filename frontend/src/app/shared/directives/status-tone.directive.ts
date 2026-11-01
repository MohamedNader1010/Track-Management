import { Directive, computed, input } from '@angular/core';

type Tone = 'neutral' | 'info' | 'success' | 'danger';

const TONES: Record<string, Tone> = {
  draft: 'neutral',
  submitted: 'info',
  pending: 'info',
  distributed: 'success',
  live: 'success',
  rejected: 'danger',
};

/**
 * Attribute directive that turns any element into a status pill coloured by the status value.
 *   <span [appStatusTone]="track.status">{{ track.status }}</span>
 */
@Directive({
  selector: '[appStatusTone]',
  host: {
    class: 'status-pill',
    '[attr.data-tone]': 'tone()',
    '[attr.title]': '"Status: " + appStatusTone()',
  },
})
export class StatusToneDirective {
  readonly appStatusTone = input.required<string>();

  protected readonly tone = computed<Tone>(() => TONES[this.appStatusTone().toLowerCase()] ?? 'neutral');
}
