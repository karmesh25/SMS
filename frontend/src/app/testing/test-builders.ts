export function buildSite(overrides: Partial<{ id: string; siteName: string }> = {}) {
  return { id: 'site-1', siteName: 'Tapi', isActive: true, ...overrides };
}

export function buildBooking(overrides: Partial<{ totalPrice: number; sqft: number; rate: number }> = {}) {
  return { sqft: 1000, rate: 5000, totalPrice: 5000000, ...overrides };
}
