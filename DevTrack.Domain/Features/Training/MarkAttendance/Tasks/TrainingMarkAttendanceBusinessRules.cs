using DevTrack.Database.Entities;
using DevTrack.Shared;
using Microsoft.EntityFrameworkCore;

namespace DevTrack.Features.TrainingManagement.Training;

public partial class TrainingService
{
    private static Result CheckAttendanceDay(TrainingCalendar calendar)
    {
        #region Check class-day eligibility

        if (!(calendar.IsAttendanceRequired ?? false))
        {
            return Result.Failure("Attendance can only be marked on class days.");
        }

        return Result.Success("Date allows attendance.");

        #endregion
    }

    private async Task<Result<HashSet<int>>> CheckRosterAsync(BulkAttendanceRequest request)
    {
        #region Check roster eligibility

        var submittedIds = request.Markings.Select(item => item.DeveloperId).ToHashSet();
        var rosterIds = await dbContext.BatchDevelopers
            .Where(assignment => assignment.BatchId == request.BatchId)
            .Select(assignment => assignment.DeveloperId)
            .ToListAsync();

        if (submittedIds.Count != request.Markings.Count || !submittedIds.SetEquals(rosterIds))
        {
            return Result<HashSet<int>>.Failure("Submit exactly one mark for each developer assigned to the batch.");
        }

        return Result<HashSet<int>>.Success(submittedIds, "Every assigned developer has one mark.");

        #endregion
    }
}
