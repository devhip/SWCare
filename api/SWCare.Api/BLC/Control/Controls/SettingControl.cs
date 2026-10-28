using System.Globalization;
using SWCare.Api.BLC.Control.IControls;
using SWCare.Api.Data;
using SWCare.Api.DTO.Req;
using SWCare.Api.DTO.Res;
using SWCare.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace SWCare.Api.BLC.Control.Controls;

public sealed class SettingControl(SWCareDbContext db) : ISettingControl
{
    public async Task<SettingsRes> GetAsync(CancellationToken cancellationToken)
    {
        var values = await db.AppSettings.AsNoTracking().ToDictionaryAsync(s => s.Key, s => s.Value, cancellationToken);

        return new SettingsRes(
            values.TryGetValue(AppSettingKeys.AutoCloseWorkingDays, out var days)
            && int.TryParse(days, CultureInfo.InvariantCulture, out var parsedDays)
                ? parsedDays
                : AppSettingDefaults.AutoCloseWorkingDays,
            values.GetValueOrDefault(AppSettingKeys.MorningReminderTime, AppSettingDefaults.MorningReminderTime),
            values.GetValueOrDefault(AppSettingKeys.TestedDetailsTemplate, AppSettingDefaults.TestedDetailsTemplate));
    }

    public async Task<SettingsRes> UpdateAsync(UpdateSettingsReq request, CancellationToken cancellationToken)
    {
        await SetAsync(AppSettingKeys.AutoCloseWorkingDays, request.AutoCloseWorkingDays.ToString(CultureInfo.InvariantCulture), cancellationToken);
        await SetAsync(AppSettingKeys.MorningReminderTime, request.MorningReminderTime, cancellationToken);
        await SetAsync(AppSettingKeys.TestedDetailsTemplate, request.TestedDetailsTemplate.Trim(), cancellationToken);
        await db.SaveChangesAsync(cancellationToken);
        return await GetAsync(cancellationToken);
    }

    private async Task SetAsync(string key, string value, CancellationToken cancellationToken)
    {
        var setting = await db.AppSettings.SingleOrDefaultAsync(s => s.Key == key, cancellationToken);
        if (setting is null)
        {
            db.AppSettings.Add(new AppSetting { Key = key, Value = value });
        }
        else
        {
            setting.Value = value;
        }
    }
}
