﻿using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;

namespace FIG.Assessment;

public class Example2 : Controller
{
    public IConfiguration _config;

    public Example2(IConfiguration config)
        => this._config = config;

    [HttpPost("login")]
    public async Task<IActionResult> LoginAsync([FromForm] LoginPostModel model)
    {
        using var conn = new SqlConnection(this._config.GetConnectionString("SQL"));
        await conn.OpenAsync();

        var sql = $"SELECT u.UserID, u.PasswordHash FROM User u WHERE u.UserName='{model.UserName}';";
        using var cmd = new SqlCommand(sql, conn);
        using var reader = await cmd.ExecuteReaderAsync();

        // first check user exists by the given username
        if (!reader.Read())
        {
            return this.Redirect("/Error?msg=invalid_username");
        }

        // then check password is correct
        var inputPasswordHash = MD5.HashData(Encoding.UTF8.GetBytes(model.Password));
        var databasePasswordHash = (byte[])reader["PasswordHash"];
        if (!databasePasswordHash.SequenceEqual(inputPasswordHash))
        {
            return this.Redirect("/Error?msg=invalid_password");
        }

        // if we get this far, we have a real user. sign them in
        var userId = (int)reader["UserID"];
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
        };
        var identity = new ClaimsIdentity(claims);
        var principal = new ClaimsPrincipal(identity);
        this.HttpContext.SignInAsync(principal);

        return this.Redirect(model.ReturnUrl);
    }
}

public class LoginPostModel
{
    public string UserName { get; set; }

    public string Password { get; set; }

    public string ReturnUrl { get; set; }
}
