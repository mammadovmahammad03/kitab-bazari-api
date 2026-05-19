# Kitab Bazari API

ASP.NET Core 9 + MongoDB backend for the Kitab Bazari mobile app (designed in Google Stitch).

## Endpoints

All endpoints are documented at `/swagger` once the API is running.

Public:
- `POST /api/auth/register` — register new user
- `POST /api/auth/login` — login (returns JWT + refresh token)
- `POST /api/auth/refresh` — refresh access token
- `POST /api/auth/logout` — revoke refresh token
- `POST /api/auth/forgot-password` — send OTP to email
- `POST /api/auth/verify-otp` — exchange OTP for reset token
- `POST /api/auth/resend-otp` — resend OTP
- `POST /api/auth/reset-password` — set a new password
- `GET  /api/books` — list books (filters: search, categoryId, minPrice, maxPrice, featured, sort, page, pageSize)
- `GET  /api/books/{id}` — book details
- `GET  /api/books/featured` — featured books
- `GET  /api/books/search?q=` — full-text search
- `GET  /api/books/by-category/{categoryId}` — books by category
- `GET  /api/categories` — list categories with book counts
- `GET  /api/reviews/book/{bookId}` — public reviews
- `GET  /api/promo/validate/{code}?subtotal=` — validate promo

Authenticated (Bearer JWT required):
- `/api/favorites` (GET/POST/DELETE)
- `/api/cart` (GET, POST/PUT/DELETE items, apply/remove promo, clear)
- `/api/orders` (GET/POST, cancel, repeat, track)
- `/api/addresses` (GET/POST/PUT/DELETE, set-default)
- `/api/payment-cards` (GET/POST/DELETE, set-default)
- `/api/profile` (GET/PUT, change-password, avatar, stats, delete)
- `/api/settings` (GET/PUT — notifications, language, dark mode)
- `/api/notifications` (GET, mark read, mark all read, delete, unread-count)
- `/api/reviews` (POST)

Admin (`Role = admin`):
- `POST/PUT/DELETE /api/books`
- `POST/PUT/DELETE /api/categories`
- `POST/GET/DELETE /api/promo`
- `PUT /api/orders/{id}/status`

## Run locally

```bash
# 1) Start MongoDB (locally or use a connection string in appsettings.json)
docker run -d --name kitab-mongo -p 27017:27017 mongo:7

# 2) Run the API
dotnet run --project src/BooksApi
```

Open `http://localhost:5000/swagger` (port may differ — see launch output).

## Deploy

### Option A — Render (with Docker)

1. Push this repo to GitHub.
2. Create a free MongoDB Atlas cluster at <https://cloud.mongodb.com/> → get the connection string.
3. On <https://render.com/>, create a new **Web Service** → connect your GitHub repo → it auto-detects `render.yaml`.
4. Set the `MONGO_URI` env var to your Atlas connection string (it is marked `sync: false` in `render.yaml`, so you must paste it in the dashboard).
5. Deploy — Render will build the Dockerfile and give you a URL like `https://kitab-bazari-api.onrender.com`.

### Option B — Railway

1. Push to GitHub, then on <https://railway.app/> create a new project from the repo.
2. Add a MongoDB plugin OR set `MONGO_URI` to Atlas.
3. Set env vars `MONGO_DB`, `JWT_KEY`, `JWT_ISSUER`, `JWT_AUDIENCE`.
4. Railway auto-builds the Dockerfile.

### Environment variables

| Name | Required | Default |
|---|---|---|
| `MONGO_URI` | yes | `mongodb://localhost:27017` |
| `MONGO_DB` | yes | `kitab_bazari` |
| `JWT_KEY` | yes (>= 32 chars) | dev key in appsettings |
| `JWT_ISSUER` | no | `KitabBazari` |
| `JWT_AUDIENCE` | no | `KitabBazariMobile` |
| `PORT` | no (Render/Railway sets it) | `8080` |
