using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddDbContext<TodoDb>(opt => opt.UseInMemoryDatabase("TodoList"));
builder.Services.AddDatabaseDeveloperPageExceptionFilter();

// authentik access tokens (client_credentials) are validated locally against the provider's JWKS.
// Settings: "authentik" section (see appsettings.Development.json / README.md).
var authentik = builder.Configuration.GetSection("authentik");
builder.Services
    .AddAuthentication()
    .AddJwtBearer(o =>
    {
        // e.g. http://localhost:9000/application/o/todoapi/ (issuer; discovery at <authority>.well-known/openid-configuration)
        o.Authority = authentik["authority"];
        // authentik sets aud = client_id of the provider that issued the token
        o.Audience = authentik["audience"];
        o.RequireHttpsMetadata = authentik.GetValue("requireHttpsMetadata", true);
        // keep claim names as authentik sends them (sub, scope, azp, ...)
        o.MapInboundClaims = false;
    });

builder.Services.AddAuthorizationBuilder()
    .AddPolicy(TodoScopes.Read, p => p.RequireAuthenticatedUser().RequireAssertion(c => TodoScopes.Has(c.User, TodoScopes.Read)))
    .AddPolicy(TodoScopes.Write, p => p.RequireAuthenticatedUser().RequireAssertion(c => TodoScopes.Has(c.User, TodoScopes.Write)));

var app = builder.Build();

app.UseAuthentication();
app.UseAuthorization();

RouteGroupBuilder todoItems = app.MapGroup("/todoitems");

todoItems.MapGet("/", GetAllTodos).RequireAuthorization(TodoScopes.Read);
todoItems.MapGet("/complete", GetCompleteTodos).RequireAuthorization(TodoScopes.Read);
todoItems.MapGet("/{id}", GetTodo).RequireAuthorization(TodoScopes.Read);
todoItems.MapPost("/", CreateTodo).RequireAuthorization(TodoScopes.Write);
todoItems.MapPut("/{id}", UpdateTodo).RequireAuthorization(TodoScopes.Write);
todoItems.MapDelete("/{id}", DeleteTodo).RequireAuthorization(TodoScopes.Write);

app.Run();

static async Task<IResult> GetAllTodos(TodoDb db)
{
    return TypedResults.Ok(await db.Todos.Select(x => new TodoItemDTO(x)).ToArrayAsync());
}

static async Task<IResult> GetCompleteTodos(TodoDb db) {
    return TypedResults.Ok(await db.Todos.Where(t => t.IsComplete).Select(x => new TodoItemDTO(x)).ToListAsync());
}

static async Task<IResult> GetTodo(int id, TodoDb db)
{
    return await db.Todos.FindAsync(id)
        is Todo todo
            ? TypedResults.Ok(new TodoItemDTO(todo))
            : TypedResults.NotFound();
}

static async Task<IResult> CreateTodo(TodoItemDTO todoItemDTO, TodoDb db)
{
    var todoItem = new Todo
    {
        IsComplete = todoItemDTO.IsComplete,
        Name = todoItemDTO.Name
    };

    db.Todos.Add(todoItem);
    await db.SaveChangesAsync();

    todoItemDTO = new TodoItemDTO(todoItem);

    return TypedResults.Created($"/todoitems/{todoItem.Id}", todoItemDTO);
}

static async Task<IResult> UpdateTodo(int id, TodoItemDTO todoItemDTO, TodoDb db)
{
    var todo = await db.Todos.FindAsync(id);

    if (todo is null) return TypedResults.NotFound();

    todo.Name = todoItemDTO.Name;
    todo.IsComplete = todoItemDTO.IsComplete;

    await db.SaveChangesAsync();

    return TypedResults.NoContent();
}

static async Task<IResult> DeleteTodo(int id, TodoDb db)
{
    if (await db.Todos.FindAsync(id) is Todo todo)
    {
        db.Todos.Remove(todo);
        await db.SaveChangesAsync();
        return TypedResults.Ok(todo);
    }

    return TypedResults.NotFound();
}

// authentik puts the granted scopes into a single space-separated "scope" claim.
static class TodoScopes
{
    public const string Read = "todo:read";
    public const string Write = "todo:write";

    public static bool Has(System.Security.Claims.ClaimsPrincipal user, string scope) =>
        user.FindAll("scope")
            .SelectMany(c => c.Value.Split(' ', StringSplitOptions.RemoveEmptyEntries))
            .Contains(scope);
}
