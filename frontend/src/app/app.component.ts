import { Component } from '@angular/core';

@Component({
  selector: 'app-root',
  template: `
    <div class="container">
      <h1>RAG Application</h1>
      <p>Upload PDF documents and ask questions powered by Claude AI</p>
      <div class="placeholder">
        <p>Frontend is ready for development!</p>
        <p>Run: cd frontend && npm install && npm start</p>
      </div>
    </div>
  `,
  styles: [`
    .container { max-width: 800px; margin: 50px auto; text-align: center; }
    .placeholder { margin-top: 50px; padding: 30px; background: #f0f0f0; border-radius: 8px; }
  `]
})
export class AppComponent {
  title = 'RAG Application';
}
