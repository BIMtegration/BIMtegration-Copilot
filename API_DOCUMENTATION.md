# API Documentation

## 1. Auth Server API
**URL**: `https://script.google.com/macros/s/AKfycbwZ9Qki-FSQzRNi_gr_kAMl02Rck78YQ_-6xB3R9nQ8_kFmWpwpKY1DwU-sThpjj2IL/exec`

### A. Login
Obtain a JWT token for use with other services.

- **Method**: `POST`
- **Content-Type**: `application/json`
- **Body**:
  ```json
  {
    "action": "login",
    "usuario": "YOUR_USERNAME",
    "clave": "YOUR_PASSWORD"
  }
  ```
- **Response**:
  ```json
  {
    "ok": true,
    "token": "eyJhbGciOiJIUzI1Ni...",
    "userData": { ... }
  }
  ```

### B. Register (Public)
Create a new user account (requires admin approval to become active).

- **Method**: `POST`
- **Body**:
  ```json
  {
    "action": "registro",
    "usuario": "NEW_USER",
    "clave": "PASSWORD",
    "nombre": "FULL NAME"
  }
  ```

### C. Validate Token
Check if a token is valid and active.

- **Method**: `POST`
- **Body**:
  ```json
  {
    "action": "validate",
    "token": "JWT_TOKEN_HERE"
  }
  ```

---

## 2. URL File Server API
**URL**: `https://script.google.com/macros/s/AKfycbyaMs0cBOK_f2TdzZ-fTxicCrBtl6VtnA3OASFBV97HLa1SVeFBn2TYrgZKluK4gaDC/exec`

### List Files in Folder
Retrieve a list of files from a specific Google Drive folder. Requires a valid token from the Auth Server.

- **Method**: `GET`
- **Parameters**:
  - `token`: Valid JWT token from Auth Server.
  - `folderId`: Google Drive Folder ID to list.
- **Example URL**:
  `https://script.google.com/.../exec?token=eyJhb...&folderId=10j9zaQKIL58zUD_SxWEBJW1gh8GRNEy7`

- **Response**:
  ```json
  {
    "success": true,
    "files": [
      {
        "name": "example.pdf",
        "id": "12345...",
        "url": "https://drive.usercontent.google.com/u/0/uc?id=12345...&export=download",
        "mimeType": "application/pdf",
        "size": 1024
      }
    ]
  }
  ```

### Error Handling
If the token is invalid or the secret is not synchronized, you will receive:
```json
{
  "success": false,
  "error": "Unauthorized: Invalid or expired token"
}
```

---

## 3. Data Server API
**URL**: `https://script.google.com/macros/s/AKfycbx8asivIkpIZcTmbyuPFbcitKi6h8YjbfQP9q0P0I8qnJpdtfQ4WIQnGdXPN6KH0S8pHg/exec`

### Authentication
Requires a valid JWT token from the Auth Server (same as URL File Server).
- Pass via query param: `?token=JWT_TOKEN`
- Or via JSON body: `{ "token": "JWT_TOKEN" }`
- **Note**: The legacy `DATA_SERVER_TOKEN` is no longer supported.

### A. Read Data (GET)
Reads all data from a specific sheet as a JSON array of objects.

- **Method**: `GET`
- **Parameters**:
  - `token`: Auth token.
  - `sheetId`: Google Spreadsheet ID.
  - `sheetName`: Name of the specific tab/sheet.
- **Example URL**:
  `https://script.google.com/.../exec?token=XYZ&sheetId=123...&sheetName=Sheet1`
- **Response**:
  ```json
  {
    "status": "ok",
    "data": [
      { "Col1": "Val1", "Col2": "Val2" },
      ...
    ]
  }
  ```

### B. Append Data (POST)
Appends rows to a sheet. Supports raw arrays or objects.

- **Method**: `POST`
- **Body (Option 1 - Array of Arrays)**:
  ```json
  {
    "token": "XYZ",
    "sheetId": "123...",
    "sheetName": "Sheet1",
    "values": [
      ["Row1Col1", "Row1Col2"],
      ["Row2Col1", "Row2Col2"]
    ]
  }
  ```
- **Body (Option 2 - Array of Objects)**:
  ```json
  {
    "token": "XYZ",
    "sheetId": "123...",
    "sheetName": "Sheet1",
    "records": [
      { "Header1": "Val1", "Header2": "Val2" }
    ]
  }
  ```
- **Response**:
  ```json
  {
    "status": "ok",
    "data": {
      "message": "2 fila(s) agregada(s)..."
    }
  }
  ```
