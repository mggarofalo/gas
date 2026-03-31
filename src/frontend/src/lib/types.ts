export interface Vehicle {
  id: string;
  year: number;
  make: string;
  model: string;
  notes: string | null;
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
  stationName: string;
  stationAddress: string | null;
  latitude: number | null;
  longitude: number | null;
  receiptUrl: string | null;
  tripMiles: number | null;
  mpg: number | null;
  costPerMile: number | null;
  paperlessSyncStatus: string;
  notes: string | null;
  createdAt: string;
}

export interface FillUpPage {
  items: FillUp[];
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
