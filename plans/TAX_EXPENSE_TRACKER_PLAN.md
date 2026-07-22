# Tax Expense Tracker - Project Plan

## Project Overview
A web application to track and manage tax-deductible expenses with a focus on financial organization and reporting.

---

## Technology Stack

### Backend
- **Runtime**: .NET 10 / C#
- **Framework**: ASP.NET Core Web API
- **Database**: SQLite (development/initial), upgrade path to PostgreSQL or SQL Server
- **ORM**: Entity Framework Core
- **Deployment**: Direct Azure App Service deployment or local host

### Frontend
- **Framework**: Angular (latest LTS)
- **Build Tool**: Angular CLI
- **Styling**: Angular Material
- **HTTP Client**: Angular HttpClient

### Infrastructure
- **Database**: SQLite file-based (local dev), managed DB for cloud (optional)
- **Hosting**: Azure App Service (API) and Azure Static Web Apps (frontend)
- **Version Control**: Git

---

## Data Model

### Tracker Table (Reference Data)
```
Columns:
- Id (GUID/UUID) - Primary Key
- Name (string) - Tracker name (e.g., "Home Office", "Business Travel", "Equipment")
- Description (string) - Tracker description (optional/nullable)
- IsDeleted (bool) - Soft delete flag (default: false)
- CreatedAt (DateTime) - Record creation timestamp
- UpdatedAt (DateTime) - Last modification timestamp
```

### Seed Data
**Default Trackers:**
```
- H&R Block
- Pluralsight
- Udemy
- JB Hifi
- Office Works
```

### Tag/Category Table
```
Columns:
- Id (GUID/UUID) - Primary Key
- Name (string) - Tag/category name (e.g., "Deductible", "Equipment", "Travel")
- IsDeleted (bool) - Soft delete flag (default: false)
- CreatedAt (DateTime) - Record creation timestamp
```

### TaxExpenseTag Table (Junction)
```
Columns:
- Id (GUID/UUID) - Primary Key
- TaxExpenseId (GUID) - Foreign Key to TaxExpense
- TagId (GUID) - Foreign Key to Tag
```

### Tax Expense Table
```
Columns:
- Id (GUID/UUID) - Primary Key
- Item (string) - Category/name of expense (e.g., "Office Supplies")
- Description (string) - Detailed description
- Date (DateTime) - Date of expense
- Bank (string) - Payment method/bank (e.g., "Chase Visa", "Wells Fargo")
- Price (decimal) - Amount spent
- SourceId (GUID) - Foreign Key to Tracker table
- IsDeleted (bool) - Soft delete flag (default: false)
- CreatedAt (DateTime) - Record creation timestamp
- UpdatedAt (DateTime) - Last modification timestamp
```

### Relationships
- **TaxExpense -> Tracker**: Many-to-One (Multiple expenses can belong to one tracker/source)
- **TaxExpense -> Tag**: Many-to-Many (Multiple expenses can have multiple tags)
- **Query Filtering**: All queries exclude soft-deleted records by default (IsDeleted = false)

### Additional Considerations
- UserId (for multi-user support in future)
- Audit trail for soft-deleted records

---

## Project Structure

```
tax-expense-tracker/
├── Backend/
│   ├── TaxExpenseTracker.Api/
│   │   ├── Program.cs
│   │   ├── appsettings.json
│   │   ├── Controllers/
│   │   │   ├── ExpensesController.cs
│   │   │   ├── TrackersController.cs
│   │   │   └── TagsController.cs
│   │   ├── Models/
│   │   │   ├── TaxExpense.cs
│   │   │   ├── Tracker.cs
│   │   │   ├── Tag.cs
│   │   │   ├── CreateExpenseDto.cs
│   │   │   ├── ExpenseResponseDto.cs
│   │   │   └── TagDto.cs
│   │   ├── Data/
│   │   │   ├── AppDbContext.cs
│   │   │   └── Migrations/
│   │   ├── Services/
│   │   │   ├── ExpenseService.cs
│   │   │   ├── TrackerService.cs
│   │   │   └── TagService.cs
│   │   └── TaxExpenseTracker.Api.csproj
├── Frontend/
│   ├── src/
│   │   ├── app/
│   │   │   ├── components/
│   │   │   │   ├── expense-list/
│   │   │   │   ├── expense-form/
│   │   │   │   ├── expense-details/
│   │   │   │   ├── dashboard/
│   │   │   │   ├── tracker-management/
│   │   │   │   └── tag-management/
│   │   │   ├── services/
│   │   │   │   ├── expense.service.ts
│   │   │   │   ├── tracker.service.ts
│   │   │   │   └── tag.service.ts
│   │   │   └── app.module.ts
│   │   ├── index.html
│   │   └── main.ts
│   ├── package.json
│   ├── angular.json
│   └── proxy.conf.json
├── README.md
└── .gitignore
```

---

## API Endpoints

### Tracker Endpoints (Reference Data)

| Method | Endpoint | Purpose |
|--------|----------|---------|
| GET | `/api/trackers` | List all trackers (excluding soft-deleted) |
| GET | `/api/trackers/{id}` | Get single tracker |
| POST | `/api/trackers` | Create new tracker |
| PUT | `/api/trackers/{id}` | Update tracker |
| DELETE | `/api/trackers/{id}` | Soft delete tracker |

### Tag Endpoints

| Method | Endpoint | Purpose |
|--------|----------|----------|
| GET | `/api/tags` | List all tags (excluding soft-deleted) |
| GET | `/api/tags/{id}` | Get single tag |
| POST | `/api/tags` | Create new tag |
| PUT | `/api/tags/{id}` | Update tag |
| DELETE | `/api/tags/{id}` | Soft delete tag |

### Expense Endpoints

| Method | Endpoint | Purpose |
|--------|----------|---------|
| GET | `/api/expenses` | List all expenses (excluding soft-deleted, with pagination) |
| GET | `/api/expenses/{id}` | Get single expense |
| POST | `/api/expenses` | Create new expense |
| PUT | `/api/expenses/{id}` | Update expense |
| DELETE | `/api/expenses/{id}` | Soft delete expense |
| GET | `/api/expenses/summary` | Summary statistics (total spent, by bank, etc.) |
| GET | `/api/expenses/filter` | Filter by date range, bank, amount, tracker, or tags |

### Request/Response Models

**TrackerDto:**
```json
{
  "id": "guid",
  "name": "Home Office",
  "description": "Home office deduction expenses",
  "createdAt": "2024-07-22T10:30:00Z"
}
```

**CreateTrackerDto:**
```json
{
  "name": "Home Office",
  "description": "Home office deduction expenses"
}
```

**TagDto:**
```json
{
  "id": "guid",
  "name": "Deductible",
  "createdAt": "2024-07-22T10:30:00Z"
}
```

**CreateTagDto:**
```json
{
  "name": "Deductible"
}
```

**CreateExpenseDto:**
```json
{
  "item": "Office Supplies",
  "description": "Printer ink and paper",
  "date": "2024-07-22",
  "bank": "Chase Visa",
  "price": 45.99,
  "sourceId": "tracker-guid",
  "tagIds": ["tag-guid-1", "tag-guid-2"]
}
```

**ExpenseResponse:**
```json
{
  "id": "guid",
  "item": "Office Supplies",
  "description": "Printer ink and paper",
  "date": "2024-07-22",
  "bank": "Chase Visa",
  "price": 45.99,
  "sourceId": "tracker-guid",
  "source": {
    "id": "tracker-guid",
    "name": "Home Office",
    "description": "Home office deduction expenses"
  },
  "tags": [
    {
      "id": "tag-guid-1",
      "name": "Deductible"
    },
    {
      "id": "tag-guid-2",
      "name": "Equipment"
    }
  ],
  "createdAt": "2024-07-22T10:30:00Z",
  "updatedAt": "2024-07-22T10:30:00Z"
}
```

---

## Frontend Components

### Angular Component Hierarchy
```
AppComponent
├── NavbarComponent
├── DashboardComponent
│   ├── SummaryCardsComponent
│   └── ChartComponent (expense trends)
├── TrackerManagementComponent
│   ├── TrackerListComponent
│   └── TrackerFormComponent
├── TagManagementComponent
│   ├── TagListComponent
│   └── TagFormComponent
├── ExpenseListComponent
│   ├── ExpenseTableComponent
│   │   └── ExpenseRowComponent (displays source badge)
│   └── FilterComponent
├── ExpenseFormComponent (Create/Edit modal)
└── ExpenseDetailsComponent
```

### Key Features
- **Tracker Management**: Create and manage expense sources/categories with soft delete
- **Tag Management**: Create and manage expense tags/categories with soft delete
- **Expense List**: Table view with source badge and tags, sorting, filtering, pagination
- **Add/Edit Form**: Modal form with tracker dropdown and multi-select tag selector
- **Dashboard**: Summary statistics grouped by tracker and charts
- **Filters**: By date range, bank, source/tracker, amount range, and tags
- **Soft Delete**: All records support soft deletion, accessible via query params if needed
- **Export**: CSV export functionality (future)

---

## Development Phases

### Phase 1: Setup & Core Backend (Week 1)
- [x] Create .NET Core API project
- [x] Set up SQLite database with EF Core
- [x] Create Tracker model with soft delete and DbContext
- [x] Create Tag model with soft delete
- [x] Create Tax Expense model with foreign key to Tracker and many-to-many to Tags
- [x] Implement Tracker CRUD endpoints (with soft delete)
- [x] Implement Tag CRUD endpoints (with soft delete)
- [x] Implement Expense CRUD endpoints (with Source and Tags relationship)
- [x] Add global query filters for soft-deleted records
- [x] Add data validation
- [ ] Configure local and cloud appsettings (dev + production)

### Phase 2: Frontend Setup (Week 1-2)
- [ ] Initialize Angular project
- [ ] Create services for API communication (Expense, Tracker & Tag services)
- [ ] Build tracker management component with soft delete support
- [ ] Build tag management component with soft delete support
- [ ] Build expense list component with source display and tags
- [ ] Build expense form component with tracker dropdown and tag multi-select
- [ ] Implement routing
- [ ] Add styling (Angular Material)

### Phase 3: Integration & Polish (Week 2)
- [ ] Connect frontend to backend API
- [ ] Implement filters by tracker/source and tags
- [ ] Add pagination
- [ ] Add dashboard with summary stats grouped by tracker and tags
- [ ] Implement soft delete UI (soft delete buttons, restore option)
- [ ] Error handling and user feedback
- [ ] Configure production build and environment files

### Phase 4: Deployment & Enhancement (Week 3)
- [ ] Cloud deployment setup (no containers)
- [ ] Testing (unit & integration)
- [ ] CSV export functionality (grouped by source and tags)
- [ ] Charts/graphs for expense trends by tracker and tags
- [ ] Performance optimization for soft delete queries

---

## Cloud Deployment & Security (Azure)

### Recommended: Azure App Service Free Tier + Azure SQL

**Setup:**
```bash
# Create resource group
az group create --name TaxTrackerRG --location eastus

# Create App Service Plan (Free tier)
az appservice plan create --name TaxTrackerPlan \
  --resource-group TaxTrackerRG --sku FREE

# Create API Web App
az webapp create --resource-group TaxTrackerRG \
  --plan TaxTrackerPlan --name tax-tracker-api --runtime "DOTNET|10"

# Publish API directly from local machine
dotnet publish src/TaxExpenseTracker.Api -c Release
az webapp deploy --resource-group TaxTrackerRG \
  --name tax-tracker-api --src-path <path-to-publish-zip>

# Host Angular app as static site (Azure Static Web Apps or Blob Static Website)
ng build --configuration production
```

**Pros:**
- ✅ Free tier sufficient for infrequent use
- ✅ Includes SSL/HTTPS by default
- ✅ No container setup required
- ✅ Built-in CI/CD with GitHub Actions

**Security Implementation:**

1. **API Key Protection** (Simple & Effective)
```csharp
// Add to Program.cs
services.AddScoped<ApiKeyMiddleware>();

// Middleware checks X-API-Key header
app.UseMiddleware<ApiKeyMiddleware>();
```

2. **JWT Token (Recommended)**
```csharp
// Program.cs - Single user authentication
services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options => {
        options.Authority = "https://your-azure-ad.onmicrosoft.com/";
        options.Audience = "api://tax-tracker";
    });

app.UseAuthentication();
app.UseAuthorization();
```

3. **Frontend Security**
- Store JWT token in localStorage
- Include token in all API requests
- Auto-logout on token expiry (24 hours recommended)

**API Security Implementation:**

```csharp
// 1. Simple API Key Middleware
public class ApiKeyMiddleware
{
    private readonly RequestDelegate _next;
    
    public ApiKeyMiddleware(RequestDelegate next)
    {
        _next = next;
    }
    
    public async Task InvokeAsync(HttpContext context)
    {
        if (!context.Request.Headers.TryGetValue("X-API-Key", out var apiKey))
        {
            context.Response.StatusCode = 401;
            return;
        }
        
        var configApiKey = Environment.GetEnvironmentVariable("API_KEY");
        if (apiKey != configApiKey)
        {
            context.Response.StatusCode = 403;
            return;
        }
        
        await _next(context);
    }
}

// 2. Environment Variables (Store Securely)
// Azure: Configuration -> Connection Strings
```

**Frontend Security:**
```typescript
// environment.prod.ts
export const environment = {
  production: true,
  apiUrl: 'https://tax-tracker.azurewebsites.net/api',
  apiKey: 'your-secure-api-key'  // Or retrieve from secure endpoint
};

// Interceptor
@Injectable()
export class ApiKeyInterceptor implements HttpInterceptor {
  intercept(req: HttpRequest<any>, next: HttpHandler) {
    const authReq = req.clone({
      setHeaders: {
        'X-API-Key': environment.apiKey
      }
    });
    return next.handle(authReq);
  }
}
```

### Security Best Practices

| Feature | Implementation | Cost |
|---------|----------------|------|
| **HTTPS/SSL** | Automatic (all platforms) | Free |
| **API Key** | Environment variable + header check | Free |
| **JWT Tokens** | Azure AD / Google Identity | Free |
| **Rate Limiting** | Nginx or cloud provider | Free |
| **CORS** | Restrict to your domain only | Free |
| **Firewall Rules** | Cloud provider built-in | Free |
| **Secrets Management** | Azure Key Vault | Free tier |
| **Monitoring** | Application Insights (limited free) | Free |

### Recommended Security Setup

```yaml
Authentication:
  Method: API Key + JWT Token combination
  Flow:
    1. Login with email/password (one-time setup)
    2. Backend validates against single hardcoded user
    3. Returns JWT token (valid 24 hours)
    4. Frontend includes JWT in all API requests
    5. API validates JWT on every request

Environment Variables (Cloud Secrets):
  - API_KEY: Unique identifier
  - JWT_SECRET: Token signing secret
  - CONNECTION_STRING: Database connection (with encryption at rest)
  - ALLOWED_ORIGINS: CORS whitelist (your IP/domain only)

Database Security:
  - SQLite: Encrypt database file (at-rest encryption)
  - Enable database backups
  - No public internet access
```

### Cost Breakdown (Monthly)

| Platform | Frontend | Backend | Database | Total |
|----------|----------|---------|----------|-------|
| **Azure** | Static hosting free tier | Free tier | Free tier* | $0-5 |

*Azure SQL free tier has limitations; use SQLite with blob storage for production

### Deployment Checklist

- [ ] Confirm Azure subscription and resource group
- [ ] Publish API with direct deploy (no container)
- [ ] Build and host Angular static files
- [ ] Set up environment variables for secrets
- [ ] Implement API Key middleware
- [ ] Add JWT authentication
- [ ] Configure CORS (allow only your domain)
- [ ] Set up automatic HTTPS/SSL
- [ ] Enable audit logging
- [ ] Set rate limiting (10 req/sec per user)
- [ ] Configure database backups
- [ ] Test from phone on public WiFi
- [ ] Monitor logs for suspicious activity

---

## Database Considerations

### SQLite (Development)
- **Pros**: File-based, zero configuration, no server needed
- **Cons**: Limited concurrent writes, not ideal for production scale
- **File Location**: `./data/expenses.db`

### Migration Path
- Start with SQLite
- Migrate to PostgreSQL for production
- Use Entity Framework Core migrations for consistency

### Initial Migration & Seeding
```csharp
dotnet ef migrations add InitialCreate
dotnet ef database update

// Seed data will be automatically applied via DbContext.OnModelCreating()
// Default trackers inserted on initial database creation:
// - H&R Block
// - Pluralsight
// - Udemy
// - JB Hifi
// - Office Works
```

---

## Key Features to Implement

### MVP (Minimum Viable Product)
1. ✅ Create/Read/Update/Delete expenses
2. ✅ View expense list with basic filtering
3. ✅ Form validation
4. ✅ Responsive UI

### Phase 2 Enhancements
1. ⏳ Dashboard with summary cards
2. ⏳ Monthly/yearly reports
3. ⏳ Export to CSV
4. ⏳ Charts and trends visualization
5. ⏳ Search functionality

### Future Enhancements
1. Multi-user support with authentication
2. Receipt image upload/storage
3. Category management
4. Recurring expenses
5. Tax deduction recommendations
6. Mobile app (React Native)
7. Integration with accounting software

---

## Getting Started Commands

### Backend Setup
```bash
cd Backend
dotnet new webapi -n TaxExpenseTracker.Api
cd TaxExpenseTracker.Api
dotnet add package Microsoft.EntityFrameworkCore.Sqlite
dotnet ef migrations add InitialCreate
dotnet ef database update
dotnet run
```

### Frontend Setup
```bash
ng new TaxExpenseTracker --routing --style=scss
cd TaxExpenseTracker
npm install @angular/material
ng serve
```

### Local Run
```bash
# API
dotnet run

# Frontend (separate terminal)
ng serve

# Access API at http://localhost:5000
# Access Frontend at http://localhost:4200
```

---

## Success Criteria
- [ ] Expenses can be created, read, updated, and deleted via UI
- [ ] Data persists in SQLite database
- [ ] Frontend communicates successfully with API
- [ ] Application runs locally and via cloud deployment
- [ ] Responsive design works on desktop and tablet
- [ ] All form validations work properly
- [ ] Performance acceptable for 1000+ expense records

---

## Questions to Address
1. **User Authentication**: Will this be single-user or multi-user?
2. **Data Export**: Need CSV/PDF export for tax filing?
3. **Reporting**: Need specific tax reports/summaries?
4. **Notifications**: Email reminders for recurring expenses?
5. **Mobile Support**: Need native mobile apps?
6. **Scalability**: Expected user base and expense records?

