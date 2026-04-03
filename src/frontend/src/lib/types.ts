export interface Vehicle {
  id: string;
  year: number;
  make: string;
  model: string;
  notes: string | null;
  octaneRating: number | null;
  isActive: boolean;
  label: string;
  createdAt: string;
  updatedAt: string;
}

export interface FillUp {
  id: string;
  vehicleId: string;
  vehicleLabel: string;
  date: string;
  odometerMiles: number;
  gallons: number;
  pricePerGallon: number;
  totalCost: number;
  octaneRating: number | null;
  stationName: string;
  stationAddress: string | null;
  latitude: number | null;
  longitude: number | null;
  receiptUrl: string | null;
  tripMiles: number | null;
  mpg: number | null;
  costPerMile: number | null;
  paperlessSyncStatus: string;
  paperlessSyncedAt: string | null;
  paperlessSyncError: string | null;
  ynabSyncStatus: string;
  ynabAccountId: string | null;
  ynabAccountName: string | null;
  ynabCategoryId: string | null;
  ynabCategoryName: string | null;
  notes: string | null;
  createdAt: string;
}

export interface FillUpPage {
  items: FillUp[];
  page: number;
  pageSize: number;
  totalCount: number;
}

export interface StationSuggestion {
  stationName: string;
  visitCount: number;
  lastVisit: string;
}

export interface NearbyStation {
  stationName: string;
  stationAddress: string | null;
  distanceMiles: number;
  visitCount: number;
}

export interface YnabImport {
  id: string;
  ynabTransactionId: string;
  date: string;
  payeeName: string;
  amountMilliunits: number;
  memo: string | null;
  gallons: number | null;
  pricePerGallon: number | null;
  octaneRating: number | null;
  odometerMiles: number | null;
  vehicleName: string | null;
  vehicleId: string | null;
  status: string;
}

export interface YnabImportPage {
  items: YnabImport[];
  page: number;
  pageSize: number;
  totalCount: number;
}

export interface Stats {
  totalFillUps: number;
  totalGallons: number;
  totalCost: number;
  totalMiles: number;
  avgMpg: number | null;
  avgPricePerGallon: number | null;
  avgCostPerFillUp: number | null;
  costPerMile: number | null;
}
