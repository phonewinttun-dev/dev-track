using DevTrack.Shared;

namespace DevTrack.Features.TrainingManagement.Training;

public partial class TrainingService
{
    private static Result ValidateMarkings(BulkAttendanceRequest request)
    {
        #region Validate Markings

        if (request.Markings is not { Count: > 0 } ||
            request.Markings.Any(item => item is null ||
                item.Status is not ("Present" or "Absent" or "Leave" or "Late")))
        {
            return Result.Failure("Submit at least one marking and use a valid attendance status.");
        }

        return Result.Success("Attendance statuses are valid.");

        #endregion
    }
}
