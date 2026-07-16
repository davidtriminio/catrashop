import { Component } from '@angular/core';
import { RouterLink } from '@angular/router';

@Component({
  selector: 'app-forbidden',
  imports: [RouterLink],
  template: `
    <div class="card" style="max-width:520px">
      <h1>Access denied</h1>
      <p class="muted">Your account doesn't have permission to view that section.</p>
      <a routerLink="/dashboard" class="btn">Back to dashboard</a>
    </div>
  `,
})
export class Forbidden {}
