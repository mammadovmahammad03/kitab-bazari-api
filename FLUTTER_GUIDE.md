# 📱 Kitab Bazari API — Flutter İnteqrasiya Bələdçisi

Bu sənəd Flutter developer-lər üçündür. **Sıfırdan** Kitab Bazari API-ni Flutter tətbiqinə qoşmağı, JWT auth-u idarə etməyi və hər ekran üçün hansı endpoint-i çağırmağı izah edir.

---

## 📋 Mündəricat

1. [Əsas məlumat](#əsas-məlumat)
2. [Layihəni qurma (1 dəfə)](#1-layihəni-qurma)
3. [Folder strukturu](#2-folder-strukturu)
4. [Dio API client + JWT interceptor](#3-dio-api-client--jwt-interceptor)
5. [Token saxlama (Secure Storage)](#4-token-saxlama-secure-storage)
6. [Auth servisi (register, login, logout)](#5-auth-servisi)
7. [Hər ekran üçün endpoint və kod nümunələri](#6-hər-ekran-üçün-endpoint-və-kod-nümunələri)
   - [Giriş (Login)](#61-giriş-login)
   - [Qeydiyyat (Register)](#62-qeydiyyat-register)
   - [Şifrəni unutmuşam + OTP](#63-şifrəni-unutmuşam--otp)
   - [Kitab siyahısı (Home/Search)](#64-kitab-siyahısı-homesearch)
   - [Kitab detalı + Favorit](#65-kitab-detalı--favorit)
   - [Səbət](#66-səbət)
   - [Sifariş yaratma (Checkout)](#67-sifariş-yaratma-checkout)
   - [Sifarişlərim](#68-sifarişlərim)
   - [Ünvanlarım](#69-ünvanlarım)
   - [Ödəniş kartları](#610-ödəniş-kartları)
   - [Profil](#611-profil)
   - [Bildirişlər](#612-bildirişlər)
   - [Parametrlər](#613-parametrlər)
8. [Xətalar və status kodları](#7-xətalar-və-status-kodları)
9. [Cold start (Render free tier)](#8-cold-start-render-free-tier)
10. [Test üçün hazır endpoint və hesab](#9-test-üçün-hazır-hesab)

---

## Əsas məlumat

| Parametr | Dəyər |
|---|---|
| **Base URL** | `https://kitab-bazari-api.onrender.com` |
| **API prefix** | `/api` |
| **Swagger UI** | <https://kitab-bazari-api.onrender.com/swagger> |
| **Auth tipi** | JWT Bearer Token |
| **Content-Type** | `application/json` |
| **Currency** | AZN (Azərbaycan manatı) |
| **Language** | az |

**Tam endpoint nümunəsi**:
```
POST https://kitab-bazari-api.onrender.com/api/auth/login
```

**Auth header** (qorunan endpoint-lər üçün):
```
Authorization: Bearer eyJhbGciOiJIUzI1NiI...
```

> 💡 **Swagger UI-yə girib bütün endpoint-ləri brauzerdən test edə bilərsən** — orada hər endpoint üçün request/response misalı var. Sənədin sonunda Swagger üzərində "Try it out" istifadəsi izah olunub.

---

## 1. Layihəni qurma

### a) Lazımi paketlər

`pubspec.yaml`-a bu paketləri əlavə et:

```yaml
dependencies:
  flutter:
    sdk: flutter
  dio: ^5.7.0                      # HTTP client
  flutter_secure_storage: ^9.2.2   # Token-ləri təhlükəsiz saxlamaq üçün
  pretty_dio_logger: ^1.4.0        # Debug üçün gözəl log (istəyə bağlı)
  intl: ^0.19.0                    # Tarix formatı üçün
```

Sonra terminalda:

```bash
flutter pub get
```

### b) Android — internet icazəsi

`android/app/src/main/AndroidManifest.xml`-də `<manifest>` daxilində:

```xml
<uses-permission android:name="android.permission.INTERNET" />
```

### c) iOS — App Transport Security (yalnız debug-da)

Render API HTTPS olduğu üçün xüsusi konfiqurasiya **lazım deyil** ✓

---

## 2. Folder strukturu

Layihədə belə struktur yarat:

```
lib/
├── core/
│   ├── api/
│   │   ├── api_client.dart       # Dio quraşdırması + interceptor
│   │   ├── api_exception.dart    # Xəta wrapper
│   │   └── endpoints.dart        # URL sabitləri
│   └── storage/
│       └── secure_storage.dart   # Token saxlama
├── data/
│   ├── models/                   # JSON modelləri (User, Book, Order...)
│   └── services/                 # API servisləri (auth, books, cart...)
└── ui/
    └── screens/                  # Ekranlar
```

---

## 3. Dio API client + JWT interceptor

### `lib/core/api/endpoints.dart`

```dart
class ApiEndpoints {
  static const String baseUrl = 'https://kitab-bazari-api.onrender.com';
  static const String apiPrefix = '/api';

  // Auth
  static const String register = '$apiPrefix/auth/register';
  static const String login = '$apiPrefix/auth/login';
  static const String refresh = '$apiPrefix/auth/refresh';
  static const String logout = '$apiPrefix/auth/logout';
  static const String forgotPassword = '$apiPrefix/auth/forgot-password';
  static const String verifyOtp = '$apiPrefix/auth/verify-otp';
  static const String resendOtp = '$apiPrefix/auth/resend-otp';
  static const String resetPassword = '$apiPrefix/auth/reset-password';

  // Books
  static const String books = '$apiPrefix/books';
  static const String featuredBooks = '$apiPrefix/books/featured';
  static const String searchBooks = '$apiPrefix/books/search';
  static String booksByCategory(String id) => '$apiPrefix/books/by-category/$id';
  static String book(String id) => '$apiPrefix/books/$id';

  // Categories
  static const String categories = '$apiPrefix/categories';

  // Favorites
  static const String favorites = '$apiPrefix/favorites';
  static String favorite(String bookId) => '$apiPrefix/favorites/$bookId';

  // Cart
  static const String cart = '$apiPrefix/cart';
  static const String cartItems = '$apiPrefix/cart/items';
  static String cartItem(String bookId) => '$apiPrefix/cart/items/$bookId';
  static const String applyPromo = '$apiPrefix/cart/apply-promo';
  static const String cartPromo = '$apiPrefix/cart/promo';

  // Orders
  static const String orders = '$apiPrefix/orders';
  static String order(String id) => '$apiPrefix/orders/$id';
  static String cancelOrder(String id) => '$apiPrefix/orders/$id/cancel';
  static String repeatOrder(String id) => '$apiPrefix/orders/$id/repeat';
  static String trackOrder(String id) => '$apiPrefix/orders/$id/track';

  // Addresses
  static const String addresses = '$apiPrefix/addresses';
  static String address(String id) => '$apiPrefix/addresses/$id';
  static String setDefaultAddress(String id) => '$apiPrefix/addresses/$id/set-default';

  // Payment cards
  static const String paymentCards = '$apiPrefix/payment-cards';
  static String paymentCard(String id) => '$apiPrefix/payment-cards/$id';
  static String setDefaultCard(String id) => '$apiPrefix/payment-cards/$id/set-default';

  // Profile
  static const String profile = '$apiPrefix/profile';
  static const String profileAvatar = '$apiPrefix/profile/avatar';
  static const String changePassword = '$apiPrefix/profile/change-password';
  static const String profileStats = '$apiPrefix/profile/stats';

  // Notifications
  static const String notifications = '$apiPrefix/notifications';
  static const String unreadCount = '$apiPrefix/notifications/unread-count';
  static String markRead(String id) => '$apiPrefix/notifications/$id/read';
  static const String markAllRead = '$apiPrefix/notifications/mark-all-read';

  // Reviews
  static const String reviews = '$apiPrefix/reviews';
  static String reviewsByBook(String bookId) => '$apiPrefix/reviews/book/$bookId';

  // Settings
  static const String settings = '$apiPrefix/settings';

  // Promo
  static String validatePromo(String code) => '$apiPrefix/promo/validate/$code';
}
```

### `lib/core/api/api_exception.dart`

```dart
class ApiException implements Exception {
  final int statusCode;
  final String code;
  final String message;

  ApiException({
    required this.statusCode,
    required this.code,
    required this.message,
  });

  @override
  String toString() => message;
}
```

### `lib/core/api/api_client.dart`

Singleton Dio client. JWT-ni avtomatik header-ə əlavə edir, **401**-də avtomatik refresh edir.

```dart
import 'package:dio/dio.dart';
import 'package:pretty_dio_logger/pretty_dio_logger.dart';
import '../storage/secure_storage.dart';
import 'api_exception.dart';
import 'endpoints.dart';

class ApiClient {
  static final ApiClient _instance = ApiClient._internal();
  factory ApiClient() => _instance;

  late final Dio dio;
  final SecureStorage _storage = SecureStorage();
  bool _isRefreshing = false;

  ApiClient._internal() {
    dio = Dio(BaseOptions(
      baseUrl: ApiEndpoints.baseUrl,
      connectTimeout: const Duration(seconds: 60),  // cold start üçün uzun timeout
      receiveTimeout: const Duration(seconds: 30),
      headers: {'Content-Type': 'application/json'},
    ));

    // Debug log (release-də söndür)
    dio.interceptors.add(PrettyDioLogger(
      requestHeader: true,
      requestBody: true,
      responseBody: true,
      error: true,
      compact: true,
    ));

    // JWT + auto-refresh interceptor
    dio.interceptors.add(InterceptorsWrapper(
      onRequest: (options, handler) async {
        final token = await _storage.getAccessToken();
        if (token != null && token.isNotEmpty) {
          options.headers['Authorization'] = 'Bearer $token';
        }
        handler.next(options);
      },
      onError: (DioException e, handler) async {
        // 401 alınanda refresh cəhdi
        if (e.response?.statusCode == 401 && !_isRefreshing) {
          _isRefreshing = true;
          try {
            final refreshToken = await _storage.getRefreshToken();
            if (refreshToken != null) {
              final res = await Dio().post(
                '${ApiEndpoints.baseUrl}${ApiEndpoints.refresh}',
                data: {'refreshToken': refreshToken},
              );
              await _storage.saveTokens(
                accessToken: res.data['accessToken'],
                refreshToken: res.data['refreshToken'],
              );
              // Original sorğunu yenidən cəhd et
              final clonedRequest = await dio.fetch(
                e.requestOptions
                  ..headers['Authorization'] = 'Bearer ${res.data['accessToken']}',
              );
              _isRefreshing = false;
              return handler.resolve(clonedRequest);
            }
          } catch (_) {
            // Refresh də alınmadı — logout
            await _storage.clear();
          }
          _isRefreshing = false;
        }

        // Backend error JSON-unu standart formada qaytarırıq
        final data = e.response?.data;
        if (data is Map && data['error'] != null) {
          return handler.reject(DioException(
            requestOptions: e.requestOptions,
            response: e.response,
            error: ApiException(
              statusCode: e.response?.statusCode ?? 500,
              code: data['error']['code'] ?? 'UNKNOWN',
              message: data['error']['message'] ?? 'Naməlum xəta',
            ),
          ));
        }
        handler.next(e);
      },
    ));
  }
}
```

---

## 4. Token saxlama (Secure Storage)

### `lib/core/storage/secure_storage.dart`

```dart
import 'package:flutter_secure_storage/flutter_secure_storage.dart';

class SecureStorage {
  static const _accessTokenKey = 'access_token';
  static const _refreshTokenKey = 'refresh_token';
  static const _userIdKey = 'user_id';

  final _storage = const FlutterSecureStorage();

  Future<void> saveTokens({
    required String accessToken,
    required String refreshToken,
  }) async {
    await _storage.write(key: _accessTokenKey, value: accessToken);
    await _storage.write(key: _refreshTokenKey, value: refreshToken);
  }

  Future<String?> getAccessToken() => _storage.read(key: _accessTokenKey);
  Future<String?> getRefreshToken() => _storage.read(key: _refreshTokenKey);

  Future<void> saveUserId(String id) => _storage.write(key: _userIdKey, value: id);
  Future<String?> getUserId() => _storage.read(key: _userIdKey);

  Future<bool> isLoggedIn() async {
    final token = await getAccessToken();
    return token != null && token.isNotEmpty;
  }

  Future<void> clear() => _storage.deleteAll();
}
```

---

## 5. Auth servisi

### `lib/data/services/auth_service.dart`

```dart
import '../../core/api/api_client.dart';
import '../../core/api/endpoints.dart';
import '../../core/storage/secure_storage.dart';

class AuthService {
  final _api = ApiClient();
  final _storage = SecureStorage();

  /// Qeydiyyat
  Future<Map<String, dynamic>> register({
    required String fullName,
    required String email,
    required String password,
    String? phone,
  }) async {
    final res = await _api.dio.post(ApiEndpoints.register, data: {
      'fullName': fullName,
      'email': email,
      'phone': phone,
      'password': password,
      'acceptTerms': true,
    });
    await _saveSession(res.data);
    return res.data['user'];
  }

  /// Giriş
  Future<Map<String, dynamic>> login(String email, String password) async {
    final res = await _api.dio.post(ApiEndpoints.login, data: {
      'email': email,
      'password': password,
    });
    await _saveSession(res.data);
    return res.data['user'];
  }

  /// Çıxış
  Future<void> logout() async {
    try {
      final refresh = await _storage.getRefreshToken();
      if (refresh != null) {
        await _api.dio.post(ApiEndpoints.logout, data: {'refreshToken': refresh});
      }
    } catch (_) {/* offline də olsa lokal clear et */}
    await _storage.clear();
  }

  /// Şifrəni unutmuşam — email-ə OTP göndərilir
  Future<void> forgotPassword(String email) async {
    await _api.dio.post(ApiEndpoints.forgotPassword, data: {'email': email});
  }

  /// OTP-ni təsdiqlə — reset token qaytarır
  Future<String> verifyOtp(String email, String code) async {
    final res = await _api.dio.post(ApiEndpoints.verifyOtp, data: {
      'target': email,
      'code': code,
      'purpose': 'PasswordReset',
    });
    return res.data['resetToken'] as String;
  }

  /// Yeni şifrə təyin et
  Future<void> resetPassword({
    required String email,
    required String resetToken,
    required String newPassword,
  }) async {
    await _api.dio.post(ApiEndpoints.resetPassword, data: {
      'email': email,
      'resetToken': resetToken,
      'newPassword': newPassword,
    });
  }

  Future<void> _saveSession(Map<String, dynamic> data) async {
    await _storage.saveTokens(
      accessToken: data['accessToken'],
      refreshToken: data['refreshToken'],
    );
    await _storage.saveUserId(data['user']['id']);
  }
}
```

---

## 6. Hər ekran üçün endpoint və kod nümunələri

### 6.1 Giriş (Login)

**Stitch ekranı**: `giri/screen.png`
**Endpoint**: `POST /api/auth/login`

**Request**:
```json
{
  "email": "test@kitabbazari.az",
  "password": "Test1234!"
}
```

**Response (200)**:
```json
{
  "accessToken": "eyJhbGciOi...",
  "refreshToken": "FHGDNoK_Hm...",
  "expiresAt": "2026-05-19T12:35:02Z",
  "user": {
    "id": "6a0c3cd1dd011a6f325d21f9",
    "fullName": "Test User",
    "email": "test@kitabbazari.az",
    "avatarUrl": null,
    "role": "user"
  }
}
```

**Flutter**:
```dart
final auth = AuthService();
try {
  final user = await auth.login(emailController.text, passwordController.text);
  // Ana səhifəyə keç
  Navigator.pushReplacementNamed(context, '/home');
} on DioException catch (e) {
  final err = e.error as ApiException?;
  ScaffoldMessenger.of(context).showSnackBar(
    SnackBar(content: Text(err?.message ?? 'Giriş alınmadı')),
  );
}
```

---

### 6.2 Qeydiyyat (Register)

**Stitch ekranı**: `qeydiyyat/screen.png`
**Endpoint**: `POST /api/auth/register`

**Request**:
```json
{
  "fullName": "Elvin Məmmədov",
  "email": "elvin@example.az",
  "phone": "+994501234567",
  "password": "GucluSifre123!",
  "acceptTerms": true
}
```

Response **AuthResponse** ilə eynidir (yuxarıda).

**Flutter**:
```dart
final user = await auth.register(
  fullName: nameController.text,
  email: emailController.text,
  password: passwordController.text,
  phone: phoneController.text,
);
```

> ⚠️ `acceptTerms: true` məcburidir, yoxsa 400 xətası gəlir.

---

### 6.3 Şifrəni unutmuşam + OTP

**Stitch ekranları**: `ifr_ni_unutmu_am`, `otp_t_sdiql_m`, `otp_t_sdiql_m_x_ta`

**3 mərhələli axın**:

#### Mərhələ 1: Email-ə OTP göndər
```dart
await auth.forgotPassword(emailController.text);
// Növbəti ekrana keç (OTP daxil etmə)
```

> 💡 **Development modunda** API cavabında `devCode` sahəsi olur — bu test üçündür. Production-da SMS/Email göndərmə inteqrasiyası lazımdır.

#### Mərhələ 2: OTP-ni təsdiqlə
```dart
try {
  final resetToken = await auth.verifyOtp(email, otpCodeController.text);
  // resetToken-i saxla, yeni şifrə ekranına keç
} on DioException catch (e) {
  // "Daxil etdiyiniz kod yanlışdır" — error state göstər
}
```

#### Mərhələ 3: Yeni şifrə təyin et
```dart
await auth.resetPassword(
  email: email,
  resetToken: resetToken,
  newPassword: newPasswordController.text,
);
// Login ekranına yönləndir
```

---

### 6.4 Kitab siyahısı (Home/Search)

**Stitch ekranları**: home (kitab kartı şəkilləri), search bar
**Endpoint**: `GET /api/books`

**Query parametrləri** (hamısı optional):

| Parametr | Tip | Misal |
|---|---|---|
| `search` | string | `dədə qorqud` |
| `categoryId` | string | `6a0c...` |
| `minPrice` | decimal | `10` |
| `maxPrice` | decimal | `50` |
| `featured` | bool | `true` |
| `sort` | enum | `newest` \| `price_asc` \| `price_desc` \| `rating` |
| `page` | int | `1` |
| `pageSize` | int | `20` (max 100) |

**Response**:
```json
{
  "items": [
    {
      "id": "6a0c1f2...",
      "title": "Dədə Qorqud",
      "author": "Anar Rzayev",
      "description": "...",
      "price": 12.50,
      "currency": "AZN",
      "coverImageUrl": "https://...",
      "categoryId": "...",
      "categoryName": "Klassika",
      "stock": 10,
      "rating": 4.5,
      "reviewCount": 12,
      "isFavorited": false
    }
  ],
  "totalCount": 50,
  "page": 1,
  "pageSize": 20,
  "totalPages": 3
}
```

**Flutter**:
```dart
class BooksService {
  final _api = ApiClient();

  Future<Map<String, dynamic>> list({
    String? search,
    String? categoryId,
    String sort = 'newest',
    int page = 1,
    int pageSize = 20,
  }) async {
    final res = await _api.dio.get(ApiEndpoints.books, queryParameters: {
      if (search != null && search.isNotEmpty) 'search': search,
      if (categoryId != null) 'categoryId': categoryId,
      'sort': sort,
      'page': page,
      'pageSize': pageSize,
    });
    return res.data;
  }

  Future<List<dynamic>> featured({int limit = 10}) async {
    final res = await _api.dio.get(ApiEndpoints.featuredBooks,
        queryParameters: {'limit': limit});
    return res.data as List;
  }

  Future<List<dynamic>> categories() async {
    final res = await _api.dio.get(ApiEndpoints.categories);
    return res.data as List;
  }
}
```

**Pagination üçün infinite scroll**:
```dart
int _page = 1;
final List<dynamic> _books = [];
bool _hasMore = true;

Future<void> _loadMore() async {
  if (!_hasMore) return;
  final data = await booksService.list(page: _page);
  setState(() {
    _books.addAll(data['items']);
    _hasMore = _page < data['totalPages'];
    _page++;
  });
}
```

---

### 6.5 Kitab detalı + Favorit

**Stitch ekran**: `favoril_r/screen.png` (favoritlər ekranı)
**Endpoint-lər**:
- `GET /api/books/{id}` — kitab detalı
- `POST /api/favorites/{bookId}` — favoritə əlavə et
- `DELETE /api/favorites/{bookId}` — favoritdən sil
- `GET /api/favorites` — favorit kitabları siyahısı

**Flutter**:
```dart
class FavoritesService {
  final _api = ApiClient();

  Future<List<dynamic>> list() async {
    final res = await _api.dio.get(ApiEndpoints.favorites);
    return res.data as List;
  }

  Future<void> add(String bookId) async {
    await _api.dio.post(ApiEndpoints.favorite(bookId));
  }

  Future<void> remove(String bookId) async {
    await _api.dio.delete(ApiEndpoints.favorite(bookId));
  }

  Future<void> toggle(String bookId, bool isFavorited) async {
    if (isFavorited) {
      await remove(bookId);
    } else {
      await add(bookId);
    }
  }
}
```

**UI**:
```dart
IconButton(
  icon: Icon(book.isFavorited ? Icons.favorite : Icons.favorite_border,
      color: book.isFavorited ? Colors.red : Colors.grey),
  onPressed: () async {
    await favService.toggle(book.id, book.isFavorited);
    setState(() => book.isFavorited = !book.isFavorited);
  },
)
```

---

### 6.6 Səbət

**Stitch ekranları**: `s_b_t_1`, `s_b_t_2`, `s_b_t_3`, `bo_s_b_t` (boş səbət)

**Endpoint-lər**:
| Action | Endpoint |
|---|---|
| Səbəti gətir | `GET /api/cart` |
| Kitab əlavə et | `POST /api/cart/items` |
| Miqdarı dəyiş | `PUT /api/cart/items/{bookId}` |
| Kitabı çıxar | `DELETE /api/cart/items/{bookId}` |
| Səbəti təmizlə | `DELETE /api/cart` |
| Promokod tətbiq et | `POST /api/cart/apply-promo` |
| Promokodu sil | `DELETE /api/cart/promo` |

**Səbət cavabı**:
```json
{
  "id": "...",
  "items": [
    {
      "bookId": "...",
      "title": "Dədə Qorqud",
      "author": "Anar Rzayev",
      "coverImageUrl": "...",
      "price": 12.50,
      "quantity": 1,
      "lineTotal": 12.50,
      "stock": 10
    }
  ],
  "subtotal": 42.50,
  "discount": 5.00,
  "total": 37.50,
  "appliedPromoCode": "KITAB10",
  "itemCount": 3
}
```

**Flutter**:
```dart
class CartService {
  final _api = ApiClient();

  Future<Map<String, dynamic>> get() async =>
      (await _api.dio.get(ApiEndpoints.cart)).data;

  Future<Map<String, dynamic>> addItem(String bookId, {int quantity = 1}) async {
    final res = await _api.dio.post(ApiEndpoints.cartItems,
        data: {'bookId': bookId, 'quantity': quantity});
    return res.data;
  }

  Future<Map<String, dynamic>> updateQuantity(String bookId, int quantity) async {
    // quantity = 0 olsa, item-i silir
    final res = await _api.dio.put(ApiEndpoints.cartItem(bookId),
        data: {'quantity': quantity});
    return res.data;
  }

  Future<Map<String, dynamic>> removeItem(String bookId) async =>
      (await _api.dio.delete(ApiEndpoints.cartItem(bookId))).data;

  Future<Map<String, dynamic>> clear() async =>
      (await _api.dio.delete(ApiEndpoints.cart)).data;

  Future<Map<String, dynamic>> applyPromo(String code) async {
    final res = await _api.dio.post(ApiEndpoints.applyPromo, data: {'code': code});
    return res.data;
  }
}
```

---

### 6.7 Sifariş yaratma (Checkout)

**Stitch ekran**: `sifari_i_tamamla/screen.png` (checkout), `sifari_u_urla_tamamland` (success)

**Axın**: Səbət → Çatdırılma ünvanı seç → Çatdırılma üsulu → Ödəniş üsulu → Sifariş təsdiqlə

**Endpoint**: `POST /api/orders`

**Request**:
```json
{
  "addressId": "6a0c1f2...",
  "deliveryMethod": "Standard",
  "paymentMethod": "Card",
  "paymentCardId": "6a0c1f3...",
  "promoCode": "KITAB10"
}
```

| Sahə | Mümkün dəyərlər |
|---|---|
| `deliveryMethod` | `Standard` (pulsuz, 2-3 gün) \| `Express` (5 AZN, bugün) |
| `paymentMethod` | `Card` \| `CashOnDelivery` \| `MilliOn` |
| `paymentCardId` | yalnız `Card` üçün |
| `promoCode` | optional — və ya səbətdə artıq tətbiq olunubsa boş buraxa bilərsən |

**Response (Order)**:
```json
{
  "id": "...",
  "orderNumber": "KB-90421",
  "items": [...],
  "deliveryAddress": {...},
  "deliveryMethod": "Standard",
  "paymentMethod": "Card",
  "subtotal": 42.50,
  "deliveryFee": 0,
  "discount": 5.00,
  "total": 37.50,
  "status": "Confirmed",
  "estimatedDeliveryAt": "2026-05-21T18:00:00Z",
  "createdAt": "2026-05-19T10:35:00Z"
}
```

**Flutter**:
```dart
class OrdersService {
  final _api = ApiClient();

  Future<Map<String, dynamic>> create({
    required String addressId,
    String deliveryMethod = 'Standard',
    String paymentMethod = 'Card',
    String? paymentCardId,
    String? promoCode,
  }) async {
    final res = await _api.dio.post(ApiEndpoints.orders, data: {
      'addressId': addressId,
      'deliveryMethod': deliveryMethod,
      'paymentMethod': paymentMethod,
      if (paymentCardId != null) 'paymentCardId': paymentCardId,
      if (promoCode != null) 'promoCode': promoCode,
    });
    return res.data;
  }
}
```

**Uğur ekranı üçün** sifariş cavabından `orderNumber` və `estimatedDeliveryAt` istifadə et.

---

### 6.8 Sifarişlərim

**Stitch ekran**: `sifari_l_rim/screen.png`

**Endpoint**: `GET /api/orders?status={all|active|delivered|cancelled}&page=1`

```dart
Future<Map<String, dynamic>> listOrders({
  String status = 'all',  // all | active | delivered | cancelled
  int page = 1,
}) async {
  final res = await _api.dio.get(ApiEndpoints.orders, queryParameters: {
    'status': status,
    'page': page,
  });
  return res.data;
}

// İzləmə
Future<Map<String, dynamic>> track(String orderId) async =>
    (await _api.dio.get(ApiEndpoints.trackOrder(orderId))).data;

// "Təkrarla" — eyni məhsulları yeni səbətə qoyur
Future<Map<String, dynamic>> repeat(String orderId) async =>
    (await _api.dio.post(ApiEndpoints.repeatOrder(orderId))).data;

// Ləğv
Future<Map<String, dynamic>> cancel(String orderId) async =>
    (await _api.dio.post(ApiEndpoints.cancelOrder(orderId))).data;
```

**Status enum-ları**:
- `Pending`, `Confirmed`, `Preparing`, `InTransit`, `Delivered`, `Cancelled`

Track response (`/api/orders/{id}/track`):
```json
{
  "orderNumber": "KB-90421",
  "status": "InTransit",
  "steps": [
    {"status": "Pending", "label": "Qəbul edildi", "completed": true, "at": "..."},
    {"status": "Confirmed", "label": "Təsdiqləndi", "completed": true},
    {"status": "Preparing", "label": "Hazırlanır", "completed": true},
    {"status": "InTransit", "label": "Yoldadır", "completed": true},
    {"status": "Delivered", "label": "Çatdırıldı", "completed": false}
  ],
  "estimatedDeliveryAt": "2026-05-21T18:00:00Z"
}
```

---

### 6.9 Ünvanlarım

**Stitch ekran**: `nvanlar_m/screen.png`

```dart
class AddressService {
  final _api = ApiClient();

  Future<List<dynamic>> list() async =>
      (await _api.dio.get(ApiEndpoints.addresses)).data as List;

  Future<Map<String, dynamic>> create({
    required String label,      // "Ev", "İş", "Other"
    required String streetLine,
    required String district,
    required String city,
    String country = 'Azərbaycan',
    String? phone,
    bool isDefault = false,
  }) async {
    final res = await _api.dio.post(ApiEndpoints.addresses, data: {
      'label': label,
      'streetLine': streetLine,
      'district': district,
      'city': city,
      'country': country,
      'phone': phone,
      'isDefault': isDefault,
    });
    return res.data;
  }

  Future<void> update(String id, Map<String, dynamic> data) async {
    await _api.dio.put(ApiEndpoints.address(id), data: data);
  }

  Future<void> delete(String id) async {
    await _api.dio.delete(ApiEndpoints.address(id));
  }

  Future<void> setDefault(String id) async {
    await _api.dio.put(ApiEndpoints.setDefaultAddress(id));
  }
}
```

---

### 6.10 Ödəniş kartları

**Stitch ekran**: `d_ni_sullar/screen.png`

> ⚠️ **Vacib**: Real production-da kart məlumatları **birbaşa backend-ə göndərilməməlidir** (PCI compliance). Real layihə üçün Stripe / Adyen / 2C2P kimi PSP istifadə edilməlidir. Bu API demo məqsədilə kartı qəbul edir və yalnız **son 4 rəqəm**i saxlayır — pul axını yoxdur.

```dart
class PaymentCardsService {
  final _api = ApiClient();

  Future<List<dynamic>> list() async =>
      (await _api.dio.get(ApiEndpoints.paymentCards)).data as List;

  Future<Map<String, dynamic>> add({
    required String cardNumber,    // tam nömrə (back-end yalnız son 4-ünü saxlayır)
    required String holderName,
    required int expiryMonth,
    required int expiryYear,
    required String cvv,
    bool setAsDefault = false,
  }) async {
    final res = await _api.dio.post(ApiEndpoints.paymentCards, data: {
      'cardNumber': cardNumber,
      'holderName': holderName,
      'expiryMonth': expiryMonth,
      'expiryYear': expiryYear,
      'cvv': cvv,
      'setAsDefault': setAsDefault,
    });
    return res.data;
  }

  Future<void> delete(String id) async {
    await _api.dio.delete(ApiEndpoints.paymentCard(id));
  }

  Future<void> setDefault(String id) async {
    await _api.dio.put(ApiEndpoints.setDefaultCard(id));
  }
}
```

---

### 6.11 Profil

**Stitch ekranlar**: `profil`, `yeni_profil_dizayn`

```dart
class ProfileService {
  final _api = ApiClient();

  /// Profil + stats
  Future<Map<String, dynamic>> get() async =>
      (await _api.dio.get(ApiEndpoints.profile)).data;

  Future<Map<String, dynamic>> update({
    String? fullName,
    String? phone,
    String? avatarUrl,
  }) async {
    final res = await _api.dio.put(ApiEndpoints.profile, data: {
      if (fullName != null) 'fullName': fullName,
      if (phone != null) 'phone': phone,
      if (avatarUrl != null) 'avatarUrl': avatarUrl,
    });
    return res.data;
  }

  Future<void> changePassword(String current, String newPass) async {
    await _api.dio.put(ApiEndpoints.changePassword, data: {
      'currentPassword': current,
      'newPassword': newPass,
    });
  }

  /// Hesabı sil
  Future<void> deleteAccount() async {
    await _api.dio.delete(ApiEndpoints.profile);
  }
}
```

Profil cavabı:
```json
{
  "id": "...",
  "fullName": "Elvin Məmmədov",
  "email": "elvin@example.az",
  "phone": "+994501234567",
  "avatarUrl": null,
  "emailVerified": false,
  "stats": {
    "booksPurchased": 12,
    "favoritesCount": 5,
    "ordersCount": 7
  },
  "createdAt": "2026-05-01T..."
}
```

---

### 6.12 Bildirişlər

**Stitch ekran**: `notifications`

```dart
class NotificationsService {
  final _api = ApiClient();

  Future<List<dynamic>> list() async =>
      (await _api.dio.get(ApiEndpoints.notifications)).data as List;

  Future<int> unreadCount() async {
    final res = await _api.dio.get(ApiEndpoints.unreadCount);
    return res.data['count'] as int;
  }

  Future<void> markRead(String id) async {
    await _api.dio.put(ApiEndpoints.markRead(id));
  }

  Future<void> markAllRead() async {
    await _api.dio.put(ApiEndpoints.markAllRead);
  }
}
```

**Bildiriş tipləri**: `PriceDrop`, `OrderShipped`, `OrderDelivered`, `NewArrival`, `Recommendation`, `Promo`, `System`

---

### 6.13 Parametrlər

**Stitch ekran**: `parametrl_r`

```dart
Future<Map<String, dynamic>> getSettings() async =>
    (await ApiClient().dio.get(ApiEndpoints.settings)).data;

Future<void> updateSettings({
  required bool notificationsEnabled,
  required String language,  // 'az' | 'en' | 'ru'
  required bool darkMode,
}) async {
  await ApiClient().dio.put(ApiEndpoints.settings, data: {
    'notificationsEnabled': notificationsEnabled,
    'language': language,
    'darkMode': darkMode,
  });
}
```

---

## 7. Xətalar və status kodları

API xətaları **standart formatda** qaytarır:

```json
{
  "error": {
    "code": "BAD_REQUEST",
    "message": "İstifadə şərtlərini qəbul etməlisiniz."
  }
}
```

| HTTP | `code` | Tipik səbəb |
|---|---|---|
| **400** | `BAD_REQUEST` | Yanlış input (məcburi sahə yox, format səhv) |
| **401** | `UNAUTHORIZED` | Token yoxdur / vaxtı bitib (auto-refresh işə düşür) |
| **403** | `FORBIDDEN` | Bu resurs sənə aid deyil |
| **404** | `NOT_FOUND` | Element tapılmadı |
| **409** | `CONFLICT` | Dublikat (məs: artıq qeydiyyatlı email) |
| **500** | `INTERNAL_ERROR` | Server xətası |

**Flutter-də UI-yə göstərmək**:
```dart
try {
  await someApiCall();
} on DioException catch (e) {
  final err = e.error as ApiException?;
  ScaffoldMessenger.of(context).showSnackBar(
    SnackBar(
      content: Text(err?.message ?? 'Xəta baş verdi'),
      backgroundColor: Colors.red,
    ),
  );
}
```

---

## 8. Cold start (Render free tier)

API **pulsuz** Render planda işləyir. Bu o deməkdir:

- 15 dəqiqə aktivlik olmasa server **yatır**
- İlk sorğu (oyandırmaq üçün) **~30 saniyə** çəkə bilər
- Sonrakı sorğular sürətli (~100ms)

**Mobile-də necə həll etmək**:

```dart
// 1) Splash screen-də API-ni "isit"
Future<void> warmUpApi() async {
  try {
    await ApiClient().dio.get('/health').timeout(const Duration(seconds: 45));
  } catch (_) {/* ignore */}
}

// 2) Connect timeout-u yüksək saxla (artıq 60sn-dir API client-də)

// 3) Loader göstər
if (isLoading) const CircularProgressIndicator()
```

---

## 9. Test üçün hazır hesab

Mən artıq bir test istifadəçi yaratdım, bunlarla login ola bilərsən:

| Sahə | Dəyər |
|---|---|
| **Email** | `test@kitabbazari.az` |
| **Password** | `Test1234!` |

Lakin tövsiyə edirəm **öz email-inlə qeydiyyatdan keç** və test et.

---

## 🎯 Tövsiyə edilən iş axını

1. **Swagger UI-yə gir** <https://kitab-bazari-api.onrender.com/swagger>
2. **`POST /api/auth/register`** açın → "Try it out" → öz email/password ilə qeydiyyatdan keç
3. Cavabdakı **`accessToken`**-i kopyala
4. Yuxarıda **"Authorize"** düyməsi var → ora `Bearer <token>` yapışdır
5. İndi qorunan endpoint-ləri də sınaya bilərsən (favorites, cart, orders və s.)
6. Flutter-də eyni axını implementasiya et

---

## 🆘 Problem yaşayanda

- **CORS xətası** — yoxdur, API hər origin-ə icazə verir ✓
- **Cold start çox uzun çəkir** — splash-də warm-up et (yuxarıdakı misal)
- **Token vaxtı bitib** — auto-refresh işə düşür (api_client.dart-da quraşdırılıb)
- **Validasiya xətaları** — `error.message` field-ində konkret mesaj gəlir (Azərbaycan dilində)
- **Endpoint sənədləri** — hər zaman Swagger-dən yoxla, ən düzgün mənbədir

---

## 📚 Faydalı linklər

- Swagger UI: <https://kitab-bazari-api.onrender.com/swagger>
- API GitHub repo: <https://github.com/mammadovmahammad03/kitab-bazari-api>
- Dio sənədi: <https://pub.dev/packages/dio>
- Flutter Secure Storage: <https://pub.dev/packages/flutter_secure_storage>

---

**Uğurlar! 💚📚** Suallar olsa, Mahammad-dan soruş.
