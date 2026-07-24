# Frontend

This project was generated using [Angular CLI](https://github.com/angular/angular-cli) version 22.0.7.

## Development server

To start a local development server, run:

```bash
npm start
```

The development server uses `proxy.conf.json` so requests to `/api` are forwarded to `https://localhost:5001`.

Once the server is running, open your browser and navigate to `http://localhost:4200/`. The application will automatically reload whenever you modify any of the source files.

## Code scaffolding

Angular CLI includes powerful code scaffolding tools. To generate a new component, run:

```bash
ng generate component component-name
```

For a complete list of available schematics (such as `components`, `directives`, or `pipes`), run:

```bash
ng generate --help
```

## Building

To build the project run:

```bash
ng build
```

This will compile your project and store the build artifacts in the `dist/` directory. By default, the production build optimizes your application for performance and speed.

## Running unit tests

To execute unit tests with the [Vitest](https://vitest.dev/) test runner, use the following command:

```bash
ng test
```

## UI validation strategy

This project does not use automated UI/e2e tests right now.

Validation approach:

- Run API and unit/integration automated checks.
- Perform manual UI smoke and end-to-end flow checks in the app.

## Reusable Date Input Pattern

Use the shared date-input helpers so every `matInput` date field has a consistent calendar popup button:

- Directive: `src/app/shared/date-input.directive.ts`
- Toggle component: `src/app/shared/date-picker-toggle.component.ts`

When adding a new date input in a standalone component:

1. Add `DateInputDirective` and `DatePickerToggleComponent` to the component `imports` array.
2. Decorate the input with `appDateInput` and export it in the template.
3. Place `<app-date-picker-toggle>` in the same `mat-form-field` and bind `[for]` to that exported input.

Example:

```html
<mat-form-field appearance="outline" subscriptSizing="dynamic">
	<mat-label>Date</mat-label>
	<input #entryDateInput="appDateInput" appDateInput matInput type="date" formControlName="date" />
	<app-date-picker-toggle [for]="entryDateInput" ariaLabel="Open date calendar"></app-date-picker-toggle>
</mat-form-field>
```

## Additional Resources

For more information on using the Angular CLI, including detailed command references, visit the [Angular CLI Overview and Command Reference](https://angular.dev/tools/cli) page.
