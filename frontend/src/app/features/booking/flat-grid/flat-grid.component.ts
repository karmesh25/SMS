import { Component, inject, OnInit } from '@angular/core';
import { Router } from '@angular/router';
import { MatSelectModule } from '@angular/material/select';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatButtonModule } from '@angular/material/button';
import { MatButtonToggleModule } from '@angular/material/button-toggle';
import { MatDialog } from '@angular/material/dialog';
import { PageHeaderComponent } from '../../../shared/components/page-header/page-header.component';
import { MasterDataService } from '../../../core/services/master-data.service';
import { SiteContextService } from '../../../core/services/site-context.service';
import { BookingDetailDialogComponent } from '../booking-detail/booking-detail-dialog.component';

type ViewMode = 'wing' | 'plot';

interface FlatItem {
  id: string;
  flatNo: string;
  status: string;
  sqft: number;
  floor: number;
  flatType?: string;
}

interface FlatGrid {
  wingId: string;
  wingName: string;
  floors: number;
  flatsPerFloor: number;
  isBungalow: boolean;
  isPlot?: boolean;
  bookedCount: number;
  availableCount: number;
  cancelledCount: number;
  flats: FlatItem[];
}

interface AssetOption {
  id: string;
  wingName: string;
  floors: number;
  flatsPerFloor: number;
  isBungalow: boolean;
  isPlot?: boolean;
}

interface FloorGroup {
  key: string;
  title: string;
  flats: FlatItem[];
}

@Component({
  selector: 'app-flat-grid',
  standalone: true,
  imports: [MatSelectModule, MatFormFieldModule, MatButtonModule, MatButtonToggleModule, PageHeaderComponent],
  template: `
    <app-page-header
      title="Wing & Plot Booking Grid"
      [subtitle]="viewMode === 'wing' ? 'Select a wing to view flat status by floor' : 'Select a plot scheme to view plot units'">
    </app-page-header>

    <div class="toolbar">
      <mat-button-toggle-group [value]="viewMode" (change)="onViewModeChange($event.value)">
        <mat-button-toggle value="wing">Wing</mat-button-toggle>
        <mat-button-toggle value="plot">Plot</mat-button-toggle>
      </mat-button-toggle-group>

      <mat-form-field appearance="outline">
        <mat-label>{{ viewMode === 'wing' ? 'Wing' : 'Plot Scheme' }}</mat-label>
        <mat-select [(value)]="selectedAssetId" (selectionChange)="loadGrid()">
          @for (asset of assets; track asset.id) {
            <mat-option [value]="asset.id">{{ asset.wingName }}</mat-option>
          }
        </mat-select>
      </mat-form-field>
    </div>

    @if (grid) {
      <div class="legend">
        <span class="chip available">Available ({{ grid.availableCount }})</span>
        <span class="chip booked">Booked ({{ grid.bookedCount }})</span>
        <span class="chip cancelled">Cancelled ({{ grid.cancelledCount }})</span>
      </div>

      @for (group of floorGroups; track group.key) {
        <section class="floor-section">
          <div class="floor-header">
            <h3>{{ group.title }}</h3>
            <span class="floor-count">{{ group.flats.length }} units</span>
          </div>
          <div class="grid">
            @for (flat of group.flats; track flat.id) {
              <button
                type="button"
                class="flat-card"
                [class]="flat.status"
                [title]="flat.flatNo"
                (click)="onFlatClick(flat)">
                <span class="flat-label">{{ displayFlatLabel(flat) }}</span>
              </button>
            }
          </div>
        </section>
      }
    }
  `,
  styles: [`
    .toolbar {
      display: flex;
      flex-wrap: wrap;
      align-items: center;
      gap: 1rem;
      margin-bottom: 0.5rem;
    }
    .legend { display: flex; gap: 1rem; margin: 1rem 0; flex-wrap: wrap; }
    .chip { padding: 0.25rem 0.75rem; border-radius: 4px; color: #fff; font-size: 0.875rem; }
    .chip.available { background: #16a34a; }
    .chip.booked { background: #dc2626; }
    .chip.cancelled { background: #64748b; }
    .floor-section {
      margin-bottom: 1.75rem;
      padding: 1rem;
      background: var(--abr-surface);
      border-radius: 8px;
      box-shadow: var(--abr-shadow-sm);
    }
    .floor-header {
      display: flex;
      align-items: baseline;
      justify-content: space-between;
      gap: 1rem;
      margin-bottom: 0.75rem;
      padding-bottom: 0.5rem;
      border-bottom: 2px solid var(--abr-primary-strong);
    }
    .floor-header h3 {
      margin: 0;
      color: var(--abr-primary-strong);
      font-size: 1rem;
      font-weight: 600;
    }
    .floor-count {
      color: var(--abr-text-secondary);
      font-size: 0.85rem;
    }
    .grid {
      display: grid;
      grid-template-columns: repeat(auto-fill, minmax(88px, 1fr));
      gap: 0.5rem;
    }
    @media (max-width: 599px) {
      .grid { grid-template-columns: repeat(auto-fill, minmax(68px, 1fr)); gap: 0.35rem; }
      .flat-card { min-height: 46px; font-size: 0.8rem; padding: 0.35rem 0.25rem; }
    }
    .flat-card {
      border: none;
      border-radius: var(--abr-radius-sm);
      min-height: 52px;
      padding: 0.5rem 0.35rem;
      color: #fff;
      cursor: pointer;
      font-weight: 700;
      display: flex;
      align-items: center;
      justify-content: center;
      text-align: center;
      box-shadow: var(--abr-shadow-sm);
      transition: transform var(--abr-dur-fast) var(--abr-ease),
        box-shadow var(--abr-dur) var(--abr-ease), filter var(--abr-dur-fast) var(--abr-ease);
    }
    .flat-label {
      font-size: 0.72rem;
      line-height: 1.2;
      word-break: break-all;
    }
    .flat-card.available { background: #16a34a; }
    .flat-card.booked { background: #dc2626; }
    .flat-card.cancelled { background: #64748b; }
    .flat-card:hover { transform: translateY(-2px); box-shadow: var(--abr-shadow-md); filter: brightness(1.06); }
  `]
})
export class FlatGridComponent implements OnInit {
  private readonly masterData = inject(MasterDataService);
  private readonly siteContext = inject(SiteContextService);
  private readonly router = inject(Router);
  private readonly dialog = inject(MatDialog);

  viewMode: ViewMode = 'wing';
  assets: AssetOption[] = [];
  selectedAssetId: string | null = null;
  grid: FlatGrid | null = null;
  floorGroups: FloorGroup[] = [];

  ngOnInit(): void {
    const siteId = this.siteContext.activeSiteId();
    if (siteId) this.loadAssets(siteId);
    else {
      this.siteContext.loadSites().subscribe({
        next: () => {
          const id = this.siteContext.activeSiteId();
          if (id) this.loadAssets(id);
        }
      });
    }
  }

  onViewModeChange(mode: ViewMode): void {
    this.viewMode = mode;
    this.grid = null;
    this.floorGroups = [];
    const siteId = this.siteContext.activeSiteId();
    if (siteId) this.loadAssets(siteId);
  }

  loadAssets(siteId: string): void {
    const request = this.viewMode === 'wing'
      ? this.masterData.getWings(siteId)
      : this.masterData.getPlots(siteId);

    request.subscribe({
      next: (r) => {
        if (r.success) {
          this.assets = r.data as AssetOption[];
          if (this.assets.length > 0) {
            this.selectedAssetId = this.assets[0].id;
            this.loadGrid();
          } else {
            this.selectedAssetId = null;
            this.grid = null;
            this.floorGroups = [];
          }
        }
      }
    });
  }

  loadGrid(): void {
    if (!this.selectedAssetId) return;
    this.masterData.getFlatGrid(this.selectedAssetId).subscribe({
      next: (r) => {
        if (r.success) {
          this.grid = r.data as FlatGrid;
          this.applyAssetDefaults();
          this.buildFloorGroups();
        }
      }
    });
  }

  private applyAssetDefaults(): void {
    if (!this.grid || !this.selectedAssetId) return;
    const asset = this.assets.find((w) => w.id === this.selectedAssetId);
    if (!asset) return;

    if (!this.grid.floors) this.grid.floors = asset.floors;
    if (!this.grid.flatsPerFloor) this.grid.flatsPerFloor = asset.flatsPerFloor;
    if (this.grid.isBungalow === undefined) this.grid.isBungalow = asset.isBungalow;
    if (this.grid.isPlot === undefined) this.grid.isPlot = asset.isPlot ?? this.viewMode === 'plot';
  }

  buildFloorGroups(): void {
    if (!this.grid) return;

    if (this.grid.isPlot || this.viewMode === 'plot') {
      this.floorGroups = [{
        key: 'plots',
        title: this.grid.wingName,
        flats: this.sortFlats([...this.grid.flats])
      }];
      return;
    }

    const towerMap = new Map<number, FlatItem[]>();
    const shops: FlatItem[] = [];
    const other: FlatItem[] = [];

    for (const flat of this.grid.flats) {
      if (this.isShop(flat)) {
        shops.push(flat);
        continue;
      }

      const floor = this.resolveFloor(flat);
      if (floor > 0) {
        if (!towerMap.has(floor)) towerMap.set(floor, []);
        towerMap.get(floor)!.push(flat);
      } else {
        other.push(flat);
      }
    }

    const groups: FloorGroup[] = [];

    [...towerMap.entries()]
      .sort(([a], [b]) => a - b)
      .forEach(([floor, flats]) => {
        groups.push({
          key: `floor-${floor}`,
          title: `Floor ${floor}`,
          flats: this.sortFlats(flats)
        });
      });

    if (shops.length > 0) {
      groups.push({
        key: 'shops',
        title: 'Shops',
        flats: this.sortFlats(shops)
      });
    }

    if (other.length > 0) {
      groups.push({
        key: 'other',
        title: 'Other',
        flats: this.sortFlats(other)
      });
    }

    this.floorGroups = groups;
  }

  displayFlatLabel(flat: FlatItem): string {
    if (!this.grid) return flat.flatNo;

    const wingPrefix = this.grid.wingName.toUpperCase();
    const upperFlat = flat.flatNo.toUpperCase();
    if (upperFlat.startsWith(wingPrefix)) {
      const suffix = flat.flatNo.slice(wingPrefix.length);
      if (this.isShop(flat)) {
        return suffix.replace(/^S/i, '');
      }
      return suffix;
    }

    const digits = flat.flatNo.match(/\d+/)?.[0];
    return digits ?? flat.flatNo;
  }

  private isShop(flat: FlatItem): boolean {
    return (flat.flatType ?? '').toLowerCase() === 'shop';
  }

  private isPlotUnit(flat: FlatItem): boolean {
    return (flat.flatType ?? '').toLowerCase() === 'plot';
  }

  private resolveFloor(flat: FlatItem): number {
    if (!this.grid || this.grid.isPlot || this.grid.isBungalow || this.isShop(flat) || this.isPlotUnit(flat)) {
      return 0;
    }

    const apiFloor = flat.floor ?? 0;
    if (apiFloor > 0) {
      return apiFloor;
    }

    return this.computeFloorFromFlatNo(flat.flatNo);
  }

  private computeFloorFromFlatNo(flatNo: string): number {
    if (!this.grid) return 0;

    let suffix = flatNo;
    const wingPrefix = this.grid.wingName;
    if (suffix.toUpperCase().startsWith(wingPrefix.toUpperCase())) {
      suffix = suffix.slice(wingPrefix.length);
    } else {
      suffix = suffix.match(/\d+/)?.[0] ?? '';
    }

    if (!suffix || suffix[0].toUpperCase() === 'S') {
      return 0;
    }

    const maxFloorDigits = this.grid.floors.toString().length;
    for (let floorDigits = maxFloorDigits; floorDigits >= 1; floorDigits--) {
      if (suffix.length <= floorDigits) continue;

      const floor = parseInt(suffix.slice(0, floorDigits), 10);
      const positionPart = suffix.slice(floorDigits);
      if (Number.isNaN(floor) || floor < 1 || floor > this.grid.floors) continue;
      if (!/^\d+$/.test(positionPart)) continue;

      const position = parseInt(positionPart, 10);
      if (position >= 1 && position <= this.grid.flatsPerFloor) {
        return floor;
      }
    }

    return 0;
  }

  private sortFlats(flats: FlatItem[]): FlatItem[] {
    return [...flats].sort((a, b) => a.flatNo.localeCompare(b.flatNo, undefined, { numeric: true }));
  }

  onFlatClick(flat: FlatItem): void {
    if (flat.status === 'available') {
      void this.router.navigate(['/booking/new'], { queryParams: { flatId: flat.id } });
      return;
    }

    this.dialog.open(BookingDetailDialogComponent, {
      data: { flatId: flat.id, flatStatus: flat.status },
      width: 'min(720px, calc(100vw - 2rem))',
      maxWidth: '100vw',
      maxHeight: '90vh'
    }).afterClosed().subscribe(() => this.loadGrid());
  }
}
