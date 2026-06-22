describe('FlatGrid floor grouping', () => {
  function computeFloorFromFlatNo(flatNo: string, wingName: string, floors: number, flatsPerFloor: number): number {
    const prefix = wingName.toUpperCase();
    if (!flatNo.toUpperCase().startsWith(prefix)) return 0;
    const suffix = flatNo.slice(prefix.length);
    const position = parseInt(suffix, 10);
    if (!position || position <= 0) return 0;
    return Math.ceil(position / flatsPerFloor);
  }

  it('groups tower flats by floor', () => {
    expect(computeFloorFromFlatNo('BHAGVATI101', 'BHAGVATI', 4, 100)).toBe(2);
    expect(computeFloorFromFlatNo('BHAGVATI250', 'BHAGVATI', 4, 100)).toBe(3);
  });

  it('returns 0 for non-matching prefix', () => {
    expect(computeFloorFromFlatNo('SHOP01', 'BHAGVATI', 4, 100)).toBe(0);
  });
});
