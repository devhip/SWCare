using System.Security.Cryptography;
using SWCare.Api.Models;
using SWCare.Api.Services;
using Microsoft.EntityFrameworkCore;

namespace SWCare.Api.Data;

/// <summary>
/// First-start data (plan 6.2): the four departments, the draft products and device models from the sheet,
/// default settings and an Admin account. Each part runs only while its table is empty, so later changes
/// made by the Admin are never overwritten.
/// </summary>
public sealed class DbSeeder(SWCareDbContext db, IConfiguration configuration, ILogger<DbSeeder> logger)
{
    /// <summary>Draft from the sheet; owners are set by the Admin (Q19).</summary>
    private static readonly string[] Products =
    [
        "HIP TIME 4.0",
        "HIP Premium Time (Series 8)",
        "HIP TIME On Cloud",
        "HIPEZTIME",
        "HIP CarPark Solution",
        "HIP EasyPark",
        "HIP EzLPR",
        "HIP LPR Payment",
        "HIP Easynext",
        "Fix Solutions",
        "อื่นๆ (Other)"
    ];

    private static readonly string[] DeviceModels =
    [
        "CMTV924",
        "CiF93S-VL",
        "CMi511C",
        "CMiF54C",
        "CiF55C",
        "CiF263-VL",
        "CiF265-VL",
        "CiF273-VL",
        "CiF510-FP",
        "CMiF63S",
        "CMiF68S",
        "CMiF68S Plus",
        "CMiF75S",
        "CMiF95S-VL"
    ];

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        if (!await db.Departments.AnyAsync(cancellationToken))
        {
            db.Departments.AddRange(
                new Department { Name = "Customer Support", Kind = DepartmentKind.Support },
                new Department { Name = "Sale", Kind = DepartmentKind.General },
                new Department { Name = "Product & Innovation", Kind = DepartmentKind.General },
                new Department { Name = "Software", Kind = DepartmentKind.Software });
        }

        if (!await db.Products.AnyAsync(cancellationToken))
        {
            db.Products.AddRange(Products.Select((name, index) => new Product { Name = name, SortOrder = (index + 1) * 10 }));
        }

        if (!await db.DeviceModels.AnyAsync(cancellationToken))
        {
            db.DeviceModels.AddRange(DeviceModels.Select((name, index) => new DeviceModel { Name = name, SortOrder = (index + 1) * 10 }));
        }

        await AddSettingIfMissingAsync(AppSettingKeys.AutoCloseWorkingDays, AppSettingDefaults.AutoCloseWorkingDays.ToString(), cancellationToken);
        await AddSettingIfMissingAsync(AppSettingKeys.MorningReminderTime, AppSettingDefaults.MorningReminderTime, cancellationToken);
        await AddSettingIfMissingAsync(AppSettingKeys.TestedDetailsTemplate, AppSettingDefaults.TestedDetailsTemplate, cancellationToken);

        await db.SaveChangesAsync(cancellationToken);

        if (!await db.Users.AnyAsync(cancellationToken))
        {
            await SeedAdminAsync(cancellationToken);
        }
    }

    private async Task AddSettingIfMissingAsync(string key, string value, CancellationToken cancellationToken)
    {
        if (!await db.AppSettings.AnyAsync(s => s.Key == key, cancellationToken))
        {
            db.AppSettings.Add(new AppSetting { Key = key, Value = value });
        }
    }

    /// <summary>
    /// The first account, so the Admin can create the others. Its password comes from Seed:AdminPassword
    /// (user-secrets on a dev machine, the Seed__AdminPassword environment variable on the server); without it
    /// a random one is printed on the console once. It must be changed at the first login either way.
    /// </summary>
    private async Task SeedAdminAsync(CancellationToken cancellationToken)
    {
        var password = configuration["Seed:AdminPassword"];
        var generated = string.IsNullOrWhiteSpace(password);
        if (generated)
        {
            password = RandomNumberGenerator.GetString("ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnopqrstuvwxyz23456789", 16);
        }

        var software = await db.Departments.SingleAsync(d => d.Kind == DepartmentKind.Software, cancellationToken);
        db.Users.Add(new User
        {
            Username = "admin",
            DisplayName = "Administrator",
            DepartmentId = software.Id,
            IsAdmin = true,
            PasswordHash = PasswordHasher.Hash(password!),
            MustChangePassword = true
        });
        await db.SaveChangesAsync(cancellationToken);

        logger.LogWarning("Created the first account 'admin'. Its password must be changed at the first login.");
        if (generated)
        {
            // Console only, not the log files.
            Console.WriteLine($"SWCare: one-time password of 'admin' = {password}");
        }
    }
}
