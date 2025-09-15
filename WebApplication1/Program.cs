using MySqlConnector;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddCors(o => o.AddDefaultPolicy(p =>
    p.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod()
));

var app = builder.Build();

// ให้บริการไฟล์ใน wwwroot (เช่น /uploads/xxx.png)
app.UseStaticFiles();
app.UseCors();

app.MapControllers();

// ตั้งพอร์ตเป็น 5000 (วิธีง่ายช่วง Dev)
app.Urls.Add("http://localhost:5000");

app.Run();
