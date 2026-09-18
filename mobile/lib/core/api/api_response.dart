class ApiResponse<T> {
  ApiResponse({required this.status, this.message, this.data});

  final String status;
  final String? message;
  final T? data;

  bool get isSuccess => status == 'success';

  factory ApiResponse.fromJson(
    Map<String, dynamic> json,
    T Function(Object? json) fromJsonT,
  ) {
    return ApiResponse<T>(
      status: json['status'] as String? ?? 'error',
      message: json['message'] as String?,
      data: json['data'] == null ? null : fromJsonT(json['data']),
    );
  }
}
