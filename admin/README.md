# zouq admin

Vite + React + TypeScript dashboard for the zouq API.

## Run

1. Start the API on **http://localhost:5280**
2. In this folder:

```bash
npm install
npm run dev
```

3. Open **http://localhost:5174**
4. Sign in: `admin@zouq.app` / `Admin@12345`

Optional: copy `.env.example` → `.env` and set `VITE_API_BASE`.

## Sections

Dashboard · Products · Fabrics · Cuts · Sizes · Printing & embroidery · Design assets · Orders · Featured designs · Ads · Users

## API

Auth: `POST /api/auth/login` → Bearer `access_token` on `/api/admin/*`.

Responses: `{ status, message, data }` (snake_case).
