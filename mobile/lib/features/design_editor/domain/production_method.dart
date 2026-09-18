enum ProductionMethod {
  printing('Printing'),
  embroidery('Embroidery');

  const ProductionMethod(this.apiValue);
  final String apiValue;

  static ProductionMethod fromApi(String value) {
    return ProductionMethod.values.firstWhere(
      (m) => m.apiValue.toLowerCase() == value.toLowerCase(),
      orElse: () => ProductionMethod.printing,
    );
  }
}
