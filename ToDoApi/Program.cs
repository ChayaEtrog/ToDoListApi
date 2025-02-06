using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using TodoApi;
var builder = WebApplication.CreateBuilder(args);

// הזרקת DbContext לשירותים
builder.Services.AddDbContext<ToDoDbContext>(options =>
    options.UseMySql(builder.Configuration.GetConnectionString("ToDoDB"),
    ServerVersion.AutoDetect(builder.Configuration.GetConnectionString("ToDoDB"))));

// הוספת שירותי CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll",
        policy =>
        {
            policy.AllowAnyOrigin()
                  .AllowAnyMethod()
                  .AllowAnyHeader();
        });
});

// הוספת שירותי Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "ToDo API", Version = "v1" });
});

var app = builder.Build();

// שימוש ב-Swagger (כמובן רק בסביבה של פיתוח)
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "ToDo API v1");
    c.RoutePrefix = string.Empty;  // הפניה לדף Swagger בשטח הבסיסי
});

// שימוש במדיניות CORS
app.UseCors("AllowAll");

// שליפת כל המשימות
app.MapGet("/tasks", async (ToDoDbContext db) =>
{
    var tasks = await db.Items.ToListAsync();
    return tasks.Any() ? Results.Ok(tasks) : Results.NoContent();
});

// הוספת משימה חדשה
app.MapPost("/tasks", async (ToDoDbContext db, Items newTask) =>
{
    db.Items.Add(newTask);
    await db.SaveChangesAsync();
    return Results.Created($"/tasks/{newTask.Id}", newTask);
});

// עדכון משימה
app.MapPut("/tasks/{id}", async (ToDoDbContext db, int id, Items updatedTask) =>
{
    var existingTask = await db.Items.FindAsync(id);
    if (existingTask is null)
        return Results.NotFound();

    existingTask.Name = updatedTask.Name;
    existingTask.IsComplete = updatedTask.IsComplete;

    await db.SaveChangesAsync();
    return Results.Ok(existingTask);
});

// מחיקת משימה
app.MapDelete("/tasks/{id}", async (ToDoDbContext db, int id) =>
{
    var task = await db.Items.FindAsync(id);
    if (task is null)
        return Results.NotFound();

    db.Items.Remove(task);
    await db.SaveChangesAsync();
    return Results.NoContent();
});

app.MapGet("/", () => "ToDoListApi is running...");

app.Run();