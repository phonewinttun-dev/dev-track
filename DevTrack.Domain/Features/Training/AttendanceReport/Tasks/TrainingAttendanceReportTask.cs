using DevTrack.Database.Entities;
using DevTrack.Shared;
using Microsoft.EntityFrameworkCore;

namespace DevTrack.Features.TrainingManagement.Training;

public partial class TrainingService
{
    private async Task<Result<List<SummaryDeveloper>>> LoadReportDevelopersAsync(int batchId)
    {
        #region Load Report Developers

        var developers = await dbContext.BatchDevelopers
            .Where(bd => bd.BatchId == batchId)
            .Select(bd => new SummaryDeveloper
            {
                Id = bd.Developer.Id,
                FullName = bd.Developer.FullName,
                DeveloperCode = bd.Developer.DeveloperCode
            })
            .OrderBy(bd => bd.FullName)
            .ToListAsync();

        return Result<List<SummaryDeveloper>>.Success(developers, "Report developers loaded.");

        #endregion
    }

    private async Task<Result<List<AttendanceRecord>>> LoadReportAttendanceAsync(int batchId)
    {
        #region Load Report Attendance

        var attendance = await dbContext.AttendanceRecords
            .AsNoTracking()
            .Where(a => a.BatchId == batchId)
            .ToListAsync();

        return Result<List<AttendanceRecord>>.Success(attendance, "Report attendance loaded.");

        #endregion
    }
}
