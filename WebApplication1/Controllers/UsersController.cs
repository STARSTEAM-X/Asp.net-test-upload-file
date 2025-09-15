using Microsoft.AspNetCore.Mvc;
using MySqlConnector;
using UserRegisterApi.Models;
using UserRegisterApi.Helpers;
using System.Globalization;
using BCrypt.Net;

namespace UserRegisterApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class UsersController : ControllerBase
{
    private readonly string _connStr;
    private readonly IWebHostEnvironment _env;

    public UsersController(IConfiguration config, IWebHostEnvironment env)
    {
        _connStr = config.GetConnectionString("Default")!;
        _env = env;
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterDto dto)
    {
        // 1) parse วันที่ได้ทั้ง 1990-02-29 และ 29-02-1990
        var formats = new[] { "yyyy-MM-dd", "dd-MM-yyyy", "dd/MM/yyyy", "yyyy/MM/dd" };
        if (!DateTime.TryParseExact(dto.DateOfBirth, formats, CultureInfo.InvariantCulture,
            DateTimeStyles.None, out var dob))
        {
            return BadRequest("Invalid date format. Use yyyy-MM-dd or dd-MM-yyyy");
        }

        // 2) hash password (อย่าบันทึกรหัส plain-text)
        var passwordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password, workFactor: 11);

        // 3) ถ้ามีรูปให้บันทึกไฟล์ลง wwwroot/uploads
        string? imgPath = null;
        if (!string.IsNullOrWhiteSpace(dto.Profile_Img))
        {
            var (bytes, ext) = ImageHelper.DecodeBase64Image(dto.Profile_Img);

            const int MAX_SIZE = 5 * 1024 * 1024; // 5MB
            if (bytes.Length > MAX_SIZE) return BadRequest("Image too large (>5MB)");

            var uploads = Path.Combine(_env.WebRootPath ?? "wwwroot", "uploads");
            Directory.CreateDirectory(uploads);

            var fileName = $"{Guid.NewGuid()}{ext}";
            var savePath = Path.Combine(uploads, fileName);
            await System.IO.File.WriteAllBytesAsync(savePath, bytes);

            imgPath = "/uploads/" + fileName;
        }

        // 4) insert ลง MySQL (เก็บ path)
        using var conn = new MySqlConnection(_connStr);
        await conn.OpenAsync();

        var sql = @"
INSERT INTO users (username, password, firstname, surname, dateofbirth, gender, email, profile_img)
VALUES (@u, @p, @f, @s, @dob, @g, @e, @img);
SELECT LAST_INSERT_ID();";

        using var cmd = new MySqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@u", dto.Username);
        cmd.Parameters.AddWithValue("@p", passwordHash);
        cmd.Parameters.AddWithValue("@f", dto.Firstname);
        cmd.Parameters.AddWithValue("@s", dto.Surname);
        cmd.Parameters.AddWithValue("@dob", dob);
        cmd.Parameters.AddWithValue("@g", dto.Gender);
        cmd.Parameters.AddWithValue("@e", dto.Email);
        cmd.Parameters.AddWithValue("@img", (object?)imgPath ?? DBNull.Value);

        var newId = Convert.ToInt32(await cmd.ExecuteScalarAsync());

        return Ok(new
        {
            id = newId,
            imageUrl = imgPath,
            message = "Registered successfully"
        });
    }

    // API ดึงข้อมูลผู้ใช้ (สำหรับทดสอบ)
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        using var conn = new MySqlConnection(_connStr);
        await conn.OpenAsync();

        var sql = @"SELECT id, username, firstname, surname, dateofbirth, gender, email, profile_img
                    FROM users WHERE id=@id";
        using var cmd = new MySqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@id", id);

        using var rdr = await cmd.ExecuteReaderAsync();
        if (!await rdr.ReadAsync()) return NotFound();

        int idxProfileImg = rdr.GetOrdinal("profile_img");

        return Ok(new
        {
            id = rdr.GetInt32(rdr.GetOrdinal("id")),
            username = rdr.GetString(rdr.GetOrdinal("username")),
            firstname = rdr.GetString(rdr.GetOrdinal("firstname")),
            surname = rdr.GetString(rdr.GetOrdinal("surname")),
            dateofbirth = rdr.GetDateTime(rdr.GetOrdinal("dateofbirth")).ToString("yyyy-MM-dd"),
            gender = rdr.GetString(rdr.GetOrdinal("gender")),
            email = rdr.GetString(rdr.GetOrdinal("email")),
            profile_img = rdr.IsDBNull(idxProfileImg) ? null : rdr.GetString(idxProfileImg)
        });
    }
}
