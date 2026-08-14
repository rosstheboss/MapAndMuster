import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { provideZonelessChangeDetection } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';

import { CampaignsPage } from './campaigns.page';

describe('CampaignsPage', () => {
  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [CampaignsPage],
      providers: [provideZonelessChangeDetection(), provideRouter([]), provideHttpClient(), provideHttpClientTesting()],
    }).compileComponents();
  });

  it('lists campaigns and shows a create action', async () => {
    const fixture = TestBed.createComponent(CampaignsPage);
    const http = TestBed.inject(HttpTestingController);
    http.expectOne('/api/campaigns').flush([
      {
        id: 'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa',
        name: 'Border War',
        playerSlotCount: 8,
        occupiedPlayerSlots: 1,
        isPrivate: false,
        canManage: true,
        isParticipant: true,
      },
    ]);
    await fixture.whenStable();
    fixture.detectChanges();

    const compiled = fixture.nativeElement as HTMLElement;
    expect(compiled.querySelector('h1')?.textContent).toContain('Your campaigns');
    expect(compiled.querySelector('a.button')?.textContent).toContain('Create campaign');
    expect(compiled.textContent).toContain('Border War');
    expect(compiled.textContent).toContain('1 of 8 players');
    http.verify();
  });
});
