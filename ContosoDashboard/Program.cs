using Microsoft.EntityFrameworkCore;
using ContosoDashboard.Data;
using ContosoDashboard.Services;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Components.Authorization;
using System.Security.Claims;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor();

// Add authentication state provider for Blazor
builder.Services.AddScoped<AuthenticationStateProvider, CustomAuthenticationStateProvider>();

// Configure Database
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Configure Mock Authentication (Cookie-based for training purposes)
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/login";
        options.LogoutPath = "/logout";
        options.AccessDeniedPath = "/login";
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
        options.SlidingExpiration = true;
    });

// Add authorization
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("Employee", policy => policy.RequireRole("Employee", "TeamLead", "ProjectManager", "Administrator"));
    options.AddPolicy("TeamLead", policy => policy.RequireRole("TeamLead", "ProjectManager", "Administrator"));
    options.AddPolicy("ProjectManager", policy => policy.RequireRole("ProjectManager", "Administrator"));
    options.AddPolicy("Administrator", policy => policy.RequireRole("Administrator"));
});

// Register application services
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<ITaskService, TaskService>();
builder.Services.AddScoped<IProjectService, ProjectService>();
builder.Services.AddScoped<INotificationService, NotificationService>();
builder.Services.AddScoped<IDashboardService, DashboardService>();
builder.Services.AddScoped<IFileStorageService, LocalFileStorageService>();
builder.Services.AddScoped<IFileScanService, DeterministicFileScanService>();
builder.Services.AddScoped<IDocumentService, DocumentService>();

// Add HttpContextAccessor for accessing user claims
builder.Services.AddHttpContextAccessor();

var app = builder.Build();

// Initialize database
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<ApplicationDbContext>();
        context.Database.EnsureCreated(); // For development - use migrations in production
        EnsureDocumentTables(context);
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "An error occurred creating the database.");
    }
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}
else
{
    // Use HSTS even in development for training purposes
    app.UseHsts();
}

// Add security headers
app.Use(async (context, next) =>
{
    context.Response.Headers["X-Content-Type-Options"] = "nosniff";
    context.Response.Headers["X-Frame-Options"] = "DENY";
    context.Response.Headers["X-XSS-Protection"] = "1; mode=block";
    context.Response.Headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
    
    // Content Security Policy for Blazor Server
    context.Response.Headers["Content-Security-Policy"] = 
        "default-src 'self'; " +
        "script-src 'self' 'unsafe-inline' 'unsafe-eval' https://cdn.jsdelivr.net; " +
        "style-src 'self' 'unsafe-inline' https://cdn.jsdelivr.net; " +
        "font-src 'self' https://cdn.jsdelivr.net; " +
        "img-src 'self' data: https:; " +
        "connect-src 'self' wss: ws:;";
    
    await next();
});

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

// Enable authentication and authorization
app.UseAuthentication();
app.UseAuthorization();

app.MapBlazorHub();
app.MapGet("/documents/{documentId:int}/content", async (
    int documentId,
    IDocumentService documentService,
    ClaimsPrincipal user,
    CancellationToken cancellationToken) =>
{
    var userId = GetUserId(user);
    if (!userId.HasValue) return Results.Unauthorized();

    var content = await documentService.OpenContentAsync(documentId, userId.Value, false, cancellationToken);
    return content is null
        ? Results.NotFound()
        : Results.Stream(content.Content, content.ContentType, content.FileName, enableRangeProcessing: true);
}).RequireAuthorization();

app.MapGet("/documents/{documentId:int}/preview", async (
    int documentId,
    IDocumentService documentService,
    ClaimsPrincipal user,
    CancellationToken cancellationToken) =>
{
    var userId = GetUserId(user);
    if (!userId.HasValue) return Results.Unauthorized();

    var content = await documentService.OpenContentAsync(documentId, userId.Value, true, cancellationToken);
    return content is null
        ? Results.NotFound()
        : Results.Stream(content.Content, content.ContentType, enableRangeProcessing: true);
}).RequireAuthorization();
app.MapFallbackToPage("/_Host");

app.Run();

static int? GetUserId(ClaimsPrincipal user)
{
    var value = user.FindFirstValue(ClaimTypes.NameIdentifier);
    return int.TryParse(value, out var userId) ? userId : null;
}

static void EnsureDocumentTables(ApplicationDbContext context)
{
    context.Database.ExecuteSqlRaw("""
        IF OBJECT_ID(N'[Documents]', N'U') IS NULL
        BEGIN
            CREATE TABLE [Documents]
            (
                [DocumentId] int NOT NULL IDENTITY,
                [Title] nvarchar(255) NOT NULL,
                [Description] nvarchar(2000) NULL,
                [Category] nvarchar(100) NOT NULL,
                [Tags] nvarchar(2000) NULL,
                [FileName] nvarchar(255) NOT NULL,
                [FilePath] nvarchar(1024) NOT NULL,
                [FileType] nvarchar(255) NOT NULL,
                [FileSize] bigint NOT NULL,
                [UploadedByUserId] int NOT NULL,
                [ProjectId] int NULL,
                [TaskId] int NULL,
                [UploadedDate] datetime2 NOT NULL,
                [UpdatedDate] datetime2 NOT NULL,
                [DeletedDate] datetime2 NULL,
                CONSTRAINT [PK_Documents] PRIMARY KEY ([DocumentId]),
                CONSTRAINT [FK_Documents_Users_UploadedByUserId] FOREIGN KEY ([UploadedByUserId]) REFERENCES [Users] ([UserId]),
                CONSTRAINT [FK_Documents_Projects_ProjectId] FOREIGN KEY ([ProjectId]) REFERENCES [Projects] ([ProjectId]),
                CONSTRAINT [FK_Documents_Tasks_TaskId] FOREIGN KEY ([TaskId]) REFERENCES [Tasks] ([TaskId])
            );
            CREATE INDEX [IX_Documents_UploadedByUserId_UploadedDate] ON [Documents] ([UploadedByUserId], [UploadedDate]);
            CREATE INDEX [IX_Documents_ProjectId_UploadedDate] ON [Documents] ([ProjectId], [UploadedDate]);
            CREATE INDEX [IX_Documents_Category] ON [Documents] ([Category]);
            CREATE INDEX [IX_Documents_DeletedDate] ON [Documents] ([DeletedDate]);
        END

        IF OBJECT_ID(N'[DocumentShares]', N'U') IS NULL
        BEGIN
            CREATE TABLE [DocumentShares]
            (
                [DocumentShareId] int NOT NULL IDENTITY,
                [DocumentId] int NOT NULL,
                [RecipientUserId] int NULL,
                [RecipientTeamKey] nvarchar(255) NULL,
                [GrantedByUserId] int NOT NULL,
                [GrantedDate] datetime2 NOT NULL,
                [RevokedDate] datetime2 NULL,
                CONSTRAINT [PK_DocumentShares] PRIMARY KEY ([DocumentShareId]),
                CONSTRAINT [FK_DocumentShares_Documents_DocumentId] FOREIGN KEY ([DocumentId]) REFERENCES [Documents] ([DocumentId]),
                CONSTRAINT [FK_DocumentShares_Users_RecipientUserId] FOREIGN KEY ([RecipientUserId]) REFERENCES [Users] ([UserId]),
                CONSTRAINT [FK_DocumentShares_Users_GrantedByUserId] FOREIGN KEY ([GrantedByUserId]) REFERENCES [Users] ([UserId])
            );
            CREATE INDEX [IX_DocumentShares_DocumentId_RecipientUserId_RecipientTeamKey] ON [DocumentShares] ([DocumentId], [RecipientUserId], [RecipientTeamKey]);
        END

        IF OBJECT_ID(N'[DocumentActivities]', N'U') IS NULL
        BEGIN
            CREATE TABLE [DocumentActivities]
            (
                [DocumentActivityId] int NOT NULL IDENTITY,
                [DocumentId] int NOT NULL,
                [ActorUserId] int NOT NULL,
                [Action] nvarchar(50) NOT NULL,
                [OccurredDate] datetime2 NOT NULL,
                [RetainUntil] datetime2 NOT NULL,
                [Details] nvarchar(1000) NULL,
                CONSTRAINT [PK_DocumentActivities] PRIMARY KEY ([DocumentActivityId]),
                CONSTRAINT [FK_DocumentActivities_Documents_DocumentId] FOREIGN KEY ([DocumentId]) REFERENCES [Documents] ([DocumentId]),
                CONSTRAINT [FK_DocumentActivities_Users_ActorUserId] FOREIGN KEY ([ActorUserId]) REFERENCES [Users] ([UserId])
            );
            CREATE INDEX [IX_DocumentActivities_DocumentId_OccurredDate] ON [DocumentActivities] ([DocumentId], [OccurredDate]);
            CREATE INDEX [IX_DocumentActivities_RetainUntil] ON [DocumentActivities] ([RetainUntil]);
        END
        """);
}
