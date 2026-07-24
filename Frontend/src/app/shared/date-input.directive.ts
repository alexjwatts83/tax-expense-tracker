import { Directive, ElementRef } from '@angular/core';

@Directive({
  selector: 'input[type="date"][matInput][appDateInput]',
  standalone: true,
  exportAs: 'appDateInput',
})
export class DateInputDirective {
  constructor(private readonly elementRef: ElementRef<HTMLInputElement>) {}

  get inputElement(): HTMLInputElement {
    return this.elementRef.nativeElement;
  }
}
