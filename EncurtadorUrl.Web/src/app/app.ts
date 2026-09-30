import { Component, viewChild } from '@angular/core';
import { ShortenForm } from './components/shorten-form/shorten-form';
import { UrlList } from './components/url-list/url-list';

@Component({
  selector: 'app-root',
  imports: [ShortenForm, UrlList],
  templateUrl: './app.html',
  styleUrl: './app.css',
})
export class App {
  private readonly list = viewChild.required(UrlList);

  onChanged(): void {
    this.list().reload();
  }
}
